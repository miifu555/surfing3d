using System.Collections;
using UnityEngine;
using TMPro;

public class RaceCountdown : MonoBehaviour
{
    // カウントダウンが終わるまで false。debugmover はこれを見て動きを止める。
    // このコンポーネントが無いシーンでも動かせるよう、初期値は true にしてある。
    public static bool raceStarted = true;

    // インスペクターで指定するのはこちら（非static）
    [SerializeField]
    private TextMeshPro countdownTextField;

    [Header("カウントダウン設定")]
    // いくつから数え始めるか
    public int countFrom = 3;
    // 1カウントあたりの秒数
    public float countInterval = 1.0f;
    // 数字の代わりに最後に出す文字
    public string goText = "GO!";
    // 「GO!」を表示しておく秒数。この時間が過ぎたらオブジェクトを消す
    public float goDisplayTime = 1.0f;

    [Header("後始末")]
    // カウントが終わったあとに消すオブジェクト。
    // 未設定ならテキストが乗っているGameObjectを消す。
    public GameObject destroyTarget;

    void Awake()
    {
        // このコンポーネントがある場合だけスタートを止める。
        // static はシーンを開き直しても値が残るので、ここで必ずリセットする。
        raceStarted = false;
    }

    void Start()
    {
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        // 3, 2, 1, ... と数える
        for (int i = countFrom; i > 0; i--)
        {
            SetText(i.ToString());
            yield return new WaitForSeconds(countInterval);
        }

        // ここで操作可能になる
        SetText(goText);
        raceStarted = true;

        yield return new WaitForSeconds(goDisplayTime);

        DestroyCountdownObject();
    }

    void SetText(string text)
    {
        if (countdownTextField != null)
        {
            countdownTextField.text = text;
        }
    }

    // 表示に使ったオブジェクトごと消す
    void DestroyCountdownObject()
    {
        GameObject target = destroyTarget;

        if (target == null && countdownTextField != null)
        {
            target = countdownTextField.gameObject;
        }

        if (target != null)
        {
            Destroy(target);
        }
    }
}
