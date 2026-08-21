using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class reciever : MonoBehaviour
{
    public string portName = "COM3";  // Windowsの場合 (Macなら "/dev/tty.usbmodemXXXX")
    public int baudRate = 115200;     // micro:bit標準の115200bps    
    private SerialPort serialPort;
    private Thread thread;
    private bool isRunning = false;
    public static string message;
    private readonly object messageLock = new object();
    // -------------------------------------------------
    public static float rotate = 0.0f;
    public static bool Ready = false;
    public static bool shake = false;
    public static bool logo = false;
    public static string botton_mark = "N";  // デフォルトで "N" に初期化

    void Start()
    {
        DontDestroyOnLoad (this);
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 1000;
        serialPort.Open();

        isRunning = true;
        thread = new Thread(ReadData);
        thread.Start();
    }

    void ReadData()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string data = serialPort.ReadLine();
                if (string.IsNullOrEmpty(data)) continue;

                lock (messageLock) { message = data; }

                Ready = true;
                
                if (data.Contains("roll"))
                {
                    // float.TryParse を使用して安全にパース
                    string rollPart = data.Substring(5);
                    if (float.TryParse(rollPart, out float parsedRotate))
                    {
                        rotate = parsedRotate;
                    }
                }

                if (data.Contains("botton"))
                {
                    if (data.Contains("2"))
                    {
                        botton_mark = "A";
                    }
                    else if (data.Contains("1"))
                    {
                        botton_mark = "B";
                    }
                    else if (data.Contains("0"))
                    {
                        botton_mark = "N";
                    }
                }

                // if (data.Contains("shake"))
                // {
                //     if (data.Contains("1"))
                //     {
                //         shake = true;
                //     }
                //     else if (data.Contains("0"))
                //     {
                //         shake = false;
                //     }
                // }

                if (data.Contains("logo"))
                {
                    if (data.Contains("1"))
                    {
                        logo = true;
                    }
                    else
                    {
                        logo = false;
                    }
                }
            }
            catch (System.Exception ex) { Debug.LogWarning("Serial read error: " + ex.Message); }
        }
    }

    void Update()
    {
        if (message != null)
        {
            lock (messageLock) { message = null; }
        }
    }

    void OnDestroy()
    {
        isRunning = false;
        if (thread != null && thread.IsAlive) thread.Join();
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();
    }
}
