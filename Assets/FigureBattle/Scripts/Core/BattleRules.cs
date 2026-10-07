using UnityEngine;

namespace FigureBattle
{
    // 数値計算だけをまとめたもの（盤面の状態は持たない）
    public static class BattleRules
    {
        // a が b に有利か。ジェット→スナイパー→ブレイカー→ソルジャー→ジェット
        public static bool HasAdvantage(FigureClass a, FigureClass b)
        {
            switch (a)
            {
                case FigureClass.Jet: return b == FigureClass.Sniper;
                case FigureClass.Sniper: return b == FigureClass.Breaker;
                case FigureClass.Breaker: return b == FigureClass.Soldier;
                case FigureClass.Soldier: return b == FigureClass.Jet;
            }
            return false;
        }

        // ステージカード込みの行動範囲
        public static int EffectiveRange(FigureSpec spec, BattleRulesConfig config)
        {
            float r = spec.range;
            if (config.stageRule == StageRule.Space) r *= 4f / 3f;
            else if (config.stageRule == StageRule.SnowMountain) r *= 2f / 3f;
            return Mathf.Max(1, Mathf.RoundToInt(r));
        }

        // 攻撃するときのパワー（相性・ステージ込み）
        public static int AttackPower(FigureSpec attacker, FigureSpec defender, BattleRulesConfig config)
        {
            int power = attacker.power;
            if (config.stageRule == StageRule.Dojo && attacker.figureClass == FigureClass.Soldier)
                power *= 2;

            bool advantage = config.stageRule == StageRule.TeaCup
                ? HasAdvantage(defender.figureClass, attacker.figureClass)
                : HasAdvantage(attacker.figureClass, defender.figureClass);
            if (advantage)
                power *= config.stageRule == StageRule.Versailles ? 3 : config.affinityMultiplier;
            return power;
        }

        public static bool WouldDefeat(FigureSpec attacker, FigureSpec defender, BattleRulesConfig config)
        {
            int power = AttackPower(attacker, defender, config);
            return config.defeatOnEqual ? power >= defender.life : power > defender.life;
        }

        public static int SummonCost(FigureSpec spec, BattleRulesConfig config)
        {
            if (config.stageRule == StageRule.Kinkakuji) return spec.cost * 2;
            if (config.stageRule == StageRule.Ginkakuji) return Mathf.CeilToInt(spec.cost * 0.5f);
            return spec.cost;
        }
    }
}
