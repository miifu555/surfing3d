using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FigureBattle.Tests
{
    public class BattleEngineTests
    {
        static FigureSpec Fig(FigureClass c, int power = 3000, int life = 3000, int range = 2, int cost = 1, string name = null)
        {
            return FigureSpec.Create(name ?? c.ToString(), c, power, life, range, cost);
        }

        // 障害物の無い盤面で、配置フェーズを終えた状態のエンジンを作る
        static BattleEngine Setup(List<FigureSpec> deck0, List<FigureSpec> deck1, GridPos p0, GridPos p1, BattleRulesConfig config = null)
        {
            config = config ?? new BattleRulesConfig();
            var board = new Board(config.boardWidth, config.boardHeight, config.safeRows);
            var e = new BattleEngine(config, board, deck0, deck1, 0);
            Assert.IsTrue(e.Place(e.HandUnits(0).First(), p0));
            Assert.IsTrue(e.Place(e.HandUnits(1).First(), p1));
            Assert.AreEqual(BattlePhase.Battle, e.Phase);
            Assert.AreEqual(0, e.CurrentPlayer);
            return e;
        }

        // 配置したあとに盤上の好きな位置へ直接動かす（テスト用）
        static FigureUnit Field(BattleEngine e, int player) => e.FieldUnits(player).First();

        [Test]
        public void Affinity_Cycle()
        {
            Assert.IsTrue(BattleRules.HasAdvantage(FigureClass.Jet, FigureClass.Sniper));
            Assert.IsTrue(BattleRules.HasAdvantage(FigureClass.Sniper, FigureClass.Breaker));
            Assert.IsTrue(BattleRules.HasAdvantage(FigureClass.Breaker, FigureClass.Soldier));
            Assert.IsTrue(BattleRules.HasAdvantage(FigureClass.Soldier, FigureClass.Jet));
            Assert.IsFalse(BattleRules.HasAdvantage(FigureClass.Sniper, FigureClass.Jet));
        }

        [Test]
        public void Power_MustExceedLife()
        {
            var cfg = new BattleRulesConfig();
            Assert.IsFalse(BattleRules.WouldDefeat(Fig(FigureClass.Soldier, 3000), Fig(FigureClass.Soldier, life: 3000), cfg));
            Assert.IsTrue(BattleRules.WouldDefeat(Fig(FigureClass.Soldier, 3001), Fig(FigureClass.Soldier, life: 3000), cfg));
            // 相性有利なら2倍
            Assert.IsTrue(BattleRules.WouldDefeat(Fig(FigureClass.Soldier, 2000), Fig(FigureClass.Jet, life: 3000), cfg));
            cfg.stageRule = StageRule.TeaCup;
            Assert.IsFalse(BattleRules.WouldDefeat(Fig(FigureClass.Soldier, 2000), Fig(FigureClass.Jet, life: 3000), cfg));
        }

        [Test]
        public void Placement_AlternatesAndStartsBattle()
        {
            var cfg = new BattleRulesConfig();
            var board = new Board(cfg.boardWidth, cfg.boardHeight, cfg.safeRows);
            var e = new BattleEngine(cfg, board, new List<FigureSpec> { Fig(FigureClass.Soldier) }, new List<FigureSpec> { Fig(FigureClass.Soldier) }, 1);
            Assert.AreEqual(1, e.CurrentPlayer);
            // セーフエリアの外には置けない
            Assert.IsFalse(e.Place(e.HandUnits(1).First(), new GridPos(0, 0)));
            Assert.IsTrue(e.Place(e.HandUnits(1).First(), new GridPos(0, 8)));
            Assert.AreEqual(0, e.CurrentPlayer);
            Assert.IsTrue(e.Place(e.HandUnits(0).First(), new GridPos(0, 0)));
            Assert.AreEqual(BattlePhase.Battle, e.Phase);
            Assert.AreEqual(1, e.CurrentPlayer);
        }

        [Test]
        public void Move_LimitedByRange()
        {
            var e = Setup(new List<FigureSpec> { Fig(FigureClass.Soldier, range: 2) }, new List<FigureSpec> { Fig(FigureClass.Soldier) },
                new GridPos(0, 1), new GridPos(6, 8));
            var u = Field(e, 0);
            var opts = e.GetMoveOptions(u);
            Assert.IsTrue(opts.All(o => o.Pos.Manhattan(u.Pos) <= 2));
            Assert.IsTrue(opts.Any(o => o.Pos == new GridPos(0, 3)));
            Assert.IsTrue(e.Move(u, new GridPos(0, 3)));
            Assert.AreEqual(ActState.Moved, u.State);
            // 1ターンに移動は1回
            Assert.IsFalse(e.Move(u, new GridPos(0, 4)));
        }

        [Test]
        public void Jet_BoostedMove_CannotAttack()
        {
            var e = Setup(new List<FigureSpec> { Fig(FigureClass.Jet, range: 2) }, new List<FigureSpec> { Fig(FigureClass.Soldier) },
                new GridPos(0, 1), new GridPos(6, 8));
            var u = Field(e, 0);
            var opts = e.GetMoveOptions(u);
            Assert.IsTrue(opts.Any(o => o.Pos == new GridPos(0, 5) && o.Boosted));
            Assert.IsTrue(opts.Any(o => o.Pos == new GridPos(0, 3) && !o.Boosted));
            Assert.IsTrue(e.Move(u, new GridPos(0, 5)));
            Assert.AreEqual(ActState.Done, u.State);
            Assert.IsEmpty(e.GetAttackTargets(u));
        }

        [Test]
        public void Sniper_DoubleRangeOnHighGround()
        {
            var cfg = new BattleRulesConfig();
            var e = Setup(new List<FigureSpec> { Fig(FigureClass.Sniper, range: 2) }, new List<FigureSpec> { Fig(FigureClass.Soldier) },
                new GridPos(0, 1), new GridPos(6, 8), cfg);
            var u = Field(e, 0);
            Assert.AreEqual(2, e.AttackRange(u, new GridPos(0, 4)));
            e.Board.Set(new GridPos(0, 4), TileType.HighGround);
            Assert.AreEqual(4, e.AttackRange(u, new GridPos(0, 4)));
        }

        [Test]
        public void Attack_DefeatAddsLifeAndReturnsToHand()
        {
            var e = Setup(new List<FigureSpec> { Fig(FigureClass.Soldier, power: 5000, range: 3) },
                new List<FigureSpec> { Fig(FigureClass.Soldier, life: 3000), Fig(FigureClass.Soldier) },
                new GridPos(3, 1), new GridPos(3, 7));
            var me = Field(e, 0);
            var enemy = Field(e, 1);
            Assert.IsTrue(e.Move(me, new GridPos(3, 4)));
            Assert.IsTrue(e.Attack(me, enemy.Pos));
            Assert.IsFalse(enemy.OnField);
            Assert.IsTrue(enemy.InHand);
            Assert.AreEqual(3000, e.Players[0].DefeatedLifeTotal);
        }

        [Test]
        public void Attack_BlockedWhenPowerTooLow()
        {
            var e = Setup(new List<FigureSpec> { Fig(FigureClass.Soldier, power: 1000, range: 3) },
                new List<FigureSpec> { Fig(FigureClass.Soldier, life: 3000) },
                new GridPos(3, 1), new GridPos(3, 7));
            var me = Field(e, 0);
            var enemy = Field(e, 1);
            e.Move(me, new GridPos(3, 4));
            Assert.IsTrue(e.Attack(me, enemy.Pos));
            Assert.IsTrue(enemy.OnField);
            Assert.AreEqual(ActState.Done, me.State);
        }

        [Test]
        public void Win_ByLifeTotal()
        {
            var cfg = new BattleRulesConfig { lifeTotalToWin = 3000 };
            var e = Setup(new List<FigureSpec> { Fig(FigureClass.Soldier, power: 5000, range: 3) },
                new List<FigureSpec> { Fig(FigureClass.Soldier, life: 3000), Fig(FigureClass.Soldier) },
                new GridPos(3, 1), new GridPos(3, 7), cfg);
            var me = Field(e, 0);
            e.Move(me, new GridPos(3, 4));
            e.Attack(me, Field(e, 1).Pos);
            Assert.AreEqual(BattlePhase.Finished, e.Phase);
            Assert.AreEqual(0, e.Winner);
            Assert.AreEqual(WinReason.LifeTotal, e.WinReason);
        }

        [Test]
        public void Barrier_ThreeHitsOrOneBreaker()
        {
            var cfg = new BattleRulesConfig();
            var e = Setup(new List<FigureSpec> { Fig(FigureClass.Soldier, range: 3) }, new List<FigureSpec> { Fig(FigureClass.Soldier) },
                new GridPos(0, 1), new GridPos(0, 8), cfg);
            var me = Field(e, 0);
            var barrier = e.Board.BarrierPos[1];
            me.Pos = new GridPos(barrier.x, barrier.y - 2);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(e.Attack(me, barrier), $"hit {i}");
                if (i < 2)
                {
                    e.EndTurn();
                    e.EndTurn();
                }
            }
            Assert.IsTrue(e.Players[1].BarrierBroken);
            Assert.AreEqual(WinReason.BarrierBreak, e.WinReason);

            var e2 = Setup(new List<FigureSpec> { Fig(FigureClass.Breaker, range: 1) }, new List<FigureSpec> { Fig(FigureClass.Soldier) },
                new GridPos(0, 1), new GridPos(0, 8));
            var br = Field(e2, 0);
            br.Pos = new GridPos(barrier.x, barrier.y - 1);
            Assert.IsTrue(e2.Attack(br, barrier));
            Assert.AreEqual(0, e2.Winner);
        }

        [Test]
        public void Summon_CostsImaginationAndRespectsFieldLimit()
        {
            var cfg = new BattleRulesConfig { startImagination = 10, maxFiguresOnField = 2 };
            var deck0 = new List<FigureSpec> { Fig(FigureClass.Soldier), Fig(FigureClass.Sniper, cost: 3), Fig(FigureClass.Jet, cost: 1) };
            var e = Setup(deck0, new List<FigureSpec> { Fig(FigureClass.Soldier) }, new GridPos(0, 0), new GridPos(6, 8), cfg);
            var sniper = e.HandUnits(0).First(u => u.Spec.figureClass == FigureClass.Sniper);
            int before = e.Players[0].Imagination;
            Assert.IsTrue(e.Summon(sniper, new GridPos(1, 0)));
            Assert.AreEqual(before - 3, e.Players[0].Imagination);
            // 出したターンは行動できない
            Assert.AreEqual(ActState.Done, sniper.State);
            var jet = e.HandUnits(0).First();
            Assert.IsFalse(e.Summon(jet, new GridPos(2, 0)), "場の上限");
            // セーフエリア外には出せない
            Assert.IsFalse(e.Summon(jet, new GridPos(2, 4)));
        }

        [Test]
        public void TurnStart_GainsImagination()
        {
            var cfg = new BattleRulesConfig { startImagination = 0, imaginationPerTurn = 1 };
            var e = Setup(new List<FigureSpec> { Fig(FigureClass.Soldier) }, new List<FigureSpec> { Fig(FigureClass.Soldier) },
                new GridPos(0, 0), new GridPos(6, 8), cfg);
            Assert.AreEqual(1, e.Players[0].Imagination);
            e.EndTurn();
            Assert.AreEqual(1, e.Players[1].Imagination);
            e.EndTurn();
            Assert.AreEqual(2, e.Players[0].Imagination);
        }

        [Test]
        public void MapGenerator_IsSymmetricAndKeepsSafeAreaClear()
        {
            var cfg = new BattleRulesConfig();
            foreach (MapType t in System.Enum.GetValues(typeof(MapType)))
            {
                var b = MapGenerator.Generate(t, new System.Random(42), cfg);
                foreach (var p in b.AllPositions())
                {
                    var m = new GridPos(b.Width - 1 - p.x, b.Height - 1 - p.y);
                    Assert.AreEqual(b.Get(p), b.Get(m));
                    if (b.IsSafeArea(0, p) || b.IsSafeArea(1, p)) Assert.AreEqual(TileType.Normal, b.Get(p));
                }
            }
        }

        // どのマップでも CPU 同士で決着がつく（にらみ合いで止まらない）
        [Test]
        public void AI_FinishesGames_OnAllMaps([Values] MapType map, [Values(1, 2, 3, 4, 5)] int seed)
        {
            var cfg = new BattleRulesConfig();
            var rng = new System.Random(seed);
            var board = MapGenerator.Generate(map, rng, cfg);
            var e = new BattleEngine(cfg, board, DefaultFigures.DeckA(), DefaultFigures.DeckB(), seed % 2);
            var ai0 = new SimpleAI(seed * 10 + 1);
            var ai1 = new SimpleAI(seed * 10 + 2);
            int steps = 0;
            while (e.Phase != BattlePhase.Finished && steps < 20000 && e.TurnNumber < 150)
            {
                var ai = e.CurrentPlayer == 0 ? ai0 : ai1;
                var a = ai.Next(e, e.CurrentPlayer);
                switch (a.Type)
                {
                    case AIActionType.Place: Assert.IsTrue(e.Place(a.Unit, a.Pos)); break;
                    case AIActionType.Summon: Assert.IsTrue(e.Summon(a.Unit, a.Pos)); break;
                    case AIActionType.Move: Assert.IsTrue(e.Move(a.Unit, a.Pos)); break;
                    case AIActionType.Attack: Assert.IsTrue(e.Attack(a.Unit, a.Pos)); break;
                    case AIActionType.Rest: e.Rest(a.Unit); break;
                    case AIActionType.EndTurn: e.EndTurn(); break;
                    default: Assert.Fail("AI returned no action"); break;
                }
                steps++;
            }
            Assert.AreEqual(BattlePhase.Finished, e.Phase, $"決着がつかなかった（ターン{e.TurnNumber}）");
        }
    }
}
