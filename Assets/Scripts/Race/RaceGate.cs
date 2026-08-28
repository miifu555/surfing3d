using UnityEngine;

namespace Surfing3D.Race
{
    /// <summary>
    /// スタート兼ゴールのゲート。
    /// 最初の通過で制限時間のカウントダウンが始まり、そのときテープが切れる。
    /// そのあとゴールテープとして張り直され、次に通過するとゴール（ボーナススコア）になる。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Race Gate")]
    public class RaceGate : MonoBehaviour
    {
        [Header("Tape")]
        [Tooltip("スタート／ゴールのテープ")]
        [SerializeField] GoalTape m_Tape;

        [Tooltip("スタート後、ゴールテープを張り直すまでの待ち時間（秒）")]
        [SerializeField] float m_TapeRespawnDelay = 1.2f;

        [Tooltip("OFF にするとスタート後にテープを張り直さない")]
        [SerializeField] bool m_ShowGoalTape = true;

        [Header("Detection")]
        [Tooltip("通過を判定するタグ。空文字なら何が通っても判定する")]
        [SerializeField] string m_PlayerTag = "Player";

        [Tooltip("連続で判定されないようにする待ち時間（秒）")]
        [SerializeField] float m_RetriggerCooldown = 3f;

        float m_LastPassTime = float.NegativeInfinity;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        void Start()
        {
            var race = RaceManager.Ensure();

            if (m_Tape != null)
            {
                // スタート前はスタートテープとして張っておく
                if (race.State == RaceState.Ready)
                {
                    m_Tape.Show(false);
                }
                else
                {
                    m_Tape.Hide();
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!TriggerFilter.Matches(other, m_PlayerTag))
            {
                return;
            }

            if (Time.time - m_LastPassTime < m_RetriggerCooldown)
            {
                return;
            }

            var race = RaceManager.Ensure();

            switch (race.State)
            {
                case RaceState.Ready:
                    m_LastPassTime = Time.time;
                    race.StartRace();

                    if (m_Tape != null)
                    {
                        m_Tape.Break();

                        // 一度切れたテープを、今度はゴールテープとして張り直す
                        if (m_ShowGoalTape)
                        {
                            m_Tape.ShowAfter(m_TapeRespawnDelay);
                        }
                    }
                    break;

                case RaceState.Racing:
                    m_LastPassTime = Time.time;
                    race.ReachGoal();

                    if (m_Tape != null)
                    {
                        m_Tape.Break();
                    }
                    break;
            }
        }
    }
}
