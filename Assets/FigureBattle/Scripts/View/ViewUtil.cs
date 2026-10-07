using UnityEngine;

namespace FigureBattle
{
    public static class ViewUtil
    {
        static Shader litShader;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public static Material NewMaterial(Color color, Material template = null)
        {
            Material m;
            if (template != null) m = new Material(template);
            else
            {
                if (litShader == null)
                    litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(litShader);
            }
            SetColor(m, color);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
        }

        // 当たり判定を消したプリミティブを作る（クリック判定は親でまとめて取る）
        public static GameObject Primitive(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static string ClassName(FigureClass c)
        {
            switch (c)
            {
                case FigureClass.Soldier: return "ソルジャー";
                case FigureClass.Sniper: return "スナイパー";
                case FigureClass.Jet: return "ジェット";
                case FigureClass.Breaker: return "ブレイカー";
            }
            return c.ToString();
        }

        public static string MapName(MapType t)
        {
            switch (t)
            {
                case MapType.Mountain: return "山岳";
                case MapType.Plains: return "平原";
                case MapType.Desert: return "砂漠";
                case MapType.Forest: return "森林";
            }
            return t.ToString();
        }

        public static string StageName(StageRule r)
        {
            switch (r)
            {
                case StageRule.Space: return "宇宙（行動範囲4/3倍）";
                case StageRule.SnowMountain: return "雪山（行動範囲2/3倍）";
                case StageRule.Kinkakuji: return "金閣寺（必要空想力2倍）";
                case StageRule.Ginkakuji: return "銀閣寺（必要空想力1/2倍）";
                case StageRule.Versailles: return "ヴェルサイユ宮殿（相性倍率3倍）";
                case StageRule.TeaCup: return "ティーカップ（相性が逆）";
                case StageRule.LeaningTower: return "ピザの斜塔（左にしか動けない）";
                case StageRule.Dojo: return "剣道場（ソルジャーのパワー2倍）";
            }
            return "なし";
        }

        public static Color ClassColor(FigureClass c)
        {
            switch (c)
            {
                case FigureClass.Soldier: return new Color(0.85f, 0.85f, 0.88f);
                case FigureClass.Sniper: return new Color(0.35f, 0.75f, 0.45f);
                case FigureClass.Jet: return new Color(0.95f, 0.75f, 0.25f);
                case FigureClass.Breaker: return new Color(0.55f, 0.4f, 0.75f);
            }
            return Color.white;
        }

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.UC: return new Color(0.4f, 0.85f, 0.4f);
                case Rarity.R: return new Color(0.3f, 0.6f, 1f);
                case Rarity.SR: return new Color(0.75f, 0.4f, 1f);
                case Rarity.UR: return new Color(1f, 0.55f, 0.15f);
                case Rarity.GR: return new Color(1f, 0.85f, 0.2f);
            }
            return new Color(0.6f, 0.6f, 0.6f);
        }

        public static Color PlayerColor(int player)
        {
            return player == 0 ? new Color(0.25f, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f);
        }
    }
}
