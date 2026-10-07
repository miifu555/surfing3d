using UnityEngine;

namespace FigureBattle
{
    // 盤面の1マスの見た目。クリック判定もここで受ける
    public class TileView : MonoBehaviour
    {
        public GridPos Pos;
        // フィギュアを立たせる高さ
        public float TopY;

        Material mat;
        Color baseColor;

        public void Init(GridPos pos, Color color, float topY, Material material)
        {
            Pos = pos;
            TopY = topY;
            baseColor = color;
            mat = material;
            ViewUtil.SetColor(mat, color);
        }

        public void SetHighlight(Color? c)
        {
            ViewUtil.SetColor(mat, c.HasValue ? Color.Lerp(baseColor, c.Value, 0.8f) : baseColor);
        }
    }
}
