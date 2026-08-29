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

    // 二重に読み込まないための番人
    private bool loading = false;

    void Update()
    {
        if (startWithKey == false) return;
        if (loading) return;

        for (int i = 0; i < startKeys.Length; i++)
        {
            if (Input.GetKeyDown(startKeys[i]))
            {
                LoadGameScene();
                return;
            }
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
