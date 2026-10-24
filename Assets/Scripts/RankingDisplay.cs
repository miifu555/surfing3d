using UnityEngine;
using TMPro;

// ランキングをテキストに出すだけの表示役。タイトル画面で使う。
// 表示のたびに保存済みの記録を読み直すので、
// リザルトから戻ってきたときも最新の内容になる。
public class RankingDisplay : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI rankingText;

    [Header("表示")]
    // 何位まで出すか
    public int displayCount = 3;
    // {0} に順位、{1} にスコアが入る
    public string lineFormat = "{0}.  {1}";
    // まだ記録が無い行に出す文字
    public string emptyMark = "- - -";

    [Header("記録の消去")]
    // キーを押して保存済みの記録を消せるようにするか
    public bool allowClearKey = true;
    // 記録を消すキー
    public KeyCode clearKey = KeyCode.C;
    // 消したときに Console へ知らせるか
    public bool logOnClear = true;

    void OnEnable()
    {
        Refresh();
    }

    void Update()
    {
        if (allowClearKey == false) return;

        if (Input.GetKeyDown(clearKey))
        {
            ClearRanking();
        }
    }

    // 保存されている記録をすべて消して、表示を更新する。
    // ボタンの OnClick からも呼べるように public にしている。
    public void ClearRanking()
    {
        ScoreRanking.Clear();
        Refresh();

        if (logOnClear)
        {
            Debug.Log("RankingDisplay: ランキングの記録を消しました", this);
        }
    }

    public void Refresh()
    {
        if (rankingText == null) return;

        rankingText.text = ScoreRanking.BuildText(
            displayCount, 0, lineFormat, emptyMark, string.Empty, Color.white);
    }
}
