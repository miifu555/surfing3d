using System;
using UnityEngine;

namespace FigureBattle
{
    // バトルのルール数値。仕様に書かれていない部分もここで調整できるようにしてある
    [Serializable]
    public class BattleRulesConfig
    {
        [Header("盤面")]
        public int boardWidth = 7;
        public int boardHeight = 9;
        // 手前から何列をセーフエリアにするか
        public int safeRows = 2;

        [Header("場")]
        // 場に出せるフィギュアの上限
        public int maxFiguresOnField = 3;
        // バトル開始前にセーフエリアへ置く数
        public int initialPlacementCount = 1;

        [Header("空想力")]
        public int startImagination = 0;
        // 自分のターン開始時に貯まる量
        public int imaginationPerTurn = 1;
        public int maxImagination = 10;

        [Header("攻撃")]
        // 相性が有利なときのパワー倍率
        public int affinityMultiplier = 2;
        // true ならパワーとライフが同じでも倒せる（仕様は「上回ると倒せる」なので false）
        public bool defeatOnEqual = false;
        // スナイパーが水色マスにいるときの攻撃範囲の倍率
        public int sniperRangeMultiplier = 2;
        // false にするとスナイパーはどのマスでも攻撃範囲が伸びる
        public bool sniperNeedsHighGround = true;
        // ジェットが移動できる距離の倍率（この距離まで動くとそのターンは攻撃できない）
        public int jetMoveMultiplier = 2;

        [Header("バリア")]
        // バリアを壊すのに必要な攻撃回数（ブレイカーは一撃）
        public int barrierHitsToBreak = 3;

        [Header("勝利条件")]
        // 倒したフィギュアのライフの合計がこれに届いたら勝ち
        public int lifeTotalToWin = 15000;
        // 倒されたフィギュアを手札に戻して、また出せるようにする
        public bool defeatedReturnToHand = true;

        [Header("その他")]
        // 出したターンにすぐ行動できるか（バトル前の配置ぶんは常に行動できる）
        public bool summonedCanActImmediately = false;
        public StageRule stageRule = StageRule.None;
    }
}
