using UnityEngine;

// スコアの上位記録を端末に保存しておく（PlayerPrefs）。
// シーンを移っても、ゲームを終了しても残る。
public static class ScoreRanking
{
    // 何位まで残すか
    public const int MaxEntries = 5;

    const string KeyPrefix = "surfing3d.ranking.";

    // 上位から順に並んだスコア。記録が無いところは 0
    public static int[] GetScores()
    {
        int[] scores = new int[MaxEntries];

        for (int i = 0; i < MaxEntries; i++)
        {
            scores[i] = PlayerPrefs.GetInt(KeyPrefix + i, 0);
        }

        return scores;
    }

    // 記録を登録して、入った順位（1始まり）を返す。ランク外なら 0
    public static int Submit(int score)
    {
        if (score <= 0) return 0;

        int[] scores = GetScores();

        int rank = 0;
        for (int i = 0; i < MaxEntries; i++)
        {
            if (score > scores[i])
            {
                rank = i + 1;
                break;
            }
        }

        if (rank == 0) return 0;

        // 入る場所より下を1つずつ後ろへずらす
        for (int i = MaxEntries - 1; i > rank - 1; i--)
        {
            scores[i] = scores[i - 1];
        }
        scores[rank - 1] = score;

        Save(scores);
        return rank;
    }

    // ランキングを1つのテキストにまとめる。リザルト画面とタイトル画面で共用する。
    // count      : 何位まで出すか
    // highlightRank: 強調したい順位（1始まり）。0なら強調なし
    public static string BuildText(int count, int highlightRank,
                                   string lineFormat, string emptyMark,
                                   string highlightSuffix, Color highlightColor)
    {
        int[] scores = GetScores();
        int max = Mathf.Clamp(count, 1, MaxEntries);
        string hex = ColorUtility.ToHtmlStringRGB(highlightColor);

        var builder = new System.Text.StringBuilder();

        for (int i = 0; i < max; i++)
        {
            string value = (scores[i] > 0) ? scores[i].ToString() : emptyMark;
            string line = string.Format(lineFormat, i + 1, value);

            if (i + 1 == highlightRank)
            {
                line = "<color=#" + hex + ">" + line + highlightSuffix + "</color>";
            }

            builder.AppendLine(line);
        }

        return builder.ToString();
    }

    // 記録をすべて消す
    public static void Clear()
    {
        for (int i = 0; i < MaxEntries; i++)
        {
            PlayerPrefs.DeleteKey(KeyPrefix + i);
        }
        PlayerPrefs.Save();
    }

    static void Save(int[] scores)
    {
        for (int i = 0; i < MaxEntries; i++)
        {
            PlayerPrefs.SetInt(KeyPrefix + i, scores[i]);
        }
        PlayerPrefs.Save();
    }
}
