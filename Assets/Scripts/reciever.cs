using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

// micro:bit からのシリアル通信を受け取る。
// 読み取りは別スレッドで行い、結果を static に置く。serialmover がそれを見て動く。
public class reciever : MonoBehaviour
{
    [Header("接続")]
    // 使うポート名。空にすると自動で探す
    public string portName = "";
    public int baudRate = 115200;
    // portName で繋がらなかったときに他のポートも試す
    public bool autoDetectPort = true;
    // 自動検出のとき、開いたポートから何秒データが来なければ次を試すか
    public float detectTimeout = 3.0f;
    // 繋がらないときに再試行する間隔（秒）。0以下なら再試行しない
    public float retryInterval = 5.0f;

    [Header("デバッグ")]
    // 受け取った行をそのまま Console に出す。通信内容の確認用
    public bool logRawLines = false;
    // 接続できた・切れたを Console に出す
    public bool logStatus = true;
    // shake を micro:bit の値で更新する。
    // 有効にすると serialmover が旋回を止めるので、必要なときだけ ON にする
    public bool enableShake = false;

    // -------------------------------------------------
    public static string message;
    public static float rotate = 0.0f;
    public static bool Ready = false;
    public static bool shake = false;
    public static bool logo = false;
    public static string botton_mark = "N";
    // 今つながっているポート名。繋がっていなければ空
    public static string ConnectedPort = "";

    private SerialPort serialPort;
    private Thread thread;
    private volatile bool isRunning = false;
    private readonly object messageLock = new object();
    // 別スレッドから Console へ直接出さず、ここに溜めて Update で出す
    private readonly Queue<string> pendingLogs = new Queue<string>();
    // 接続を試した内容を一度出したか。
    // 繋がらないまま再試行を続けても Console が同じ行で埋まらないようにするため
    private bool attemptsLogged = false;

    void Awake()
    {
        // static はシーンを移っても、再生を止めても値が残る。
        // 前回の傾きやボタン状態を持ち越さないよう、ここで初期化する。
        message = null;
        rotate = 0.0f;
        Ready = false;
        shake = false;
        logo = false;
        botton_mark = "N";
        ConnectedPort = "";
    }

    void Start()
    {
        isRunning = true;
        thread = new Thread(WorkerLoop);
        // 閉じ忘れてもエディタが終了できるようにしておく
        thread.IsBackground = true;
        thread.Start();
    }

    // 接続 -> 読み取り -> 切れたら再接続、をまとめて行う
    void WorkerLoop()
    {
        while (isRunning)
        {
            if (TryConnect() == false)
            {
                if (retryInterval <= 0.0f) return;

                // 次の再試行まで待つ。停止指示にはすぐ反応する
                float waited = 0.0f;
                while (isRunning && waited < retryInterval)
                {
                    Thread.Sleep(100);
                    waited += 0.1f;
                }
                continue;
            }

            ReadLoop();
            ClosePort();
        }
    }

    // 候補のポートを順に試して、使えたら true
    bool TryConnect()
    {
        List<string> candidates = new List<string>();

        if (string.IsNullOrEmpty(portName) == false)
        {
            candidates.Add(portName);
        }

        if (autoDetectPort)
        {
            string[] names;
            try { names = SerialPort.GetPortNames(); }
            catch { names = new string[0]; }

            foreach (string name in names)
            {
                if (candidates.Contains(name) == false) candidates.Add(name);
            }
        }

        if (candidates.Count == 0)
        {
            Log("reciever: 使えるシリアルポートが見つかりません");
            return false;
        }

        foreach (string name in candidates)
        {
            if (isRunning == false) return false;

            // 明示指定したポートは、データを待たずにそのまま使う
            bool explicitPort = (string.IsNullOrEmpty(portName) == false && name == portName);

            if (OpenPort(name, explicitPort))
            {
                ConnectedPort = name;
                // 繋がったので、次に切れたときはまた経過を出す
                attemptsLogged = false;
                Log("reciever: " + name + " に接続しました（" + baudRate + " bps）");
                return true;
            }
        }

        if (attemptsLogged == false)
        {
            Log("reciever: どのポートからもデータが来ませんでした（試したポート: "
                + string.Join(", ", candidates.ToArray())
                + "）。接続したら自動でつながります");
            attemptsLogged = true;
        }

        return false;
    }

