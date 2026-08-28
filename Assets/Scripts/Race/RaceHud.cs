using Surfing3D.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Surfing3D.Race
{
    /// <summary>
    /// 画面右上に残り時間（TIME 1:30）を表示する HUD。
    /// ラベル未設定なら実行時に自動生成する。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Race HUD")]
    public class RaceHud : MonoBehaviour
    {
        [Header("Labels")]
        [Tooltip("残り時間の表示用ラベル。未設定なら右上に自動生成する")]
        [SerializeField] Text m_TimeLabel;

        [Tooltip("「GO!」などを出す中央のラベル。未設定なら自動生成する")]
        [SerializeField] Text m_MessageLabel;

        [Tooltip("残り時間のフォーマット（{0} に 1:30 のような文字列が入る）")]
        [SerializeField] string m_TimeFormat = "TIME {0}";

        [Header("Colors")]
        [SerializeField] Color m_NormalColor = Color.white;

        [Tooltip("残り時間がわずかなときの色")]
        [SerializeField] Color m_HurryColor = new Color(1f, 0.35f, 0.3f, 1f);

        [Header("Messages")]
        [SerializeField] string m_StartMessage = "GO!";
        [SerializeField] string m_HurryMessage = "HURRY UP!";

        [Tooltip("ゴール時のメッセージ（{0} にボーナススコアが入る）")]
        [SerializeField] string m_GoalMessage = "GOAL!  +{0}";

        [SerializeField] string m_TimeUpMessage = "TIME UP!";

        [Tooltip("メッセージの表示時間（秒）")]
        [SerializeField] float m_MessageDuration = 2f;

        [Header("Options")]
        [Tooltip("ラベル未設定のときに UI を自動生成する")]
        [SerializeField] bool m_CreateUIIfMissing = true;

        RaceManager m_Race;
        float m_MessageTimer;
        bool m_KeepMessage;
        int m_ShownSeconds = -1;

        void OnEnable()
        {
            if (m_CreateUIIfMissing)
            {
                BuildRuntimeUI();
            }

            m_Race = RaceManager.Ensure();
            m_Race.RaceStarted += HandleRaceStarted;
            m_Race.TimeChanged += HandleTimeChanged;
            m_Race.HurryUp += HandleHurryUp;
            m_Race.RaceFinished += HandleRaceFinished;

            m_ShownSeconds = -1;
            RefreshTimeLabel(m_Race.State == RaceState.Ready ? m_Race.TimeLimit : m_Race.RemainingTime);

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
                m_Race.TimeChanged -= HandleTimeChanged;
                m_Race.HurryUp -= HandleHurryUp;
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

            // 表示時間の終わりぎわでフェードアウトさせる
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

        void HandleTimeChanged(float remaining)
        {
            RefreshTimeLabel(remaining);
        }

        void HandleHurryUp()
        {
            ShowMessage(m_HurryMessage, false);
        }

        void HandleRaceFinished(bool reachedGoal, int bonus)
        {
            ShowMessage(reachedGoal ? string.Format(m_GoalMessage, bonus) : m_TimeUpMessage, true);
        }

        void RefreshTimeLabel(float remaining)
        {
            if (m_TimeLabel == null)
            {
                return;
            }

            int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
            if (seconds == m_ShownSeconds)
            {
                return;
            }

            m_ShownSeconds = seconds;
            m_TimeLabel.text = string.Format(m_TimeFormat, FormatTime(seconds));
            m_TimeLabel.color = m_Race != null && m_Race.IsHurryUp ? m_HurryColor : m_NormalColor;
        }

        /// <summary>秒数を 1:30 の形にする。</summary>
        public static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return minutes + ":" + seconds.ToString("00");
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
            if (m_TimeLabel == null)
            {
                m_TimeLabel = HudCanvas.CreateLabel(
                    "Time Label",
                    new Vector2(1f, 1f),
                    new Vector2(-32f, -24f),
                    new Vector2(360f, 70f),
                    48,
                    TextAnchor.MiddleRight);
                m_TimeLabel.color = m_NormalColor;
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
