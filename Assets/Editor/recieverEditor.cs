using System.Collections.Generic;
using System.IO.Ports;
using UnityEditor;
using UnityEngine;

// reciever の Inspector に COM ポートの選択肢を出す。
// 手で "COM3" と打ち込まなくても、今つながっているポートから選べるようにする。
[CustomEditor(typeof(reciever))]
public class recieverEditor : Editor
{
    // 「自動で探す」を表す値。portName が空のときがこれにあたる
    const string AutoLabel = "自動で探す";

    private string[] ports = new string[0];

    void OnEnable()
    {
        RefreshPorts();
    }

    void RefreshPorts()
    {
        try
        {
            ports = SerialPort.GetPortNames();
        }
        catch
        {
            ports = new string[0];
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty portProperty = serializedObject.FindProperty("portName");

        // 表示用のラベルと、実際に入れる値を別々に持つ
        List<string> labels = new List<string>();
        List<string> values = new List<string>();

        labels.Add(AutoLabel);
        values.Add("");

        for (int i = 0; i < ports.Length; i++)
        {
            labels.Add(ports[i]);
            values.Add(ports[i]);
        }

        string current = portProperty.stringValue;

        // 設定済みのポートが今は見つからない場合も、選択が消えないよう残す
        if (string.IsNullOrEmpty(current) == false && values.Contains(current) == false)
        {
            labels.Add(current + "（今は見つかりません）");
            values.Add(current);
        }

        int index = values.IndexOf(current);
        if (index < 0) index = 0;

        EditorGUILayout.BeginHorizontal();
        int selected = EditorGUILayout.Popup("COM ポート", index, labels.ToArray());
        if (GUILayout.Button("更新", GUILayout.Width(50)))
        {
            RefreshPorts();
        }
        EditorGUILayout.EndHorizontal();

        if (selected != index)
        {
            portProperty.stringValue = values[selected];
        }

        if (ports.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "シリアルポートが1つも見つかりません。micro:bit を USB で挿してから「更新」を押してください。",
                MessageType.Info);
        }

        // portName は上で扱ったので、残りのフィールドだけ通常どおり描く
        DrawPropertiesExcluding(serializedObject, "m_Script", "portName");

        // 実行中は今どこに繋がっているかを出す
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            string connected = string.IsNullOrEmpty(reciever.ConnectedPort) ? "未接続" : reciever.ConnectedPort;
            EditorGUILayout.LabelField("接続中のポート", connected);
            EditorGUILayout.LabelField("受信あり", reciever.Ready ? "はい" : "いいえ");
            EditorGUILayout.LabelField("傾き / ボタン",
                reciever.rotate.ToString("F1") + " / " + reciever.botton_mark);
            Repaint();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
