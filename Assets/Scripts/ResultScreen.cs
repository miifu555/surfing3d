using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// 時間切れのあとに出すリザルト画面。
// 合計スコアとゴール回数を出して、もう一度遊ぶかタイトルへ戻るかを選ばせる。
public class ResultScreen : MonoBehaviour
{
    [Header("参照")]
    // リザルト全体をまとめた入れ物。最初は非表示にしておく
    public GameObject panel;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI goalText;
    public TextMeshProUGUI rankingText;
    // リザルトを出すときに隠すもの（プレイ中のスコア表示や残り時間表示）
    public GameObject[] hideOnShow;

    [Header("表示")]
    // {0} に合計スコアが入る
    public string scoreFormat = "SCORE  {0}";
    // {0} にゴール回数が入る
    public string goalFormat = "GOAL  x{0}";

    [Header("ランキング")]
    // {0} に順位、{1} にスコアが入る
    public string rankingLineFormat = "{0}.  {1}";
    // まだ記録が無い行に出す文字
    public string rankingEmptyMark = "- - -";
    // 今回の記録の行に付ける印
    public string newRecordSuffix = "   NEW!";
    // 今回の記録の行の色
    public Color newRecordColor = new Color(1.0f, 0.83f, 0.3f, 1.0f);

    [Header("シーン")]
    public string gameSceneName = "sunset";
    public string titleSceneName = "title";

    [Header("キー操作")]
    public bool useKeys = true;
    public KeyCode retryKey = KeyCode.Space;
    public KeyCode titleKey = KeyCode.Escape;

    // リザルトを出したあとか
    private bool shown = false;
    // シーンの二重読み込み防止
    private bool loading = false;

    void Awake()
    {
        shown = false;
        loading = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // RaceManager から呼ぶ
    public void Show(int score, int goalCount)
    {
        shown = true;

        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, score);
        }

        if (goalText != null)
        {
            goalText.text = string.Format(goalFormat, goalCount);
        }

        // 記録を登録してからランキングを組み立てる
        int rank = ScoreRanking.Submit(score);
        BuildRanking(rank);

        // プレイ中の表示は邪魔になるので隠す
        if (hideOnShow != null)
        {
            for (int i = 0; i < hideOnShow.Length; i++)
            {
                if (hideOnShow[i] != null) hideOnShow[i].SetActive(false);
            }
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    // 上位記録の一覧を1つのテキストにまとめる。
    // 今回入った行だけ色を変えて印を付ける。
    void BuildRanking(int newRank)
    {
        if (rankingText == null) return;

        rankingText.text = ScoreRanking.BuildText(
            ScoreRanking.MaxEntries, newRank,
            rankingLineFormat, rankingEmptyMark, newRecordSuffix, newRecordColor);
    }

    void Update()
    {
        if (shown == false) return;
        if (useKeys == false) return;
        if (loading) return;

        if (Input.GetKeyDown(retryKey))
        {
            Retry();
        }
        else if (Input.GetKeyDown(titleKey))
        {
            BackToTitle();
        }
    }

    // ボタンの OnClick から呼ぶ
    public void Retry()
    {
        Load(gameSceneName);
    }

    // ボタンの OnClick から呼ぶ
    public void BackToTitle()
    {
        Load(titleSceneName);
    }

    void Load(string sceneName)
    {
        if (loading) return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("ResultScreen: シーン名が空です");
            return;
        }

        loading = true;
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(sceneName);
    }
}
