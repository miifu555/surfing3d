using UnityEngine;
using UnityEngine.UI;

namespace Surfing3D.Scoring
{
    /// <summary>
    /// スコアを画面に表示する簡易 HUD。
    /// ラベルを割り当てていない場合は実行時に Canvas ごと自動生成する。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Surfing3D/Score HUD")]
    public class ScoreHud : MonoBehaviour
    {
        [Header("Labels")]
        [Tooltip("スコア表示用ラベル。未設定なら自動生成する")]
        [SerializeField] Text m_ScoreLabel;

        [Tooltip("取得時に \"+10\" のように出るラベル。未設定なら自動生成する")]
        [SerializeField] Text m_PopupLabel;

        [Tooltip("スコア表示のフォーマット（{0} に数値が入る）")]
        [SerializeField] string m_Format = "SCORE {0}";

        [Header("Options")]
        [Tooltip("ラベル未設定のときに Canvas を自動生成する")]
        [SerializeField] bool m_CreateUIIfMissing = true;

        [Tooltip("加算ポップアップの表示時間（秒）")]
        [SerializeField] float m_PopupDuration = 0.9f;

        ScoreManager m_Manager;
        float m_PopupTimer;
        GameObject m_RuntimeCanvas;

        void OnEnable()
        {
            if (m_CreateUIIfMissing && (m_ScoreLabel == null || m_PopupLabel == null))
            {
                BuildRuntimeUI();
            }

            m_Manager = ScoreManager.Ensure();
            m_Manager.ScoreChanged += HandleScoreChanged;
            Refresh(m_Manager.Score);

            if (m_PopupLabel != null)
            {
                SetPopupAlpha(0f);
            }
        }

        void OnDisable()
        {
            if (m_Manager != null)
            {
                m_Manager.ScoreChanged -= HandleScoreChanged;
                m_Manager = null;
            }
        }

        void OnDestroy()
        {
            if (m_RuntimeCanvas != null)
            {
                Destroy(m_RuntimeCanvas);
                m_RuntimeCanvas = null;
            }
        }

        void Update()
        {
            if (m_PopupLabel == null || m_PopupTimer <= 0f)
            {
                return;
            }

            m_PopupTimer -= Time.deltaTime;
            float t = m_PopupDuration > 0f ? Mathf.Clamp01(m_PopupTimer / m_PopupDuration) : 0f;
            SetPopupAlpha(t);

            var rect = m_PopupLabel.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -80f + (1f - t) * 30f);

            if (m_PopupTimer <= 0f)
            {
                SetPopupAlpha(0f);
            }
        }

        void HandleScoreChanged(int total, int delta)
        {
            Refresh(total);

            if (delta > 0 && m_PopupLabel != null)
            {
                m_PopupLabel.text = "+" + delta;
                m_PopupTimer = m_PopupDuration;
            }
        }

        void Refresh(int total)
        {
            if (m_ScoreLabel != null)
            {
                m_ScoreLabel.text = string.Format(m_Format, total);
            }
        }

        void SetPopupAlpha(float alpha)
        {
            var c = m_PopupLabel.color;
            c.a = alpha;
            m_PopupLabel.color = c;
        }

        void BuildRuntimeUI()
        {
            m_RuntimeCanvas = new GameObject("Score HUD Canvas", typeof(RectTransform));

            var canvas = m_RuntimeCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = m_RuntimeCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            m_RuntimeCanvas.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (m_ScoreLabel == null)
            {
                m_ScoreLabel = CreateLabel("Score Label", m_RuntimeCanvas.transform, font, 48, new Vector2(0f, -24f));
            }

            if (m_PopupLabel == null)
            {
                m_PopupLabel = CreateLabel("Score Popup", m_RuntimeCanvas.transform, font, 36, new Vector2(0f, -80f));
                m_PopupLabel.color = new Color(1f, 0.92f, 0.4f, 0f);
                m_PopupLabel.text = string.Empty;
            }
        }

        static Text CreateLabel(string name, Transform parent, Font font, int fontSize, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(600f, 70f);
            rect.anchoredPosition = anchoredPosition;

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var shadow = go.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(2f, -2f);

            return text;
        }
    }
}
