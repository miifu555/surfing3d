using System;
using UnityEngine;

namespace Surfing3D.Race
{
    /// <summary>レースの進行状態。</summary>
    public enum RaceState
    {
        /// <summary>スタート前</summary>
        Ready,

        /// <summary>走行中</summary>
        Racing,

        /// <summary>ゴール済み</summary>
        Finished,
    }

    /// <summary>
    /// ラップ数とレース状態を管理するシングルトン。
    /// シーンに置かなくても Ensure() が呼ばれた時点で自動生成される。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Race Manager")]
    public class RaceManager : MonoBehaviour
    {
        static RaceManager s_Instance;

        [Tooltip("周回数")]
        [Min(1)]
        [SerializeField] int m_TotalLaps = 3;

        [Tooltip("シーンをまたいでレース状態を保持する")]
        [SerializeField] bool m_PersistAcrossScenes = false;

        int m_CurrentLap;
        RaceState m_State = RaceState.Ready;

        /// <summary>現在有効な RaceManager。まだ無ければ null。</summary>
        public static RaceManager Instance => s_Instance;

        /// <summary>周回数（3 ラップ制なら 3）。</summary>
        public int TotalLaps => Mathf.Max(1, m_TotalLaps);

        /// <summary>現在のラップ。スタート前は 0。</summary>
        public int CurrentLap => m_CurrentLap;

        /// <summary>現在のレース状態。</summary>
        public RaceState State => m_State;

        /// <summary>最終ラップを走行中かどうか。</summary>
        public bool IsFinalLap => m_State == RaceState.Racing && m_CurrentLap >= TotalLaps;

        /// <summary>スタートラインを最初に通過したとき。</summary>
        public event Action RaceStarted;

        /// <summary>ラップ表示を更新すべきとき。引数は (現在のラップ, 総ラップ数)。</summary>
        public event Action<int, int> LapChanged;

        /// <summary>1 周終えたとき。引数は終えたラップ番号。</summary>
        public event Action<int> LapCompleted;

        /// <summary>最終ラップを終えてゴールしたとき。</summary>
        public event Action RaceFinished;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Debug.LogWarning("[RaceManager] 既に別の RaceManager が存在するため、このコンポーネントを無効化します。", this);
                Destroy(this);
                return;
            }

            s_Instance = this;

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

        /// <summary>レースを開始する（スタートライン通過時に呼ばれる）。</summary>
        public void StartRace()
        {
            if (m_State != RaceState.Ready)
            {
                return;
            }

            m_State = RaceState.Racing;
            m_CurrentLap = 1;

            RaceStarted?.Invoke();
            LapChanged?.Invoke(m_CurrentLap, TotalLaps);
        }

        /// <summary>1 周ぶんの通過を記録する。最終ラップならゴール扱いになる。</summary>
        public void CompleteLap()
        {
            if (m_State != RaceState.Racing)
            {
                return;
            }

            int completed = m_CurrentLap;
            LapCompleted?.Invoke(completed);

            if (completed >= TotalLaps)
            {
                m_State = RaceState.Finished;
                RaceFinished?.Invoke();
                return;
            }

            m_CurrentLap = completed + 1;
            LapChanged?.Invoke(m_CurrentLap, TotalLaps);
        }

        /// <summary>スタート前の状態に戻す。</summary>
        public void ResetRace()
        {
            m_State = RaceState.Ready;
            m_CurrentLap = 0;
            LapChanged?.Invoke(m_CurrentLap, TotalLaps);
        }
    }
}
