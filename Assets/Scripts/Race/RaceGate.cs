using UnityEngine;

namespace Surfing3D.Race
{
    /// <summary>
    /// スタート兼ゴールのゲート。
    /// 最初の通過でレース開始（スタートテープが切れる）、以降は通過するたびに 1 周としてカウントし、
    /// 最終ラップに入るとゴールテープとしてもう一度テープが張られる。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Race Gate")]
    public class RaceGate : MonoBehaviour
    {
        [Header("Tape")]
        [Tooltip("スタート／ゴールのテープ")]
        [SerializeField] GoalTape m_Tape;

        [Tooltip("テープを張り直すまでの待ち時間（秒）")]
        [SerializeField] float m_TapeRespawnDelay = 1.2f;

        [Tooltip("ON にすると毎ラップ、OFF なら最終ラップだけテープを張り直す")]
        [SerializeField] bool m_ShowTapeEveryLap = false;

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
            if (race.State == RaceState.Finished)
            {
                return;
            }

            m_LastPassTime = Time.time;

            if (race.State == RaceState.Ready)
            {
                race.StartRace();
            }
            else
            {
                race.CompleteLap();
            }

            if (m_Tape != null)
            {
                m_Tape.Break();

                if (ShouldShowTape(race))
                {
                    m_Tape.ShowAfter(m_TapeRespawnDelay);
                }
            }
        }

        bool ShouldShowTape(RaceManager race)
        {
            if (race.State != RaceState.Racing)
            {
                return false;
            }

            return m_ShowTapeEveryLap || race.IsFinalLap;
        }
    }
}
