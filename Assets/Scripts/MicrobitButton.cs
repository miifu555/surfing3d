using UnityEngine;

// micro:bit のボタンを「押された瞬間」だけ拾うための小物。
//
// reciever.botton_mark は押している間ずっと同じ値が入り続けるので、
// そのまま見ると押しっぱなしのままシーンが切り替わったときに、
// 次の画面でも即座に反応してしまう。
// 前回の値と比べて、変化した瞬間だけ true を返すようにする。
public class MicrobitButton
{
    // 直前に見たボタンの状態
    private string last = "N";
    // この時刻までは反応しない（画面が変わった直後の押しっぱなし対策）
    private float ignoreUntil = 0.0f;

    // ignoreSeconds: 作られてから何秒間、入力を受け付けないか
    public MicrobitButton(float ignoreSeconds)
    {
        // 今まさに押されている状態から始めても反応しないよう、
        // 現在の値を「見たこと」にしておく
        last = reciever.botton_mark;

        // timeScale を 0 にしていても進むよう unscaled を使う
        ignoreUntil = Time.unscaledTime + ignoreSeconds;
    }

    // mark（"A" や "B"）が押された瞬間なら true
    public bool GetDown(string mark)
    {
        string current = reciever.botton_mark;
        bool down = (current == mark && last != mark);
        return Finish(current, down);
    }

    // marks のどれかが押された瞬間なら true。
    // GetDown を2回呼ぶと last が二重に進んで押下を取りこぼすので、
    // 複数のボタンを見たいときは必ずこちらを使う。
    public bool GetDownAny(string[] marks)
    {
        string current = reciever.botton_mark;
        bool down = false;

        if (marks != null && current != last)
        {
            for (int i = 0; i < marks.Length; i++)
            {
                if (current == marks[i])
                {
                    down = true;
                    break;
                }
            }
        }

        return Finish(current, down);
    }

    // 状態の更新と猶予の判定。GetDown / GetDownAny で共通
    bool Finish(string current, bool down)
    {
        // 受け付けない間も状態は追いかける。
        // そうしないと、猶予が明けた瞬間に古い押下が立ってしまう。
        last = current;

        if (Time.unscaledTime < ignoreUntil)
        {
            return false;
        }

        return down;
    }
}
