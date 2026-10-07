namespace FigureBattle
{
    // フィギュアのクラス。相性は Jet → Sniper → Breaker → Soldier → Jet の順に有利
    public enum FigureClass
    {
        Soldier,  // ソルジャー：基本の動き
        Sniper,   // スナイパー：水色マスにいると攻撃範囲が2倍
        Jet,      // ジェット：行動範囲2倍まで移動できるが、その場合は攻撃できない
        Breaker,  // ブレイカー：バリアを一撃で壊せる
    }

    // レアリティ。性能は変わらず見た目の豪華さだけが変わる
    public enum Rarity { C, UC, R, SR, UR, GR }

    // 初期状態でのマップパターン
    public enum MapType { Mountain, Plains, Desert, Forest }

    public enum TileType
    {
        Normal,     // 普通のマス
        HighGround, // 水色のマス（スナイパーの射程が伸びる高台）
        Blocked,    // 通れないマス（岩・木など）
    }

    // ステージカード（特殊ルール）
    public enum StageRule
    {
        None,
        Space,        // 宇宙：行動範囲が4/3倍
        SnowMountain, // 雪山：行動範囲が2/3倍
        Kinkakuji,    // 金閣寺：出すのに必要な空想力が2倍
        Ginkakuji,    // 銀閣寺：出すのに必要な空想力が1/2倍
        Versailles,   // ヴェルサイユ宮殿：相性で倍になるパワーが3倍
        TeaCup,       // 遊園地のティーカップ：相性が全部逆になる
        LeaningTower, // ピザの斜塔：今いる位置より左にしか移動できない
        Dojo,         // 剣道場：ソルジャーのパワーが2倍
    }

    public enum BattlePhase { Placement, Battle, Finished }

    public enum WinReason { None, LifeTotal, BarrierBreak, Annihilation }

    // 1ターン中のフィギュアの行動状態
    public enum ActState
    {
        Ready, // まだ何もしていない
        Moved, // 移動済み（攻撃はまだできる）
        Done,  // 行動終了
    }
}
