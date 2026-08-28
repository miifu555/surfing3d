using System;
using Surfing3D.Scoring;
using UnityEngine;

namespace Surfing3D.Race
{
    /// <summary>レースの進行状態。</summary>
    public enum RaceState
    {
        /// <summary>スタート前</summary>
        Ready,

        /// <summary>走行中（制限時間をカウントダウン中）</summary>
        Racing,

        /// <summary>終了（ゴール、または時間切れ）</summary>
        Finished,
    }

    /// <summary>
    /// 制限時間とゴールを管理するシングルトン。
    /// スタートゲートを通過するとカウントダウンが始まり、
    /// 時間内にゴールできればボーナススコアが入る。時間切れならボーナス無しで終了。
    /// シーンに置かなくても Ensure() が呼ばれた時点で自動生成される。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Race Manager")]
    public class RaceManager : MonoBehaviour
    {
        static RaceManager s_Instance;

        [Header("Rule")]
        [Tooltip("制限時間（秒）")]
        [Min(1f)]
        [SerializeField] float m_TimeLimit = 90f;

        [Tooltip("ゴールしたときに加算されるボーナススコア")]
        [Min(0)]
        [SerializeField] int m_GoalBonus = 1000;

        [Tooltip("残り時間がこの秒数以下になったら「残りわずか」と扱う")]
        [Min(0f)]
        [SerializeField] float m_HurryUpTime = 10f;

        [Header("Options")]
        [Tooltip("シーンをまたいで状態を保持する")]
        [SerializeField] bool m_PersistAcrossScenes = false;

        float m_RemainingTime;
        RaceState m_State = RaceState.Ready;
        bool m_ReachedGoal;
        bool m_HurryUpFired;

        /// <summary>現在有効な RaceManager。まだ無ければ null。</summary>
        public static RaceManager Instance => s_Instance;

        /// <summary>制限時間（秒）。</summary>
        public float TimeLimit => Mathf.Max(1f, m_TimeLimit);

        /// <summary>残り時間（秒）。</summary>
        public float RemainingTime => m_RemainingTime;

        /// <summary>ゴールしたときに入るボーナススコア。</summary>
        public int GoalBonus => Mathf.Max(0, m_GoalBonus);

        /// <summary>現在の状態。</summary>
        public RaceState State => m_State;

        /// <summary>ゴールして終わったかどうか（時間切れなら false）。</summary>
        public bool ReachedGoal => m_ReachedGoal;

        /// <summary>残り時間がわずかかどうか。</summary>
        public bool IsHurryUp => m_State == RaceState.Racing && m_RemainingTime <= m_HurryUpTime;

        /// <summary>スタートゲートを通過してカウントダウンが始まったとき。</summary>
        public event Action RaceStarted;

        /// <summary>残り時間が変化したとき。引数は残り秒数。</summary>
        public event Action<float> TimeChanged;

        /// <summary>残り時間が少なくなったとき（1 回だけ）。</summary>
        public event Action HurryUp;

        /// <summary>終了したとき。引数は (ゴールできたか, 加算されたボーナス)。</summary>
        public event Action<bool, int> RaceFinished;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Debug.LogWarning("[RaceManager] 既に別の RaceManager が存在するため、このコンポーネントを無効化します。", this);
                Destroy(this);
                return;
            }

            s_Instance = this;
            m_RemainingTime = TimeLimit;

            if (m_PersistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        void Update()
        {
            if (m_State != RaceState.Racing)
            {
                return;
            }

            m_RemainingTime -= Time.deltaTime;

            if (m_RemainingTime <= 0f)
            {
                m_RemainingTime = 0f;
                TimeChanged?.Invoke(0f);
                Finish(false);
                return;
            }

            TimeChanged?.Invoke(m_RemainingTime);

            if (!m_HurryUpFired && m_RemainingTime <= m_HurryUpTime)
            {
                m_HurryUpFired = true;
                HurryUp?.Invoke();
            }
        }

        /// <summary>RaceManager を取得する。シーンに無い場合は自動で作成する。</summary>
        public static RaceManager Ensure()
        {
            if (s_Instance != null)
            {
                return s_Instance;
            }

            var found = FindFirstObjectByType<RaceManager>(FindObjectsInactive.Exclude);
            if (found != null)
            {
                s_Instance = found;
                return s_Instance;
            }

            var go = new GameObject("RaceManager (auto)");
            return go.AddComponent<RaceManager>();
        }

        /// <summary>カウントダウンを開始する（スタートゲート通過時に呼ばれる）。</summary>
        public void StartRace()
        {
            if (m_State != RaceState.Ready)
            {
                return;
            }

            m_State = RaceState.Racing;
            m_RemainingTime = TimeLimit;
            m_ReachedGoal = false;
            m_HurryUpFired = false;

            RaceStarted?.Invoke();
            TimeChanged?.Invoke(m_RemainingTime);
        }

        /// <summary>ゴールする。ボーナススコアが加算されて終了する。</summary>
        public void ReachGoal()
        {
            if (m_State != RaceState.Racing)
            {
                return;
            }

            Finish(true);
        }

        void Finish(bool reachedGoal)
        {
            m_State = RaceState.Finished;
            m_ReachedGoal = reachedGoal;

            int bonus = 0;
            if (reachedGoal && GoalBonus > 0)
            {
                bonus = GoalBonus;
                ScoreManager.Ensure().AddScore(bonus);
            }

            RaceFinished?.Invoke(reachedGoal, bonus);
        }

        /// <summary>スタート前の状態に戻す。</summary>
        public void ResetRace()
        {
            m_State = RaceState.Ready;
            m_ReachedGoal = false;
            m_HurryUpFired = false;
            m_RemainingTime = TimeLimit;

            TimeChanged?.Invoke(m_RemainingTime);
        }
    }
}