    bool OpenPort(string name, bool acceptWithoutData)
    {
        SerialPort port = null;
        try
        {
            port = new SerialPort(name, baudRate);
            port.ReadTimeout = 500;
            port.NewLine = "\n";
            port.Open();
        }
        catch (System.Exception e)
        {
            // Bluetooth の仮想ポートなどは開こうとして失敗することがある。
            // 想定内なのでエラーではなく情報として出す
            if (attemptsLogged == false)
            {
                Log("reciever: " + name + " は使えません（" + e.GetType().Name + "）");
            }
            if (port != null) port.Dispose();
            return false;
        }

        if (acceptWithoutData)
        {
            serialPort = port;
            return true;
        }

        // 開けただけでは micro:bit とは限らないので、実際にデータが来るか確かめる
        float waited = 0.0f;
        while (isRunning && waited < detectTimeout)
        {
            try
            {
                string line = port.ReadLine();
                if (string.IsNullOrEmpty(line) == false)
                {
                    serialPort = port;
                    Handle(line);
                    return true;
                }
            }
            catch (System.TimeoutException) { }
            catch (System.Exception)
            {
                break;
            }
            waited += 0.5f;
        }

        if (attemptsLogged == false)
        {
            Log("reciever: " + name + " は開けましたがデータが来ません");
        }
        try { if (port.IsOpen) port.Close(); } catch { }
        port.Dispose();
        return false;
    }

    void ReadLoop()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string data = serialPort.ReadLine();
                if (string.IsNullOrEmpty(data)) continue;
                Handle(data);
            }
            catch (System.TimeoutException)
            {
                // データが来ていないだけ。正常なので何もしない
            }
            catch (System.Exception e)
            {
                // 抜かれた・切れたなど。閉じて再接続へ回す
                Log("reciever: " + ConnectedPort + " との通信が切れました（" + e.GetType().Name + "）");
                return;
            }
        }
    }

    // 1行ぶんの受信内容を解釈する
    void Handle(string data)
    {
        string line = data.Trim();
        if (line.Length == 0) return;

        lock (messageLock) { message = line; }
        Ready = true;

        if (logRawLines) Log("reciever 受信: " + line);

        if (line.Contains("roll"))
        {
            float value;
            if (TryParseValue(line, out value)) rotate = value;
        }

        if (line.Contains("botton") || line.Contains("button"))
        {
            if (line.Contains("2")) botton_mark = "A";
            else if (line.Contains("1")) botton_mark = "B";
            else if (line.Contains("0")) botton_mark = "N";
        }

        if (line.Contains("logo"))
        {
            logo = line.Contains("1");
        }

        if (enableShake && line.Contains("shake"))
        {
            shake = line.Contains("1");
        }
    }

    // "roll:-45" のような行から数値を取り出す
    static bool TryParseValue(string line, out float value)
    {
        value = 0.0f;

        int separator = line.IndexOf(':');
        string body = (separator >= 0) ? line.Substring(separator + 1) : line;

        // 数字・符号・小数点だけ拾う
        var sb = new System.Text.StringBuilder();
        foreach (char c in body)
        {
            if (char.IsDigit(c) || c == '-' || c == '+' || c == '.') sb.Append(c);
            else if (sb.Length > 0) break;
        }

        return float.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    // 直前と同じ内容は出さない。繋がらないまま再試行を続けても
    // Console が同じ行で埋まらないようにするため
    private string lastLog = "";

    void Log(string text)
    {
        bool isRaw = text.StartsWith("reciever 受信:");
        if (logStatus == false && isRaw == false) return;

        lock (pendingLogs)
        {
            if (isRaw == false)
            {
                if (text == lastLog) return;
                lastLog = text;
            }
            pendingLogs.Enqueue(text);
        }
    }

    void Update()
    {
        // 別スレッドから溜めたログをここで出す
        lock (pendingLogs)
        {
            while (pendingLogs.Count > 0) Debug.Log(pendingLogs.Dequeue());
        }

        if (message != null)
        {
            lock (messageLock) { message = null; }
        }
    }

    void OnDestroy()
    {
        isRunning = false;

        // 先にポートを閉じると、読み取り待ちのスレッドがすぐ抜ける
        ClosePort();

        // 閉じ切れなくてもエディタを固めないよう、待ち時間に上限をつける
        if (thread != null && thread.IsAlive) thread.Join(1000);
        thread = null;

        ConnectedPort = "";
    }

    void ClosePort()
    {
        if (serialPort == null) return;

        try { if (serialPort.IsOpen) serialPort.Close(); } catch { }
        try { serialPort.Dispose(); } catch { }
        serialPort = null;
    }
}
