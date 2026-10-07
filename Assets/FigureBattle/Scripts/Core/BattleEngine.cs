using System;
using System.Collections.Generic;
using System.Linq;

namespace FigureBattle
{
    // 盤上の1体のフィギュア
    public class FigureUnit
    {
        public readonly int Uid;
        public readonly int Owner;
        public readonly FigureSpec Spec;
        public GridPos Pos;
        public bool OnField;
        // 倒されて二度と出せない（defeatedReturnToHand が false のとき）
        public bool Retired;
        public ActState State = ActState.Done;

        public FigureUnit(int uid, int owner, FigureSpec spec)
        {
            Uid = uid;
            Owner = owner;
            Spec = spec;
        }

        public bool InHand => !OnField && !Retired;
        public override string ToString() => Spec.figureName;
    }

    public class PlayerState
    {
        public readonly int Index;
        public int Imagination;
        public int BarrierHits;
        public bool BarrierBroken;
        // 自分が倒した相手フィギュアのライフ合計
        public int DefeatedLifeTotal;
        public int PlacedCount;

        public PlayerState(int index) { Index = index; }
    }

    public struct MoveOption
    {
        public GridPos Pos;
        // ジェットの2倍移動（移動後は攻撃できない）
        public bool Boosted;
    }

    public struct AttackTarget
    {
        public GridPos Pos;
        // フィギュアを狙う場合
        public FigureUnit Unit;
        // バリアを狙う場合はその持ち主。違うなら -1
        public int BarrierOwner;
        public bool IsBarrier => BarrierOwner >= 0;
    }

    public enum BattleEventType
    {
        PhaseChanged, TurnStarted, Placed, Summoned, Moved,
        AttackDefeated, AttackBlocked, BarrierHit, BarrierBroken,
        UnitDone, GameOver,
    }

    public class BattleEvent
    {
        public BattleEventType Type;
        public FigureUnit Unit;
        public FigureUnit Target;
        public GridPos From;
        public GridPos To;
        public int Player = -1;
        public int Value;
        public string Message;
    }

    // バトルのルール本体。表示や入力には依存しない
    public class BattleEngine
    {
        public readonly BattleRulesConfig Config;
        public readonly Board Board;
        public readonly PlayerState[] Players = { new PlayerState(0), new PlayerState(1) };
        public readonly List<FigureUnit> Units = new List<FigureUnit>();
        public readonly List<string> Log = new List<string>();

        public BattlePhase Phase { get; private set; } = BattlePhase.Placement;
        public int FirstPlayer { get; private set; }
        public int CurrentPlayer { get; private set; }
        // 両プレイヤー合わせたターン数（1から）
        public int TurnNumber { get; private set; }
        public int Winner { get; private set; } = -1;
        // 最後にフィギュアを倒したかバリアに当てたターン（にらみ合いの判定用）
        public int LastProgressTurn { get; private set; }
        public WinReason WinReason { get; private set; }

        public event Action<BattleEvent> OnEvent;

        public BattleEngine(BattleRulesConfig config, Board board, IList<FigureSpec> deck0, IList<FigureSpec> deck1, int firstPlayer)
        {
            Config = config;
            Board = board;
            FirstPlayer = firstPlayer;
            CurrentPlayer = firstPlayer;
            int uid = 0;
            foreach (var s in deck0.Take(DeckData.MaxFigures)) Units.Add(new FigureUnit(uid++, 0, s.Clone()));
            foreach (var s in deck1.Take(DeckData.MaxFigures)) Units.Add(new FigureUnit(uid++, 1, s.Clone()));
            foreach (var p in Players) p.Imagination = config.startImagination;
            AddLog($"プレイヤー{firstPlayer + 1}が先攻です。セーフエリアにフィギュアを置いてください");
        }

        public static int Opponent(int player) => 1 - player;

        // ---------- 参照 ----------

