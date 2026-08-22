using UnityEngine;

namespace Surfing3D.Race
{
    /// <summary>
    /// スタート／ゴールのテープ。表示・非表示と、通過したときに切れる演出を担当する。
    /// テープは左右 2 枚に分かれていて、それぞれのピボット（支柱側）を回転させて開く。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Goal Tape")]
    public class GoalTape : MonoBehaviour
    {
        [Header("Halves")]
        [Tooltip("左半分の回転支点（左の支柱の位置）")]
        [SerializeField] Transform m_LeftPivot;

        [Tooltip("右半分の回転支点（右の支柱の位置）")]
        [SerializeField] Transform m_RightPivot;

        [Header("Materials")]
        [Tooltip("スタートテープのマテリアル（任意）")]
        [SerializeField] Material m_StartMaterial;

        [Tooltip("ゴールテープのマテリアル（任意。未設定ならスタートと同じ見た目）")]
        [SerializeField] Material m_GoalMaterial;

        [Header("Break")]
        [Tooltip("テープが切れる演出の長さ（秒）")]
        [SerializeField] float m_BreakDuration = 0.7f;

        [Tooltip("切れたテープが開く角度")]
        [SerializeField] float m_BreakSwingAngle = 75f;

        [Tooltip("切れたテープが下がる距離")]
        [SerializeField] float m_BreakDrop = 0.5f;

        Renderer[] m_Renderers;
        Quaternion m_LeftRotation;
        Quaternion m_RightRotation;
        Vector3 m_LeftPosition;
        Vector3 m_RightPosition;
        float m_BreakTimer;
        bool m_Breaking;
        bool m_Visible = true;

        /// <summary>テープが張られているか（切れた後・非表示中は false）。</summary>
        public bool IsVisible => m_Visible;

        void Awake()
        {
            m_Renderers = GetComponentsInChildren<Renderer>(true);

            if (m_LeftPivot != null)
            {
                m_LeftRotation = m_LeftPivot.localRotation;
                m_LeftPosition = m_LeftPivot.localPosition;
            }

            if (m_RightPivot != null)
            {
                m_RightRotation = m_RightPivot.localRotation;
                m_RightPosition = m_RightPivot.localPosition;
            }
        }

        void Update()
        {
            if (!m_Breaking)
            {
                return;
            }

            m_BreakTimer += Time.deltaTime;

            float t = m_BreakDuration > 0f ? Mathf.Clamp01(m_BreakTimer / m_BreakDuration) : 1f;
            float ease = t * t;

            ApplyBreakPose(ease);

            if (t >= 1f)
            {
                m_Breaking = false;
                Hide();
            }
        }

        /// <summary>テープを張る。asGoal が true ならゴール用のマテリアルを使う。</summary>
        public void Show(bool asGoal)
        {
            CancelInvoke(nameof(ShowAsGoal));

            m_Breaking = false;
            m_BreakTimer = 0f;
            ApplyBreakPose(0f);

            var material = asGoal && m_GoalMaterial != null ? m_GoalMaterial : m_StartMaterial;
            if (material != null)
            {
                SetMaterial(material);
            }

            SetRenderersEnabled(true);
            m_Visible = true;
        }

        /// <summary>テープを消す（演出なし）。</summary>
        public void Hide()
        {
            CancelInvoke(nameof(ShowAsGoal));

            m_Breaking = false;
            m_BreakTimer = 0f;

            SetRenderersEnabled(false);
            m_Visible = false;
        }

        /// <summary>テープを切る。演出が終わると自動的に消える。</summary>
        public void Break()
        {
            if (!m_Visible || m_Breaking)
            {
                return;
            }

            m_Breaking = true;
            m_BreakTimer = 0f;
        }

        /// <summary>指定秒後にゴールテープとして張り直す。</summary>
        public void ShowAfter(float delay)
        {
            CancelInvoke(nameof(ShowAsGoal));

            if (delay <= 0f)
            {
                Show(true);
                return;
            }

            Invoke(nameof(ShowAsGoal), delay);
        }

        void ShowAsGoal()
        {
            Show(true);
        }

        void ApplyBreakPose(float amount)
        {
            if (m_LeftPivot != null)
            {
                m_LeftPivot.localRotation = m_LeftRotation * Quaternion.Euler(0f, 0f, -m_BreakSwingAngle * amount);
                m_LeftPivot.localPosition = m_LeftPosition + Vector3.down * (m_BreakDrop * amount);
            }

            if (m_RightPivot != null)
            {
                m_RightPivot.localRotation = m_RightRotation * Quaternion.Euler(0f, 0f, m_BreakSwingAngle * amount);
                m_RightPivot.localPosition = m_RightPosition + Vector3.down * (m_BreakDrop * amount);
            }
        }

        void SetRenderersEnabled(bool value)
        {
            if (m_Renderers == null)
            {
                return;
            }

            for (int i = 0; i < m_Renderers.Length; i++)
            {
                if (m_Renderers[i] != null)
                {
                    m_Renderers[i].enabled = value;
                }
            }
        }

        void SetMaterial(Material material)
        {
            if (m_Renderers == null)
            {
                return;
            }

            for (int i = 0; i < m_Renderers.Length; i++)
            {
                if (m_Renderers[i] != null)
                {
                    m_Renderers[i].sharedMaterial = material;
                }
            }
        }
    }
}
