using System;
using UnityEngine;

namespace Surfing3D.Scoring
{
    /// <summary>
    /// スコアを保持・加算するシングルトン。
    /// シーンに置かなくても Ensure() が呼ばれた時点で自動生成される。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Score Manager")]
    public class ScoreManager : MonoBehaviour
    {
        static ScoreManager s_Instance;

        [Tooltip("ゲーム開始時のスコア")]
        [SerializeField] int m_StartingScore = 0;

        [Tooltip("シーンをまたいでスコアを保持する")]
        [SerializeField] bool m_PersistAcrossScenes = false;

        int m_Score;

        /// <summary>現在有効な ScoreManager。まだ無ければ null。</summary>
        public static ScoreManager Instance => s_Instance;

        /// <summary>現在のスコア。</summary>
        public int Score => m_Score;

        /// <summary>スコアが変化したときに呼ばれる。引数は (合計スコア, 増減分)。</summary>
        public event Action<int, int> ScoreChanged;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Debug.LogWarning("[ScoreManager] 既に別の ScoreManager が存在するため、このコンポーネントを無効化します。", this);
                Destroy(this);
                return;
            }

            s_Instance = this;
            m_Score = m_StartingScore;

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

        /// <summary>
        /// ScoreManager を取得する。シーンに無い場合は自動で作成する。
        /// </summary>
        public static ScoreManager Ensure()
        {
            if (s_Instance != null)
            {
                return s_Instance;
            }

            var found = FindFirstObjectByType<ScoreManager>(FindObjectsInactive.Exclude);
            if (found != null)
            {
                s_Instance = found;
                return s_Instance;
            }

            var go = new GameObject("ScoreManager (auto)");
            return go.AddComponent<ScoreManager>();
        }

        /// <summary>スコアを加算する（マイナス値で減算）。</summary>
        public void AddScore(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            m_Score += amount;
            ScoreChanged?.Invoke(m_Score, amount);
        }

        /// <summary>スコアを開始値に戻す。</summary>
        public void ResetScore()
        {
            int delta = m_StartingScore - m_Score;
            m_Score = m_StartingScore;
            ScoreChanged?.Invoke(m_Score, delta);
        }

        /// <summary>どこからでも呼べる加算ショートカット。</summary>
        public static void Add(int amount)
        {
            Ensure().AddScore(amount);
        }
    }
}