        public IEnumerable<FigureUnit> FieldUnits(int player) => Units.Where(u => u.Owner == player && u.OnField);
        public IEnumerable<FigureUnit> HandUnits(int player) => Units.Where(u => u.Owner == player && u.InHand);

        public FigureUnit UnitAt(GridPos p) => Units.FirstOrDefault(u => u.OnField && u.Pos == p);

        public bool IsWalkable(GridPos p)
        {
            return Board.InBounds(p) && Board.Get(p) != TileType.Blocked && Board.BarrierOwnerAt(p) < 0;
        }

        public bool IsFree(GridPos p) => IsWalkable(p) && UnitAt(p) == null;

        // ---------- 配置フェーズ ----------

        public bool CanPlace(FigureUnit unit, GridPos p)
        {
            return Phase == BattlePhase.Placement && unit != null && unit.Owner == CurrentPlayer && unit.InHand
                && Board.IsSafeArea(unit.Owner, p) && IsFree(p);
        }

        public bool Place(FigureUnit unit, GridPos p)
        {
            if (!CanPlace(unit, p)) return false;
            PutOnField(unit, p);
            unit.State = ActState.Ready;
            Players[unit.Owner].PlacedCount++;
            AddLog($"P{unit.Owner + 1}：{unit}を配置");
            Emit(new BattleEvent { Type = BattleEventType.Placed, Unit = unit, To = p, Player = unit.Owner });

            // 先攻・後攻で交互に置き、全員置き終わったらバトル開始
            int other = Opponent(CurrentPlayer);
            if (NeedsPlacement(other)) CurrentPlayer = other;
            else if (!NeedsPlacement(CurrentPlayer)) StartBattle();
            return true;
        }

        bool NeedsPlacement(int player)
        {
            return Players[player].PlacedCount < Config.initialPlacementCount && HandUnits(player).Any();
        }

        void StartBattle()
        {
            Phase = BattlePhase.Battle;
            AddLog("バトル開始！");
            Emit(new BattleEvent { Type = BattleEventType.PhaseChanged });
            BeginTurn(FirstPlayer);
        }

        // ---------- バトルフェーズ ----------

        void BeginTurn(int player)
        {
            CurrentPlayer = player;
            TurnNumber++;
            var ps = Players[player];
            ps.Imagination = Math.Min(Config.maxImagination, ps.Imagination + Config.imaginationPerTurn);
            foreach (var u in Units)
                u.State = u.OnField && u.Owner == player ? ActState.Ready : ActState.Done;
            AddLog($"--- ターン{TurnNumber}：プレイヤー{player + 1} ---");
            Emit(new BattleEvent { Type = BattleEventType.TurnStarted, Player = player });
        }

        public void EndTurn()
        {
            if (Phase != BattlePhase.Battle) return;
            BeginTurn(Opponent(CurrentPlayer));
        }

        public bool IsMyTurn(int player) => Phase == BattlePhase.Battle && CurrentPlayer == player;

        public int SummonCost(FigureUnit unit) => BattleRules.SummonCost(unit.Spec, Config);

        public bool CanSummonAny(FigureUnit unit)
        {
            return Phase == BattlePhase.Battle && unit != null && unit.Owner == CurrentPlayer && unit.InHand
                && FieldUnits(CurrentPlayer).Count() < Config.maxFiguresOnField
                && Players[CurrentPlayer].Imagination >= SummonCost(unit);
        }

        public IEnumerable<GridPos> SummonTiles(int player)
        {
            return Board.AllPositions().Where(p => Board.IsSafeArea(player, p) && IsFree(p));
        }

        public bool CanSummon(FigureUnit unit, GridPos p) => CanSummonAny(unit) && Board.IsSafeArea(unit.Owner, p) && IsFree(p);

