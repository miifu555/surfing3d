using UnityEngine;
using UnityEngine.Serialization;
using TMPro;

// 制限時間つきレースの管理役。
// スタート時に消したスタートテープを、少し経ってからゴールテープとして出し直す。
// ゴールするたびに大量のスコアが入り、テープはまた出てくるので
// 制限時間いっぱいまで何周でもゴールできる。
public class RaceManager : MonoBehaviour
{
    [Header("制限時間")]
    // 秒。カウントダウンが終わってから数え始める
    public float timeLimit = 60.0f;

    [Header("参照")]
    // 画面右上に出す残り時間の表示
    [FormerlySerializedAs("lapTextField")]
    [SerializeField]
    private TextMeshProUGUI statusTextField;
    // スタート時に消えて、あとでゴールテープとして復活するテープ
    public GameObject goalTape;
    // 通過判定に使うプレイヤー。未設定なら serialmover の付いたオブジェクトを自動で探す
    public Transform playerTransform;
    // ライン判定の基準。未設定なら goalTape の Transform をそのまま使う
    public Transform finishLine;

    [Header("ゴールテープ")]
    // スタートラインを通過してから何秒後にゴールテープを出すか。
    // 0 にするとスタート直後に真後ろへ出てしまうので少し待たせる
    public float goalTapeAppearDelay = 3.0f;

    [Header("ゴール報酬")]
    // ゴール1回ごとにもらえるスコア
    public int goalScoreBonus = 1000;

    [Header("ゴールしたとき")]
    // ゴールのたびに宝石を置き直す
    public bool respawnGemsOnGoal = true;
    // 置き直しの担当。未設定ならシーンから自動で探す
    public GemResetter gemResetter;

    [Header("ゴール音")]
    // ゴールした瞬間に鳴らす音
    public AudioClip goalSound;
    [Range(0.0f, 1.0f)]
    public float goalSoundVolume = 1.0f;

    [Header("リザルト")]
    // 時間切れのあとに出すリザルト画面。未設定ならシーンから自動で探す
    public ResultScreen resultScreen;
    // 「TIME UP」を見せてからリザルトを出すまでの秒数
    public float resultDelay = 1.5f;

    [Header("ライン判定")]
    // ラインの半分の幅。0以下ならテープの見た目の幅から自動で決める
    public float lineHalfWidth = 0.0f;
    // 自動で決めるときに掛ける余裕
    public float lineWidthMargin = 1.5f;
    // 判定する高さの範囲。0以下なら高さを見ない
    public float lineHeightRange = 0.0f;
    // 続けて通過とみなさない最短間隔（秒）。ライン上での前後動き対策
    [FormerlySerializedAs("minLapTime")]
    public float minCrossInterval = 3.0f;
    // プレイヤーがテープの手前からスタートするか。
    // true なら最初の1回目の通過は「スタート」とみなす
    public bool startsBehindLine = true;
    // 通過方向の判定を逆にする
    public bool invertDirection = false;

    [Header("表示")]
    // {0} に "0:42" のような残り時間が入る
    public string timeFormat = "TIME {0}";
    // ゴールした瞬間に出す文字。{0} にゴール回数が入る
    public string goalMessage = "GOAL! x{0}";
    // ゴール表示を出しておく秒数。過ぎたら残り時間の表示に戻る
    public float goalMessageDuration = 1.5f;
    public string timeUpMessage = "TIME UP";

    // 残り時間（秒）
    public static float remainingTime;
    // 時間切れで終了したか
    public static bool raceFinished;
    // 時間内に何回ゴールしたか
    public static int goalCount;

    // 前フレームでプレイヤーがラインのどちら側にいたか（符号付き距離）
    private float previousSide;
    // ラインを通過した回数（スタートの1回を含む）
    private int crossCount;
    private float lastCrossTime;
    // ゴールテープを出し直す基準の時刻。スタート通過時とゴール時に更新する
    private float tapeTimerStart;
    private bool goalTapeShown;
    // ゴール表示を消す時刻
    private float goalMessageUntil;
    // リザルトを出す予定があるか
    private bool resultPending;
    // リザルトを出す時刻
    private float resultShowTime;
    private bool resultShown;

    void Awake()
    {
        // static はシーンを開き直しても値が残るので、ここで必ずリセットする
        remainingTime = timeLimit;
        raceFinished = false;
        goalCount = 0;
        crossCount = 0;
        lastCrossTime = 0.0f;
        tapeTimerStart = 0.0f;
        goalTapeShown = false;
        goalMessageUntil = 0.0f;
        resultPending = false;
        resultShowTime = 0.0f;
        resultShown = false;
    }

    void Start()
    {
        if (playerTransform == null)
        {
            serialmover mover = FindFirstObjectByType<serialmover>();
            if (mover != null)
            {
                playerTransform = mover.transform;
            }
        }

        if (finishLine == null && goalTape != null)
        {
            finishLine = goalTape.transform;
        }

        if (gemResetter == null)
        {
            gemResetter = FindFirstObjectByType<GemResetter>();
        }

        if (resultScreen == null)
        {
            resultScreen = FindFirstObjectByType<ResultScreen>();
        }

        previousSide = GetSide();
        UpdateTimeText();
    }

