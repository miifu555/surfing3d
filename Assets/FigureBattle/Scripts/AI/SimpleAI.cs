using System;
using System.Collections.Generic;
using System.Linq;

namespace FigureBattle
{
    public enum AIActionType { None, Place, Summon, Move, Attack, Rest, EndTurn }

    public struct AIAction
    {
        public AIActionType Type;
        public FigureUnit Unit;
        public GridPos Pos;
    }

    // CPU 対戦用の簡単な思考。呼ぶたびに次の1手だけを返す
    public class SimpleAI
    {
        const int WinValue = 100000;
        readonly Random rng;

        public SimpleAI(int seed) { rng = new Random(seed); }

        public AIAction Next(BattleEngine e, int me)
        {
            if (e.CurrentPlayer != me || e.Phase == BattlePhase.Finished) return default;
            if (e.Phase == BattlePhase.Placement) return NextPlacement(e, me);

            // 1. 出せるフィギュアがあれば出す（コストの高いものから）
            var summon = e.HandUnits(me).Where(e.CanSummonAny).OrderByDescending(u => u.Spec.cost).FirstOrDefault();
            if (summon != null)
            {
                var tile = BestFrontTile(e, me);
                if (tile.HasValue) return new AIAction { Type = AIActionType.Summon, Unit = summon, Pos = tile.Value };
            }

            // 2. 行動できるフィギュアを1体ずつ動かす
            foreach (var u in e.FieldUnits(me).Where(u => u.State != ActState.Done).ToList())
            {
                var act = DecideUnit(e, u);
                if (act.Type != AIActionType.None) return act;
            }
            return new AIAction { Type = AIActionType.EndTurn };
        }

        AIAction NextPlacement(BattleEngine e, int me)
        {
            var unit = e.HandUnits(me).OrderByDescending(u => u.Spec.life).First();
            var tile = BestFrontTile(e, me);
            return new AIAction { Type = AIActionType.Place, Unit = unit, Pos = tile ?? default };
        }

        AIAction DecideUnit(BattleEngine e, FigureUnit u)
        {
            int curBest = BestTarget(e, u, u.Pos, out var curTarget);

            if (u.State == ActState.Moved)
            {
                return curBest > 0
                    ? new AIAction { Type = AIActionType.Attack, Unit = u, Pos = curTarget }
                    : new AIAction { Type = AIActionType.Rest, Unit = u };
            }

            // 移動してから攻撃したほうが得なマスを探す
            var moves = e.GetMoveOptions(u);
            int bestMoveVal = 0;
            GridPos? bestMovePos = null;
            foreach (var m in moves.Where(m => !m.Boosted))
            {
                int v = BestTarget(e, u, m.Pos, out _) - DangerPenalty(e, u, m.Pos);
                if (v > bestMoveVal) { bestMoveVal = v; bestMovePos = m.Pos; }
            }

            if (curBest > 0 && curBest - DangerPenalty(e, u, u.Pos) >= bestMoveVal)
                return new AIAction { Type = AIActionType.Attack, Unit = u, Pos = curTarget };
            if (bestMovePos.HasValue)
                return new AIAction { Type = AIActionType.Move, Unit = u, Pos = bestMovePos.Value };

            // 攻撃できる相手がいなければ、なるべく安全に相手のバリアへ近づく
            // 距離は障害物を回り込んだ道のりで測る（味方が詰まって動けなくなるのを防ぐ）
            var dist = PathDistances(e, e.Board.BarrierPos[BattleEngine.Opponent(u.Owner)]);
            int Score(GridPos p) => -(dist.TryGetValue(p, out var d) ? d : 999) * 100 - DangerPenalty(e, u, p);
            int stayScore = Score(u.Pos);
            var best = moves.OrderByDescending(m => Score(m.Pos) + rng.Next(10)).FirstOrDefault();
            // しばらく動きが無いときは、同じ距離のマスへも動いて詰まりをほぐす
            bool stalled = e.TurnNumber - e.LastProgressTurn >= StallTurns / 2;
            if (moves.Count > 0 && (Score(best.Pos) > stayScore || (stalled && Score(best.Pos) == stayScore)))
                return new AIAction { Type = AIActionType.Move, Unit = u, Pos = best.Pos };
            // 細い道で倒せない相手と向かい合ったままにならないよう、ときどき寄り道する
            if (moves.Count > 0 && e.TurnNumber - e.LastProgressTurn >= StallTurns && rng.Next(3) == 0)
                return new AIAction { Type = AIActionType.Move, Unit = u, Pos = moves[rng.Next(moves.Count)].Pos };
            return new AIAction { Type = AIActionType.Rest, Unit = u };
        }