        public bool Summon(FigureUnit unit, GridPos p)
        {
            if (!CanSummon(unit, p)) return false;
            Players[unit.Owner].Imagination -= SummonCost(unit);
            PutOnField(unit, p);
            unit.State = Config.summonedCanActImmediately ? ActState.Ready : ActState.Done;
            AddLog($"P{unit.Owner + 1}：空想力{SummonCost(unit)}で{unit}を出した");
            Emit(new BattleEvent { Type = BattleEventType.Summoned, Unit = unit, To = p, Player = unit.Owner });
            return true;
        }

        // 移動できるマス（今いるマスは含まない）
        public List<MoveOption> GetMoveOptions(FigureUnit unit)
        {
            var result = new List<MoveOption>();
            if (!IsMyTurn(unit.Owner) || !unit.OnField || unit.State != ActState.Ready) return result;

            int range = BattleRules.EffectiveRange(unit.Spec, Config);
            int maxSteps = unit.Spec.figureClass == FigureClass.Jet ? range * Config.jetMoveMultiplier : range;

            // 幅優先で何歩で行けるかを調べる。他のフィギュアのいるマスは通れない
            var dist = new Dictionary<GridPos, int> { [unit.Pos] = 0 };
            var queue = new Queue<GridPos>();
            queue.Enqueue(unit.Pos);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                int d = dist[c];
                if (d >= maxSteps) continue;
                foreach (var dir in GridPos.Dirs)
                {
                    var n = c + dir;
                    if (dist.ContainsKey(n) || !IsFree(n)) continue;
                    dist[n] = d + 1;
                    queue.Enqueue(n);
                }
            }

            foreach (var kv in dist)
            {
                if (kv.Key == unit.Pos) continue;
                if (Config.stageRule == StageRule.LeaningTower && !IsLeftOf(unit.Owner, unit.Pos, kv.Key)) continue;
                result.Add(new MoveOption { Pos = kv.Key, Boosted = kv.Value > range });
            }
            return result;
        }

        // ピザの斜塔：プレイヤーから見て左（同じ列も可）にしか動けない
        static bool IsLeftOf(int player, GridPos from, GridPos to)
        {
            return player == 0 ? to.x <= from.x : to.x >= from.x;
        }

        public bool Move(FigureUnit unit, GridPos p)
        {
            var opts = GetMoveOptions(unit);
            int i = opts.FindIndex(o => o.Pos == p);
            if (i < 0) return false;
            var from = unit.Pos;
            unit.Pos = p;
            unit.State = opts[i].Boosted ? ActState.Done : ActState.Moved;
            AddLog(opts[i].Boosted ? $"P{unit.Owner + 1}：{unit}が高速移動（このターンは攻撃不可）" : $"P{unit.Owner + 1}：{unit}が移動");
            Emit(new BattleEvent { Type = BattleEventType.Moved, Unit = unit, From = from, To = p, Player = unit.Owner });
            return true;
        }

        public int AttackRange(FigureUnit unit, GridPos from)
        {
            int range = BattleRules.EffectiveRange(unit.Spec, Config);
            if (unit.Spec.figureClass == FigureClass.Sniper
                && (!Config.sniperNeedsHighGround || Board.Get(from) == TileType.HighGround))
                range *= Config.sniperRangeMultiplier;
            return range;
        }

        // from に立ったときに攻撃できる相手（AI の先読みにも使う）
        public List<AttackTarget> GetAttackTargetsFrom(FigureUnit unit, GridPos from)
        {
            var list = new List<AttackTarget>();
            int range = AttackRange(unit, from);
            int enemy = Opponent(unit.Owner);
            foreach (var e in FieldUnits(enemy))
                if (from.Manhattan(e.Pos) <= range)
                    list.Add(new AttackTarget { Pos = e.Pos, Unit = e, BarrierOwner = -1 });
            var bp = Board.BarrierPos[enemy];
            if (!Players[enemy].BarrierBroken && from.Manhattan(bp) <= range)
                list.Add(new AttackTarget { Pos = bp, BarrierOwner = enemy });
            return list;
        }

