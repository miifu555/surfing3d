using UnityEngine;
using UnityEngine.UI;

namespace Surfing3D.UI
{
    /// <summary>
    /// HUD 用の Canvas とラベルをコードから生成するヘルパー。
    /// Canvas はシーンに 1 つだけ作られ、各 HUD で共有される。
    /// </summary>
    public static class HudCanvas
    {
        const string CanvasName = "Surfing3D HUD Canvas";

        static Canvas s_Canvas;

        /// <summary>共有の HUD Canvas を取得する。無ければ作る。</summary>
        public static Canvas GetOrCreate()
        {
            if (s_Canvas != null)
            {
                return s_Canvas;
            }

            var go = new GameObject(CanvasName, typeof(RectTransform));

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            s_Canvas = canvas;
            return s_Canvas;
        }

        /// <summary>HUD ラベルを 1 つ作る。</summary>
        /// <param name="name">GameObject 名</param>
        /// <param name="anchor">アンカー兼ピボット（(0,0)=左下 / (1,1)=右上）</param>
        /// <param name="anchoredPosition">アンカーからのオフセット</param>
        /// <param name="size">ラベルの大きさ</param>
        /// <param name="fontSize">フォントサイズ</param>
        /// <param name="alignment">文字揃え</param>
        public static Text CreateLabel(string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var canvas = GetOrCreate();

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);

            return text;
        }
    }
}
