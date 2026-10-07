using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FigureBattle
{
    // フィギュアバトルの進行役。盤面を組み立て、入力を受け、CPU を動かし、演出を順番に流す
    public partial class BattleController : MonoBehaviour
    {
        [Header("ルール")]
        public BattleRulesConfig rules = new BattleRulesConfig();
        // true ならマップの種類を毎回ランダムに決める
        public bool randomMap = true;
        public MapType mapType = MapType.Plains;
        // 0 なら毎回違うバトルになる
        public int randomSeed = 0;

        [Header("デッキ（空なら仮のデッキを使う）")]
        public DeckData player1Deck;
        public DeckData player2Deck;

        [Header("対戦相手")]
        // false にすると1台の画面で2人交互に遊べる
        public bool player2IsCpu = true;
        // true にすると CPU 同士の対戦を眺められる（動作確認用）
        public bool player1IsCpu = false;
        public float cpuStepDelay = 0.5f;

        [Header("見た目")]
        public float tileSize = 1f;
        // 空ならURPのLitシェーダーで作る
        public Material baseMaterial;
        public Color moveColor = new Color(0.2f, 0.9f, 0.3f);
        public Color boostMoveColor = new Color(0.85f, 0.95f, 0.2f);
        public Color attackColor = new Color(1f, 0.3f, 0.2f);
        public Color summonColor = new Color(1f, 0.85f, 0.2f);
        public Color selectedColor = Color.white;

        public BattleEngine Engine { get; private set; }

        System.Random rng;
        SimpleAI ai;
        readonly Dictionary<GridPos, TileView> tileViews = new Dictionary<GridPos, TileView>();
        readonly Dictionary<FigureUnit, FigureView> figureViews = new Dictionary<FigureUnit, FigureView>();
        readonly BarrierView[] barrierViews = new BarrierView[2];
        readonly Queue<IEnumerator> animQueue = new Queue<IEnumerator>();
        bool animating;
        bool rouletteRunning;
        float nextCpuTime;
        Transform boardRoot;
        Camera cam;

        // 入力の状態
        FigureUnit selectedHand;
        FigureUnit selectedUnit;
        GridPos? hoverPos;

        bool Busy => rouletteRunning || animating || animQueue.Count > 0;

        bool IsHuman(int player) => player == 0 ? !player1IsCpu : !player2IsCpu;

        // 今操作できる人間のプレイヤー。いなければ -1
        int ActiveHuman =>
            Engine != null && Engine.Phase != BattlePhase.Finished && IsHuman(Engine.CurrentPlayer) ? Engine.CurrentPlayer : -1;

        int restartCount;

        void Start()
        {
            SetupCamera();
            Init();
        }

        void Init()
        {
            int seed = (randomSeed != 0 ? randomSeed : System.Environment.TickCount) + restartCount;
            rng = new System.Random(seed);
            ai = new SimpleAI(seed + 1);

            var map = randomMap ? (MapType)rng.Next(4) : mapType;
            var board = MapGenerator.Generate(map, rng, rules);
            int first = rng.Next(2);

            var deck0 = player1Deck != null && player1Deck.figures.Count > 0 ? player1Deck.ToSpecs() : DefaultFigures.DeckA();
            var deck1 = player2Deck != null && player2Deck.figures.Count > 0 ? player2Deck.ToSpecs() : DefaultFigures.DeckB();
            Engine = new BattleEngine(rules, board, deck0, deck1, first);
            Engine.OnEvent += HandleEvent;

            BuildBoard();
            StartCoroutine(AnimRunner());
            StartCoroutine(Roulette(first));
        }

        // ---------- 盤面の組み立て ----------

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            float depth = rules.boardHeight * tileSize;
            cam.transform.position = new Vector3(0, depth * 1.45f, -depth * 0.8f);
            cam.transform.LookAt(new Vector3(0, 0, -depth * 0.06f));
            cam.fieldOfView = 46f;

            if (FindAnyObjectByType<Light>() == null)
            {
                var light = new GameObject("Directional Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }

        void BuildBoard()
        {
            boardRoot = new GameObject("FigureBattleBoard").transform;
            var board = Engine.Board;

            Color ground, blocked;
            switch (board.MapType)
            {
                case MapType.Mountain: ground = new Color(0.58f, 0.52f, 0.45f); blocked = new Color(0.4f, 0.38f, 0.36f); break;
                case MapType.Desert: ground = new Color(0.9f, 0.8f, 0.55f); blocked = new Color(0.7f, 0.5f, 0.3f); break;
                case MapType.Forest: ground = new Color(0.45f, 0.5f, 0.38f); blocked = new Color(0.15f, 0.35f, 0.15f); break;
                default: ground = new Color(0.74f, 0.72f, 0.56f); blocked = new Color(0.5f, 0.5f, 0.5f); break;
            }
            var highGround = new Color(0.55f, 0.85f, 0.95f);

            ViewUtil.Primitive(PrimitiveType.Cube, boardRoot, new Vector3(0, -0.35f, 0),
                new Vector3((board.Width + 0.6f) * tileSize, 0.4f, (board.Height + 0.6f) * tileSize),
                ViewUtil.NewMaterial(new Color(0.2f, 0.17f, 0.15f), baseMaterial), "Table");

            foreach (var p in board.AllPositions())
            {
                var type = board.Get(p);
                float height = type == TileType.HighGround ? 0.35f : type == TileType.Blocked ? 0.9f : 0.2f;
                Color c = type == TileType.HighGround ? highGround : type == TileType.Blocked ? blocked : ground;
                if ((p.x + p.y) % 2 == 0 && type != TileType.Blocked) c *= 0.93f;
                if (board.IsSafeArea(0, p)) c = Color.Lerp(c, ViewUtil.PlayerColor(0), 0.25f);
                if (board.IsSafeArea(1, p)) c = Color.Lerp(c, ViewUtil.PlayerColor(1), 0.25f);
                c.a = 1f;

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Tile_{p.x}_{p.y}";
                go.transform.SetParent(boardRoot, false);
                var center = GridToWorld(p);
                go.transform.position = new Vector3(center.x, height * 0.5f - 0.2f, center.z);
                go.transform.localScale = new Vector3(0.95f * tileSize, height, 0.95f * tileSize);
                var mat = ViewUtil.NewMaterial(c, baseMaterial);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                var tv = go.AddComponent<TileView>();
                tv.Init(p, c, height - 0.2f, mat);
                tileViews[p] = tv;
            }

            for (int i = 0; i < 2; i++)
            {
                var p = board.BarrierPos[i];
                var bv = new GameObject($"Barrier_P{i + 1}").AddComponent<BarrierView>();
                bv.transform.SetParent(boardRoot, false);
                bv.transform.position = WorldPos(p);
                bv.Init(i, p, rules.barrierHitsToBreak, baseMaterial);
                barrierViews[i] = bv;
            }

            foreach (var u in Engine.Units)
            {
                var fv = new GameObject().AddComponent<FigureView>();
                fv.transform.SetParent(boardRoot, false);
                fv.Init(u, baseMaterial);
                fv.gameObject.SetActive(false);
                figureViews[u] = fv;
            }
        }

        Vector3 GridToWorld(GridPos p)
        {
            var b = Engine.Board;
            return new Vector3((p.x - (b.Width - 1) * 0.5f) * tileSize, 0, (p.y - (b.Height - 1) * 0.5f) * tileSize);
        }

        // フィギュアを立たせる位置（高台なら少し上）
        Vector3 WorldPos(GridPos p)
        {
            var w = GridToWorld(p);
            w.y = tileViews.TryGetValue(p, out var tv) ? tv.TopY : 0f;
            return w;
        }

        // ---------- 進行 ----------

        IEnumerator Roulette(int first)
        {
            rouletteRunning = true;
            // 先攻・後攻を決めるルーレット。だんだん遅くなって止まる
            float interval = 0.05f;
            int shown = rng.Next(2);
            while (interval < 0.35f)
            {
                rouletteText = $"先攻は…  プレイヤー{shown + 1}";
                shown = 1 - shown;
                yield return new WaitForSeconds(interval);
                interval *= 1.18f;
            }
            rouletteText = $"先攻は  プレイヤー{first + 1}！";
            yield return new WaitForSeconds(1.2f);
            rouletteText = null;
            rouletteRunning = false;
            ShowBanner("セーフエリアにフィギュアを置こう");
        }

        IEnumerator AnimRunner()
        {
            while (true)
            {
                if (animQueue.Count > 0)
                {
                    animating = true;
                    yield return StartCoroutine(animQueue.Dequeue());
                }
                else
                {
                    animating = false;
                    yield return null;
                }
            }
        }

        void HandleEvent(BattleEvent e)
        {
            switch (e.Type)
            {
                case BattleEventType.Placed:
                case BattleEventType.Summoned:
                    animQueue.Enqueue(figureViews[e.Unit].Appear(WorldPos(e.To)));
                    break;
                case BattleEventType.Moved:
                    animQueue.Enqueue(figureViews[e.Unit].MoveTo(WorldPos(e.To)));
                    break;
                case BattleEventType.AttackDefeated:
                    animQueue.Enqueue(figureViews[e.Unit].Lunge(WorldPos(e.To)));
                    animQueue.Enqueue(figureViews[e.Target].Defeat());
                    break;
                case BattleEventType.AttackBlocked:
                    animQueue.Enqueue(figureViews[e.Unit].Lunge(WorldPos(e.To)));
                    animQueue.Enqueue(figureViews[e.Target].Shake());
                    break;
                case BattleEventType.BarrierHit:
                    animQueue.Enqueue(figureViews[e.Unit].Lunge(WorldPos(e.To)));
                    animQueue.Enqueue(barrierViews[e.Player].PlayHit(e.Value));
                    break;
                case BattleEventType.BarrierBroken:
                    animQueue.Enqueue(figureViews[e.Unit].Lunge(WorldPos(e.To)));
                    animQueue.Enqueue(barrierViews[e.Player].PlayBreak());
                    break;
                case BattleEventType.PhaseChanged:
                    ShowBanner("バトル開始！");
                    break;
                case BattleEventType.TurnStarted:
                    ShowBanner(IsHuman(e.Player) ? $"プレイヤー{e.Player + 1}のターン" : "CPUのターン");
                    break;
            }
        }

        void Update()
        {
            if (Engine == null || Busy) { RefreshHighlights(); return; }

            if (Engine.Phase != BattlePhase.Finished && !IsHuman(Engine.CurrentPlayer))
                UpdateCpu();
            else
                UpdateHumanInput();

            RefreshHighlights();
        }

        void UpdateCpu()
        {
            if (Time.time < nextCpuTime) return;
            var a = ai.Next(Engine, Engine.CurrentPlayer);
            switch (a.Type)
            {
                case AIActionType.Place: Engine.Place(a.Unit, a.Pos); break;
                case AIActionType.Summon: Engine.Summon(a.Unit, a.Pos); break;
                case AIActionType.Move: Engine.Move(a.Unit, a.Pos); break;
                case AIActionType.Attack: Engine.Attack(a.Unit, a.Pos); break;
                case AIActionType.Rest: Engine.Rest(a.Unit); break;
                case AIActionType.EndTurn: Engine.EndTurn(); break;
            }
            nextCpuTime = Time.time + cpuStepDelay;
        }

        // ---------- 人間の入力 ----------

        void UpdateHumanInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mp = mouse.position.ReadValue();
            bool overGui = IsOverGui(mp);
            hoverPos = overGui ? null : Pick(mp);

            if (ActiveHuman < 0) return;

            if (mouse.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
                ClearSelection();

            if (mouse.leftButton.wasPressedThisFrame && !overGui && hoverPos.HasValue)
                OnClickCell(hoverPos.Value);
        }

        GridPos? Pick(Vector2 screenPos)
        {
            if (cam == null) return null;
            var ray = cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 200f)) return null;
            var fv = hit.collider.GetComponent<FigureView>();
            if (fv != null && fv.Unit.OnField) return fv.Unit.Pos;
            var bv = hit.collider.GetComponent<BarrierView>();
            if (bv != null) return bv.Pos;
            var tv = hit.collider.GetComponent<TileView>();
            if (tv != null) return tv.Pos;
            return null;
        }

        void OnClickCell(GridPos p)
        {
            int me = ActiveHuman;

            // 手札から置く／出す
            if (selectedHand != null)
            {
                bool ok = Engine.Phase == BattlePhase.Placement ? Engine.Place(selectedHand, p) : Engine.Summon(selectedHand, p);
                if (ok) { selectedHand = null; return; }
            }

            if (Engine.Phase != BattlePhase.Battle) return;

            if (selectedUnit != null)
            {
                if (Engine.GetAttackTargets(selectedUnit).Any(t => t.Pos == p))
                {
                    Engine.Attack(selectedUnit, p);
                    selectedUnit = null;
                    return;
                }
                if (Engine.GetMoveOptions(selectedUnit).Any(o => o.Pos == p))
                {
                    Engine.Move(selectedUnit, p);
                    // 移動後に攻撃できる相手がいなければそのまま行動終了
                    if (selectedUnit.State == ActState.Done) selectedUnit = null;
                    else if (Engine.GetAttackTargets(selectedUnit).Count == 0)
                    {
                        Engine.Rest(selectedUnit);
                        selectedUnit = null;
                    }
                    return;
                }
            }

            var u = Engine.UnitAt(p);
            if (u != null && u.Owner == me && u.State != ActState.Done)
            {
                selectedUnit = u;
                selectedHand = null;
            }
            else
                ClearSelection();
        }

        void SelectHand(FigureUnit u)
        {
            selectedHand = selectedHand == u ? null : u;
            selectedUnit = null;
        }

        void ClearSelection()
        {
            selectedHand = null;
            selectedUnit = null;
        }

        void EndTurnByHuman()
        {
            ClearSelection();
            Engine.EndTurn();
        }

        // ---------- ハイライト ----------

        void RefreshHighlights()
        {
            if (Engine == null) return;
            var colors = new Dictionary<GridPos, Color>();

            if (!Busy && ActiveHuman >= 0)
            {
                if (selectedHand != null)
                {
                    bool can = Engine.Phase == BattlePhase.Placement || Engine.CanSummonAny(selectedHand);
                    if (can)
                        foreach (var p in Engine.SummonTiles(ActiveHuman)) colors[p] = summonColor;
                }
                if (selectedUnit != null)
                {
                    colors[selectedUnit.Pos] = selectedColor;
                    foreach (var o in Engine.GetMoveOptions(selectedUnit)) colors[o.Pos] = o.Boosted ? boostMoveColor : moveColor;
                    foreach (var t in Engine.GetAttackTargets(selectedUnit)) colors[t.Pos] = attackColor;
                }
            }

            foreach (var kv in tileViews)
                kv.Value.SetHighlight(colors.TryGetValue(kv.Key, out var c) ? c : (Color?)null);
        }

        // シーンを読み直さずに盤面ごと作り直す（ビルド設定にシーンが無くても動くように）
        void Restart()
        {
            StopAllCoroutines();
            animQueue.Clear();
            animating = false;
            if (boardRoot != null) Destroy(boardRoot.gameObject);
            tileViews.Clear();
            figureViews.Clear();
            ClearSelection();
            restartCount++;
            Init();
        }
    }
}
