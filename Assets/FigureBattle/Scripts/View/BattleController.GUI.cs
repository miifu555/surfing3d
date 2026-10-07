using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FigureBattle
{
    // 仮の画面表示（IMGUI）。本番のUIを作るまでのあいだ、遊んで確認するためのもの
    public partial class BattleController
    {
        const float BaseHeight = 720f;

        string rouletteText;
        string bannerText;
        float bannerUntil;
        // 画面上のUIの範囲。ここをクリックしたときは盤面を触らない
        readonly List<Rect> guiRects = new List<Rect>();
        GUIStyle boxStyle, labelStyle, smallStyle, bigStyle, buttonStyle, figLabelStyle;

        float GuiScale => Screen.height / BaseHeight;

        void ShowBanner(string text)
        {
            bannerText = text;
            bannerUntil = Time.time + 1.6f;
        }

        bool IsOverGui(Vector2 mouseScreen)
        {
            var v = new Vector2(mouseScreen.x / GuiScale, (Screen.height - mouseScreen.y) / GuiScale);
            return guiRects.Any(r => r.Contains(v));
        }

        void EnsureStyles()
        {
            if (boxStyle != null) return;
            boxStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true };
            labelStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(labelStyle) { fontSize = 12 };
            bigStyle = new GUIStyle(labelStyle) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, richText = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 8, 4, 4) };
            figLabelStyle = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }

        Rect Panel(Rect r)
        {
            guiRects.Add(r);
            GUI.Box(r, GUIContent.none, boxStyle);
            return r;
        }

        void OnGUI()
        {
            if (Engine == null) return;
            EnsureStyles();
            guiRects.Clear();

            float s = GuiScale;
            float vw = Screen.width / s;
            float vh = BaseHeight;

            // フィギュアの頭上の名前は拡大前の座標で描く
            DrawFigureLabels(s);

            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));

            DrawTopInfo(vw);
            DrawPlayerPanel(0, new Rect(10, 10, 260, 0));
            DrawPlayerPanel(1, new Rect(vw - 270, 10, 260, 0));
            DrawUnitPanel(new Rect(10, vh - 170, 400, 160));
            DrawLog(new Rect(vw - 380, vh - 180, 370, 100));

            if (Engine.Phase == BattlePhase.Battle && ActiveHuman >= 0 && !Busy)
            {
                var r = new Rect(vw - 180, vh - 70, 170, 60);
                guiRects.Add(r);
                if (GUI.Button(r, "<size=20>ターン終了</size>", new GUIStyle(buttonStyle) { alignment = TextAnchor.MiddleCenter }))
                    EndTurnByHuman();
            }

            if (rouletteText != null)
                DrawCenterText(vw, vh, rouletteText, new Color(0, 0, 0, 0.6f));
            else if (bannerText != null && Time.time < bannerUntil && Engine.Phase != BattlePhase.Finished)
                DrawCenterText(vw, vh, bannerText, new Color(0, 0, 0, 0.4f));

            if (Engine.Phase == BattlePhase.Finished && !Busy)
                DrawResult(vw, vh);

            GUI.matrix = Matrix4x4.identity;
        }

        void DrawTopInfo(float vw)
        {
            var r = Panel(new Rect(vw * 0.5f - 260, 8, 520, 74));
            string turn = Engine.Phase == BattlePhase.Placement ? "配置フェーズ" : $"ターン {Engine.TurnNumber}";
            string who = Engine.Phase == BattlePhase.Finished ? "" : $"   <b>{PlayerLabel(Engine.CurrentPlayer)}</b>の番";
            GUI.Label(new Rect(r.x + 10, r.y + 6, r.width - 20, 22),
                $"{turn}{who}   マップ：{ViewUtil.MapName(Engine.Board.MapType)}   ステージ：{ViewUtil.StageName(rules.stageRule)}", labelStyle);
            GUI.Label(new Rect(r.x + 10, r.y + 28, r.width - 20, 20), Hint(), smallStyle);
            GUI.Label(new Rect(r.x + 10, r.y + 48, r.width - 20, 20),
                "相性：ジェット→スナイパー→ブレイカー→ソルジャー→ジェット（矢印の先に2倍）", smallStyle);
        }

        string Hint()
        {
            if (rouletteText != null) return "先攻・後攻を決めています…";
            if (Engine.Phase == BattlePhase.Finished) return "バトル終了";
            if (ActiveHuman < 0) return "CPUが考えています…";
            if (Engine.Phase == BattlePhase.Placement)
                return selectedHand == null ? "手札からフィギュアを選んでください" : "黄色のセーフエリアに置いてください";
            if (selectedHand != null)
                return Engine.CanSummonAny(selectedHand) ? "黄色のマスに出す（右クリックで取り消し）" : "空想力が足りないか、場がいっぱいです";
            if (selectedUnit != null)
                return selectedUnit.State == ActState.Ready
                    ? "緑：移動　黄緑：高速移動（攻撃不可）　赤：攻撃"
                    : "赤のマスを攻撃するか「攻撃せず行動終了」";
            return "フィギュアを選んで行動 / 手札を空想力で出す / ターン終了";
        }

        string PlayerLabel(int p)
        {
            if (!IsHuman(p)) return player1IsCpu && player2IsCpu ? $"CPU{p + 1}" : "CPU";
            return player1IsCpu || player2IsCpu ? "あなた" : $"プレイヤー{p + 1}";
        }

        void DrawPlayerPanel(int p, Rect r)
        {
            var ps = Engine.Players[p];
            var hand = Engine.HandUnits(p).ToList();
            r.height = 128 + hand.Count * 50;
            Panel(r);

            float y = r.y + 6;
            void Line(string text, GUIStyle st, float h = 20)
            {
                GUI.Label(new Rect(r.x + 10, y, r.width - 20, h), text, st);
                y += h;
            }

            string color = ColorUtility.ToHtmlStringRGB(ViewUtil.PlayerColor(p));
            string first = Engine.FirstPlayer == p ? "先攻" : "後攻";
            Line($"<b><color=#{color}>■ {PlayerLabel(p)}</color></b>  （{first}）", labelStyle, 22);
            Line($"空想力  <b>{ps.Imagination}</b> / {rules.maxImagination}", labelStyle);
            Line($"撃破ライフ  <b>{ps.DefeatedLifeTotal}</b> / {rules.lifeTotalToWin}", labelStyle);
            int left = ps.BarrierBroken ? 0 : rules.barrierHitsToBreak - ps.BarrierHits;
            Line($"バリア  {new string('◆', left)}{new string('◇', rules.barrierHitsToBreak - left)}", labelStyle);
            Line($"場  {Engine.FieldUnits(p).Count()} / {rules.maxFiguresOnField}　　手札", labelStyle);

            bool canUse = ActiveHuman == p && !Busy;
            foreach (var u in hand)
            {
                var br = new Rect(r.x + 8, y + 2, r.width - 16, 46);
                int cost = Engine.SummonCost(u);
                bool usable = canUse && (Engine.Phase == BattlePhase.Placement || Engine.CanSummonAny(u));
                string mark = selectedHand == u ? "▶ " : "";
                string costText = Engine.Phase == BattlePhase.Placement ? "" : $"  空想力{cost}";
                string text = $"{mark}<b>{u.Spec.figureName}</b> [{u.Spec.rarity}] {ViewUtil.ClassName(u.Spec.figureClass)}\n" +
                              $"P{u.Spec.power} / L{u.Spec.life}  範囲{u.Spec.range}{costText}";
                GUI.enabled = usable;
                if (IsHuman(p))
                {
                    if (GUI.Button(br, text, buttonStyle)) SelectHand(u);
                }
                else GUI.Box(br, text, buttonStyle);
                GUI.enabled = true;
                y += 50;
            }
        }

        void DrawUnitPanel(Rect r)
        {
            FigureUnit u = selectedUnit;
            if (u == null && hoverPos.HasValue) u = Engine.UnitAt(hoverPos.Value);
            if (u == null) u = selectedHand;
            if (u == null) return;
            Panel(r);

            var sp = u.Spec;
            float y = r.y + 6;
            GUI.Label(new Rect(r.x + 10, y, r.width - 20, 22),
                $"<b>{sp.figureName}</b>  [{sp.rarity}]  {ViewUtil.ClassName(sp.figureClass)}  （{PlayerLabel(u.Owner)}）", labelStyle);
            y += 24;
            string atkRange = u.OnField ? $"（攻撃範囲{Engine.AttackRange(u, u.Pos)}）" : "";
            GUI.Label(new Rect(r.x + 10, y, r.width - 20, 22),
                $"パワー {sp.power}　ライフ {sp.life}　範囲 {BattleRules.EffectiveRange(sp, rules)}{atkRange}", labelStyle);
            y += 24;
            GUI.Label(new Rect(r.x + 10, y, r.width - 20, 20), ClassSkillText(sp.figureClass), smallStyle);
            y += 22;

            string preview = AttackPreview();
            if (preview != null)
            {
                GUI.Label(new Rect(r.x + 10, y, r.width - 20, 22), preview, labelStyle);
                y += 24;
            }

            if (u == selectedUnit && ActiveHuman == u.Owner && !Busy && Engine.Phase == BattlePhase.Battle)
            {
                if (GUI.Button(new Rect(r.x + 10, r.yMax - 38, 170, 30), "攻撃せず行動終了", buttonStyle))
                {
                    Engine.Rest(u);
                    selectedUnit = null;
                }
                if (GUI.Button(new Rect(r.x + 190, r.yMax - 38, 110, 30), "選択解除", buttonStyle))
                    ClearSelection();
            }
        }

        static string ClassSkillText(FigureClass c)
        {
            switch (c)
            {
                case FigureClass.Sniper: return "水色のマスにいると攻撃範囲が2倍";
                case FigureClass.Jet: return "範囲2倍まで移動できる（その場合は攻撃できない）";
                case FigureClass.Breaker: return "バリアを一撃で破壊できる";
            }
            return "基本のクラス。行動範囲内を移動して攻撃する";
        }

        // 攻撃先にカーソルを合わせたときの結果予想
        string AttackPreview()
        {
            if (selectedUnit == null || !hoverPos.HasValue) return null;
            var t = Engine.GetAttackTargets(selectedUnit).FirstOrDefault(a => a.Pos == hoverPos.Value);
            if (t.Unit == null && !t.IsBarrier) return null;
            if (t.IsBarrier)
            {
                var ps = Engine.Players[t.BarrierOwner];
                return selectedUnit.Spec.figureClass == FigureClass.Breaker
                    ? "<color=#ffcc33>バリアを一撃で破壊！</color>"
                    : $"<color=#ffcc33>バリアに攻撃（{ps.BarrierHits + 1}/{rules.barrierHitsToBreak}）</color>";
            }
            int power = BattleRules.AttackPower(selectedUnit.Spec, t.Unit.Spec, rules);
            bool win = BattleRules.WouldDefeat(selectedUnit.Spec, t.Unit.Spec, rules);
            return $"パワー{power} vs ライフ{t.Unit.Spec.life} → " + (win ? "<color=#ff6655><b>撃破！</b></color>" : "<color=#aaaaaa>防がれる</color>");
        }

        void DrawLog(Rect r)
        {
            Panel(r);
            var lines = Engine.Log.Skip(Mathf.Max(0, Engine.Log.Count - 4));
            GUI.Label(new Rect(r.x + 10, r.y + 6, r.width - 20, r.height - 12), string.Join("\n", lines), smallStyle);
        }

        void DrawCenterText(float vw, float vh, string text, Color bg)
        {
            var r = new Rect(0, vh * 0.5f - 50, vw, 100);
            var old = GUI.color;
            GUI.color = bg;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(r, text, bigStyle);
        }

        void DrawResult(float vw, float vh)
        {
            var r = Panel(new Rect(vw * 0.5f - 220, vh * 0.5f - 110, 440, 220));
            string reason;
            switch (Engine.WinReason)
            {
                case WinReason.BarrierBreak: reason = "バリアを破壊した"; break;
                case WinReason.LifeTotal: reason = $"倒したライフの合計が{rules.lifeTotalToWin}に到達"; break;
                case WinReason.Annihilation: reason = "相手のフィギュアが全滅した"; break;
                default: reason = ""; break;
            }
            GUI.Label(new Rect(r.x, r.y + 20, r.width, 60), $"{PlayerLabel(Engine.Winner)}の勝ち！", bigStyle);
            GUI.Label(new Rect(r.x + 20, r.y + 90, r.width - 40, 24), reason,
                new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter });
            if (GUI.Button(new Rect(r.x + r.width * 0.5f - 90, r.y + 150, 180, 44), "<size=18>もう一度</size>",
                    new GUIStyle(buttonStyle) { alignment = TextAnchor.MiddleCenter }))
                Restart();
        }

        void DrawFigureLabels(float s)
        {
            if (cam == null || Engine.Phase == BattlePhase.Finished) return;
            foreach (var kv in figureViews)
            {
                var u = kv.Key;
                if (!u.OnField || !kv.Value.gameObject.activeSelf) continue;
                var sp = cam.WorldToScreenPoint(kv.Value.transform.position + Vector3.up * 1.15f);
                if (sp.z < 0) continue;
                var r = new Rect(sp.x - 60 * s, Screen.height - sp.y - 15 * s, 120 * s, 30 * s);
                figLabelStyle.fontSize = Mathf.RoundToInt(10.5f * s);
                string c = ColorUtility.ToHtmlStringRGB(Color.Lerp(ViewUtil.PlayerColor(u.Owner), Color.white, 0.5f));
                string done = u.State == ActState.Done && Engine.CurrentPlayer == u.Owner ? "（済）" : "";
                var old = GUI.color;
                GUI.color = new Color(0, 0, 0, 0.45f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = old;
                GUI.Label(r, $"<color=#{c}>{u.Spec.figureName}{done}</color>\nP{u.Spec.power} / L{u.Spec.life}", figLabelStyle);
            }
        }
    }
}
