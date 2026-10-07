using System;
using UnityEngine;

namespace FigureBattle
{
    // フィギュア1体ぶんの性能。攻撃力・体力・スキル（クラス）の3要素でバランスを取る
    [Serializable]
    public class FigureSpec
    {
        public string figureName = "フィギュア";
        public FigureClass figureClass = FigureClass.Soldier;
        public Rarity rarity = Rarity.C;
        // パワー（攻撃力）
        public int power = 3000;
        // ライフ（体力）
        public int life = 3000;
        // 行動範囲（移動と攻撃の両方に使うマス数）
        public int range = 2;
        // 場に出すのに必要な空想力
        public int cost = 2;
        // 見た目のモデル。空なら仮の形を使う
        public GameObject modelPrefab;

        public FigureSpec Clone()
        {
            return (FigureSpec)MemberwiseClone();
        }

        public static FigureSpec Create(string name, FigureClass cls, int power, int life, int range, int cost, Rarity rarity = Rarity.C)
        {
            return new FigureSpec
            {
                figureName = name,
                figureClass = cls,
                power = power,
                life = life,
                range = range,
                cost = cost,
                rarity = rarity,
            };
        }
    }
}
