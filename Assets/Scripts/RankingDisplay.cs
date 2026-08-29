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

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (rankingText == null) return;

        rankingText.text = ScoreRanking.BuildText(
            displayCount, 0, lineFormat, emptyMark, string.Empty, Color.white);
    }
}
