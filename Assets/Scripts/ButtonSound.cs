using UnityEngine;
using UnityEngine.UI;

// シーン内のボタンすべてにクリック音を付ける。
// ボタンごとに設定して回らなくて済むよう、まとめて登録する。
public class ButtonSound : MonoBehaviour
{
    [Header("音源")]
    // ここにクリック音の AudioClip を入れる
    public AudioClip clickSound;
    [Range(0.0f, 1.0f)]
    public float volume = 1.0f;

    // 最初は非表示のボタン（リザルト画面など）にも付ける
    public bool includeInactive = true;

    void Start()
    {
        Button[] buttons = FindObjectsByType<Button>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].onClick.AddListener(PlayClick);
        }
    }

    // 個別に割り当てたい場合は、この関数を OnClick に直接指定してもよい
    public void PlayClick()
    {
        // シーンを切り替えるボタンでも音が途切れないよう、
        // SoundPlayer 側でシーンをまたいで鳴らしている
        SoundPlayer.Play(clickSound, volume, 0.0f);
    }
}