    void Update()
    {
        if (raceFinished)
        {
            // 「TIME UP」を少し見せてからリザルトを出す
            if (resultPending && resultShown == false && Time.time >= resultShowTime)
            {
                resultShown = true;

                if (resultScreen != null)
                {
                    resultScreen.Show(ScoreMan.score, goalCount);
                }
            }
            return;
        }

        float side = GetSide();

        // カウントダウン中は時間を減らさず、判定の基準だけ更新しておく
        if (RaceCountdown.raceStarted == false)
        {
            previousSide = side;
            return;
        }

        // 残り時間
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0.0f)
        {
            remainingTime = 0.0f;
            UpdateTimeText();
            TimeUp();
            return;
        }
        UpdateTimeText();

        // スタート後、またはゴールしたあと、しばらく経ったらゴールテープを出す
        if (goalTapeShown == false && crossCount >= 1 &&
            Time.time - tapeTimerStart >= goalTapeAppearDelay)
        {
            ShowGoalTape();
        }

        if (finishLine == null || playerTransform == null) return;

        float previous = previousSide;
        previousSide = side;

        if (IsInsideLine() == false) return;

        // 決めた向きに横切った瞬間だけを通過とみなす
        bool crossed = invertDirection
            ? (previous < 0.0f && side >= 0.0f)
            : (previous > 0.0f && side <= 0.0f);
        if (crossed == false) return;

        // 短すぎる間隔での再通過は無視する
        if (crossCount > 0 && Time.time - lastCrossTime < minCrossInterval) return;

        lastCrossTime = Time.time;
        OnCrossLine();
    }

    // ラインを1回通過したときの処理
    void OnCrossLine()
    {
        crossCount++;

        // スタート地点がテープの手前なら、最初の通過はスタート
        if (startsBehindLine && crossCount == 1)
        {
            tapeTimerStart = Time.time;
            return;
        }

        // テープが出ていないうちの通過はゴールにしない
        if (goalTapeShown == false) return;

        Goal();
    }

    // 消してあったスタートテープを、通り抜けられるゴールテープとして出す
    void ShowGoalTape()
    {
        goalTapeShown = true;

        if (goalTape == null) return;

        goalTape.SetActive(true);

        // ぶつかって止まってしまわないよう、当たり判定はすり抜けさせる
        Collider[] colliders = goalTape.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }
    }

    void Goal()
    {
        goalCount++;

        // テープを切ったので消す。時間が来たらまた出てくる
        if (goalTape != null)
        {
            goalTape.SetActive(false);
        }
        goalTapeShown = false;
        tapeTimerStart = Time.time;

        if (goalScoreBonus > 0)
        {
            ScoreMan.getScore(goalScoreBonus);
        }

        SoundPlayer.Play(goalSound, goalSoundVolume, 0.0f);

        // 宝石を置き直して、また集められるようにする
        if (respawnGemsOnGoal && gemResetter != null)
        {
            gemResetter.ResetAll();
        }

        // しばらくゴール表示を出してから残り時間の表示に戻す
        goalMessageUntil = Time.time + goalMessageDuration;
        SetText(string.Format(goalMessage, goalCount));

        // 走り続けられるようプレイヤーは止めない
    }

    void TimeUp()
    {
        raceFinished = true;

        SetText(timeUpMessage);
        StopPlayer();

        resultPending = true;
        resultShowTime = Time.time + resultDelay;
    }

    void StopPlayer()
    {
        // serialmover がこれを見て動きを止める
        RaceCountdown.raceStarted = false;
    }

    void UpdateTimeText()
    {
        // ゴール直後はゴール表示を優先する
        if (Time.time < goalMessageUntil) return;

        SetText(string.Format(timeFormat, FormatTime(remainingTime)));
    }

    void SetText(string text)
    {
        if (statusTextField != null)
        {
            statusTextField.text = text;
        }
    }

    // 42.3 秒 -> "0:43"（切り上げ。残り0のときだけ 0:00）
    public static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0.0f, seconds));
        int minutes = total / 60;
        int rest = total % 60;
        return minutes.ToString() + ":" + rest.ToString("00");
    }

    // ラインの表裏を表す符号付き距離。テープの正面側が正になる
    float GetSide()
    {
        if (finishLine == null || playerTransform == null) return 0.0f;

        Vector3 delta = playerTransform.position - finishLine.position;
        return Vector3.Dot(delta, finishLine.forward);
    }

    // テープの幅（と高さ）の内側を通っているか
    bool IsInsideLine()
    {
        Vector3 delta = playerTransform.position - finishLine.position;

        float halfWidth = (lineHalfWidth > 0.0f)
            ? lineHalfWidth
            : Mathf.Abs(finishLine.lossyScale.x) * 0.5f * lineWidthMargin;

        if (Mathf.Abs(Vector3.Dot(delta, finishLine.right)) > halfWidth) return false;

        if (lineHeightRange > 0.0f &&
            Mathf.Abs(Vector3.Dot(delta, finishLine.up)) > lineHeightRange) return false;

        return true;
    }
}
