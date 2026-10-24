using UnityEngine;
using UnityEngine.SceneManagement;

// タイトル画面からゲームシーンへ移る役。
// ボタンの OnClick に LoadGameScene() を割り当てて使う。
public class TitleController : MonoBehaviour
{
    // 移動先のシーン名。Build Settings に登録されている必要がある
    public string gameSceneName = "sunset";

    [Header("キー操作でも始める")]
    public bool startWithKey = true;
    public KeyCode[] startKeys = new KeyCode[] { KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter };

    [Header("micro:bit でも始める")]
    public bool startWithMicrobit = true;
    // 使うボタン。ここに並べたどれかが押されたら始まる
    public string[] startButtonMarks = new string[] { "A", "B" };
    // 画面が出てから何秒間、ボタンを受け付けないか。
    // 前の画面で押していたボタンでそのまま始まってしまうのを防ぐ
    public float microbitInputDelay = 0.5f;

    // 二重に読み込まないための番人
    private bool loading = false;

    private MicrobitButton startButton;

    void Start()
    {
        startButton = new MicrobitButton(microbitInputDelay);
    }

    void Update()
    {
        if (loading) return;

        if (startWithKey)
        {
            for (int i = 0; i < startKeys.Length; i++)
            {
                if (Input.GetKeyDown(startKeys[i]))
                {
                    LoadGameScene();
                    return;
                }
            }
        }

        if (startWithMicrobit && startButton != null && startButton.GetDownAny(startButtonMarks))
        {
            LoadGameScene();
        }
    }

    // Button の OnClick から呼ぶ
    public void LoadGameScene()
    {
        if (loading) return;

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("TitleController: gameSceneName が空です");
            return;
        }

        loading = true;
        SceneManager.LoadScene(gameSceneName);
    }
}
