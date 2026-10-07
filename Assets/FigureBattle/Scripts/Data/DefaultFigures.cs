using System.Collections.Generic;

namespace FigureBattle
{
    // デッキアセットが設定されていないときに使う仮のフィギュア
    public static class DefaultFigures
    {
        public static List<FigureSpec> DeckA()
        {
            return new List<FigureSpec>
            {
                FigureSpec.Create("ブレイドナイト", FigureClass.Soldier, 3000, 3000, 2, 2, Rarity.R),
                FigureSpec.Create("ロングアイ", FigureClass.Sniper, 2500, 2000, 2, 2, Rarity.UC),
                FigureSpec.Create("スカイファルコン", FigureClass.Jet, 2000, 2500, 2, 2, Rarity.SR),
                FigureSpec.Create("アイアンゴーレム", FigureClass.Breaker, 4000, 5000, 1, 3, Rarity.C),
            };
        }

        public static List<FigureSpec> DeckB()
        {
            return new List<FigureSpec>
            {
                FigureSpec.Create("ランサー", FigureClass.Soldier, 3500, 2500, 2, 2, Rarity.C),
                FigureSpec.Create("ホークショット", FigureClass.Sniper, 3000, 1500, 2, 2, Rarity.UR),
                FigureSpec.Create("ストームウィング", FigureClass.Jet, 2500, 2000, 2, 2, Rarity.R),
                FigureSpec.Create("クラッシャー", FigureClass.Breaker, 4500, 4000, 1, 3, Rarity.GR),
            };
        }
    }
}
