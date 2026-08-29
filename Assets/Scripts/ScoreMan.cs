using UnityEngine;
using TMPro;

public class ScoreMan : MonoBehaviour
{
    public static int score;

    // インスペクターで指定するのはこちら（非static）
    [SerializeField]
    private TextMeshProUGUI scoretextField;

    // 他のスクリプトから ScoreMan.scoretext として使う用（static）
    public static TextMeshProUGUI scoretext;

    void Awake()
    {
        // インスペクターで設定した値を static 変数にコピー
        scoretext = scoretextField;

        // static はシーンを移っても値が残るので、ここでリセットする。
        // これが無いとタイトルからやり直しても前回のスコアが残ってしまう。
        score = 0;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (scoretext != null)
        {
            scoretext.text = "Score:0";
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static void getScore(int gotscore)
    {
        score += gotscore;

        if (scoretext != null)
        {
            scoretext.text = "Score:" + score.ToString();
        }
    }
}