        public List<AttackTarget> GetAttackTargets(FigureUnit unit)
        {
            if (!IsMyTurn(unit.Owner) || !unit.OnField || unit.State == ActState.Done) return new List<AttackTarget>();
            return GetAttackTargetsFrom(unit, unit.Pos);
        }

        public bool Attack(FigureUnit unit, GridPos targetPos)
        {
            var targets = GetAttackTargets(unit);
            int i = targets.FindIndex(t => t.Pos == targetPos);
            if (i < 0) return false;
            var target = targets[i];
            unit.State = ActState.Done;
            int me = unit.Owner;

            if (target.IsBarrier)
            {
                var ps = Players[target.BarrierOwner];
                ps.BarrierHits = unit.Spec.figureClass == FigureClass.Breaker ? Config.barrierHitsToBreak : ps.BarrierHits + 1;
                LastProgressTurn = TurnNumber;
                if (ps.BarrierHits >= Config.barrierHitsToBreak)
                {
                    ps.BarrierBroken = true;
                    AddLog($"P{me + 1}：{unit}がバリアを破壊！");
                    Emit(new BattleEvent { Type = BattleEventType.BarrierBroken, Unit = unit, To = target.Pos, Player = target.BarrierOwner });
                    Finish(me, WinReason.BarrierBreak);
                }
                else
                {
                    AddLog($"P{me + 1}：{unit}がバリアを攻撃（{ps.BarrierHits}/{Config.barrierHitsToBreak}）");
                    Emit(new BattleEvent { Type = BattleEventType.BarrierHit, Unit = unit, To = target.Pos, Player = target.BarrierOwner, Value = ps.BarrierHits });
                }
                return true;
            }

            var victim = target.Unit;
            int power = BattleRules.AttackPower(unit.Spec, victim.Spec, Config);
            if (BattleRules.WouldDefeat(unit.Spec, victim.Spec, Config))
            {
                victim.OnField = false;
                victim.State = ActState.Done;
                if (!Config.defeatedReturnToHand) victim.Retired = true;
                Players[me].DefeatedLifeTotal += victim.Spec.life;
                LastProgressTurn = TurnNumber;
                AddLog($"P{me + 1}：{unit}（パワー{power}）が{victim}（ライフ{victim.Spec.life}）を倒した！");
                Emit(new BattleEvent { Type = BattleEventType.AttackDefeated, Unit = unit, Target = victim, To = target.Pos, Player = me, Value = power });

                if (Players[me].DefeatedLifeTotal >= Config.lifeTotalToWin)
                    Finish(me, WinReason.LifeTotal);
                else if (!FieldUnits(victim.Owner).Any() && !HandUnits(victim.Owner).Any())
                    Finish(me, WinReason.Annihilation);
            }
            else
            {
                AddLog($"P{me + 1}：{unit}（パワー{power}）の攻撃は{victim}（ライフ{victim.Spec.life}）に防がれた");
                Emit(new BattleEvent { Type = BattleEventType.AttackBlocked, Unit = unit, Target = victim, To = target.Pos, Player = me, Value = power });
            }
            return true;
        }

        // 攻撃せずに行動を終える
        public void Rest(FigureUnit unit)
        {
            if (!IsMyTurn(unit.Owner) || unit.State == ActState.Done) return;
            unit.State = ActState.Done;
            Emit(new BattleEvent { Type = BattleEventType.UnitDone, Unit = unit, Player = unit.Owner });
        }

        // ---------- 内部 ----------

        void PutOnField(FigureUnit unit, GridPos p)
        {
            unit.OnField = true;
            unit.Pos = p;
        }

        void Finish(int winner, WinReason reason)
        {
            Phase = BattlePhase.Finished;
            Winner = winner;
            WinReason = reason;
            AddLog($"プレイヤー{winner + 1}の勝利！");
            Emit(new BattleEvent { Type = BattleEventType.GameOver, Player = winner });
        }

        void AddLog(string s) => Log.Add(s);

        void Emit(BattleEvent e) => OnEvent?.Invoke(e);
    }
}