        // 危険度をどれくらい気にするか。大きいほど強気に前へ出る
        const int DangerDivisor = 20;
        // この数のターン、誰も倒せずバリアにも当たらなければ危険を気にせず攻めに行く
        const int StallTurns = 8;

        static int DangerPenalty(BattleEngine e, FigureUnit u, GridPos p)
        {
            int idle = e.TurnNumber - e.LastProgressTurn;
            if (idle >= StallTurns) return 0;
            return Danger(e, u, p) * (StallTurns - idle) / (StallTurns * DangerDivisor);
        }

        // target から各マスまでの道のり（フィギュアは無視して地形だけで測る）
        static Dictionary<GridPos, int> PathDistances(BattleEngine e, GridPos target)
        {
            var dist = new Dictionary<GridPos, int> { [target] = 0 };
            var queue = new Queue<GridPos>();
            queue.Enqueue(target);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var dir in GridPos.Dirs)
                {
                    var n = c + dir;
                    if (dist.ContainsKey(n) || !e.IsWalkable(n)) continue;
                    dist[n] = dist[c] + 1;
                    queue.Enqueue(n);
                }
            }
            return dist;
        }

        // from から攻撃したときの一番いい相手とその価値。何もできないなら 0
        int BestTarget(BattleEngine e, FigureUnit u, GridPos from, out GridPos targetPos)
        {
            int best = 0;
            targetPos = default;
            var me = e.Players[u.Owner];
            foreach (var t in e.GetAttackTargetsFrom(u, from))
            {
                int v;
                if (t.IsBarrier)
                {
                    var owner = e.Players[t.BarrierOwner];
                    bool breaks = u.Spec.figureClass == FigureClass.Breaker || owner.BarrierHits + 1 >= e.Config.barrierHitsToBreak;
                    v = breaks ? WinValue : 2000;
                }
                else if (BattleRules.WouldDefeat(u.Spec, t.Unit.Spec, e.Config))
                {
                    v = me.DefeatedLifeTotal + t.Unit.Spec.life >= e.Config.lifeTotalToWin ? WinValue : t.Unit.Spec.life;
                }
                else continue;

                if (v > best) { best = v; targetPos = t.Pos; }
            }
            return best;
        }

        // p にいると次の相手ターンに倒されそうか（ざっくり）
        static int Danger(BattleEngine e, FigureUnit u, GridPos p)
        {
            int danger = 0;
            foreach (var enemy in e.FieldUnits(BattleEngine.Opponent(u.Owner)))
            {
                int reach = BattleRules.EffectiveRange(enemy.Spec, e.Config) + e.AttackRange(enemy, enemy.Pos);
                if (enemy.Pos.Manhattan(p) <= reach && BattleRules.WouldDefeat(enemy.Spec, u.Spec, e.Config))
                    danger += u.Spec.life;
            }
            return danger;
        }

        // 相手のバリアに一番近いセーフエリアの空きマス
        GridPos? BestFrontTile(BattleEngine e, int me)
        {
            var target = e.Board.BarrierPos[BattleEngine.Opponent(me)];
            var tiles = e.SummonTiles(me).ToList();
            if (tiles.Count == 0) return null;
            return tiles.OrderBy(p => p.Manhattan(target) * 10 + rng.Next(10)).First();
        }
    }
}
