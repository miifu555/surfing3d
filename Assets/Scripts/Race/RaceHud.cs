using Surfing3D.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Surfing3D.Race
{
    /// <summary>
    /// 画面右上にラップ数（LAP 1/3）を表示する HUD。
    /// ラベル未設定なら実行時に自動生成する。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Race HUD")]
    public class RaceHud : MonoBehaviour
    {
        [Header("Labels")]
        [Tooltip("ラップ表示用ラベル。未設定なら右上に自動生成する")]
        [SerializeField] Text m_LapLabel;

        [Tooltip("「FINAL LAP!」などを出す中央のラベル。未設定なら自動生成する")]
        [SerializeField] Text m_MessageLabel;

        [Tooltip("ラップ表示のフォーマット（{0}=現在のラップ / {1}=総ラップ数）")]
        [SerializeField] string m_LapFormat = "LAP {0}/{1}";

        [Header("Messages")]
        [SerializeField] string m_StartMessage = "GO!";
        [SerializeField] string m_FinalLapMessage = "FINAL LAP!";
        [SerializeField] string m_FinishMessage = "FINISH!";

        [Tooltip("メッセージの表示時間（秒）")]
        [SerializeField] float m_MessageDuration = 2f;

        [Header("Options")]
        [Tooltip("ラベル未設定のときに UI を自動生成する")]
        [SerializeField] bool m_CreateUIIfMissing = true;

        RaceManager m_Race;
        float m_MessageTimer;
        bool m_KeepMessage;

        void OnEnable()
        {
            if (m_CreateUIIfMissing)
            {
                BuildRuntimeUI();
            }

            m_Race = RaceManager.Ensure();
            m_Race.RaceStarted += HandleRaceStarted;
            m_Race.LapChanged += HandleLapChanged;
            m_Race.RaceFinished += HandleRaceFinished;

            RefreshLapLabel(m_Race.CurrentLap, m_Race.TotalLaps);

            if (m_MessageLabel != null)
            {
                m_MessageLabel.text = string.Empty;
                SetMessageAlpha(0f);
            }
        }

        void OnDisable()
        {
            if (m_Race != null)
            {
                m_Race.RaceStarted -= HandleRaceStarted;
                m_Race.LapChanged -= HandleLapChanged;
                m_Race.RaceFinished -= HandleRaceFinished;
                m_Race = null;
            }
        }

        void Update()
        {
            if (m_MessageLabel == null || m_KeepMessage || m_MessageTimer <= 0f)
            {
                return;
            }

            m_MessageTimer -= Time.deltaTime;

            // 表示時間の後半でフェードアウトさせる
            float t = m_MessageDuration > 0f ? Mathf.Clamp01(m_MessageTimer / m_MessageDuration) : 0f;
            SetMessageAlpha(Mathf.Clamp01(t * 3f));

            if (m_MessageTimer <= 0f)
            {
                SetMessageAlpha(0f);
            }
        }

        void HandleRaceStarted()
        {
            ShowMessage(m_StartMessage, false);
        }

        void HandleLapChanged(int currentLap, int totalLaps)
        {
            RefreshLapLabel(currentLap, totalLaps);

            if (currentLap > 1 && currentLap >= totalLaps)
            {
                ShowMessage(m_FinalLapMessage, false);
            }
        }

        void HandleRaceFinished()
        {
            ShowMessage(m_FinishMessage, true);
        }

        void RefreshLapLabel(int currentLap, int totalLaps)
        {
            if (m_LapLabel == null)
            {
                return;
            }

            int shown = Mathf.Clamp(currentLap, 1, Mathf.Max(1, totalLaps));
            m_LapLabel.text = string.Format(m_LapFormat, shown, totalLaps);
        }

        /// <summary>中央にメッセージを出す。keep が true なら消さずに出しっぱなしにする。</summary>
        public void ShowMessage(string message, bool keep)
        {
            if (m_MessageLabel == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            m_MessageLabel.text = message;
            m_KeepMessage = keep;
            m_MessageTimer = keep ? 0f : m_MessageDuration;
            SetMessageAlpha(1f);
        }

        void SetMessageAlpha(float alpha)
        {
            var c = m_MessageLabel.color;
            c.a = alpha;
            m_MessageLabel.color = c;
        }

        void BuildRuntimeUI()
        {
            if (m_LapLabel == null)
            {
                m_LapLabel = HudCanvas.CreateLabel(
                    "Lap Label",
                    new Vector2(1f, 1f),
                    new Vector2(-32f, -24f),
                    new Vector2(360f, 70f),
                    48,
                    TextAnchor.MiddleRight);
            }

            if (m_MessageLabel == null)
            {
                m_MessageLabel = HudCanvas.CreateLabel(
                    "Race Message",
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 120f),
                    new Vector2(900f, 100f),
                    64,
                    TextAnchor.MiddleCenter);
                m_MessageLabel.color = new Color(1f, 0.95f, 0.55f, 0f);
            }
        }
    }
}
