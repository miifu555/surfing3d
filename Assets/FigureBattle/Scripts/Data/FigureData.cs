using UnityEngine;

namespace FigureBattle
{
    // フィギュアのデータアセット。Project ウィンドウの右クリック → Create → FigureBattle → Figure で作れる
    [CreateAssetMenu(menuName = "FigureBattle/Figure", fileName = "NewFigure")]
    public class FigureData : ScriptableObject
    {
        public FigureSpec spec = new FigureSpec();
    }
}
