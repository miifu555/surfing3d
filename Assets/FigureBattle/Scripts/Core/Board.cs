using System;
using System.Collections.Generic;

namespace FigureBattle
{
    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        public int x;
        public int y;

        public GridPos(int x, int y) { this.x = x; this.y = y; }

        public static readonly GridPos[] Dirs =
        {
            new GridPos(1, 0), new GridPos(-1, 0), new GridPos(0, 1), new GridPos(0, -1),
        };

        public int Manhattan(GridPos o) => Math.Abs(x - o.x) + Math.Abs(y - o.y);

        public static GridPos operator +(GridPos a, GridPos b) => new GridPos(a.x + b.x, a.y + b.y);
        public static bool operator ==(GridPos a, GridPos b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(GridPos a, GridPos b) => !(a == b);
        public bool Equals(GridPos o) => this == o;
        public override bool Equals(object obj) => obj is GridPos o && this == o;
        public override int GetHashCode() => x * 397 ^ y;
        public override string ToString() => $"({x},{y})";
    }

    // 盤面。プレイヤー0が y=0 側、プレイヤー1が y=Height-1 側
    public class Board
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int SafeRows;
        public MapType MapType;
        readonly TileType[,] tiles;
        // 各プレイヤーのバリアの位置（後ろの列の中央）
        public readonly GridPos[] BarrierPos = new GridPos[2];

        public Board(int width, int height, int safeRows)
        {
            Width = width;
            Height = height;
            SafeRows = safeRows;
            tiles = new TileType[width, height];
            BarrierPos[0] = new GridPos(width / 2, 0);
            BarrierPos[1] = new GridPos(width - 1 - width / 2, height - 1);
        }

        public bool InBounds(GridPos p) => p.x >= 0 && p.y >= 0 && p.x < Width && p.y < Height;

        public TileType Get(GridPos p) => tiles[p.x, p.y];
        public void Set(GridPos p, TileType t) => tiles[p.x, p.y] = t;

        public bool IsSafeArea(int player, GridPos p)
        {
            return player == 0 ? p.y < SafeRows : p.y >= Height - SafeRows;
        }

        // バリアのマスなら持ち主を返す。違うなら -1
        public int BarrierOwnerAt(GridPos p)
        {
            if (p == BarrierPos[0]) return 0;
            if (p == BarrierPos[1]) return 1;
            return -1;
        }

        public IEnumerable<GridPos> AllPositions()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    yield return new GridPos(x, y);
        }
    }

    // マップの種類に合わせて盤面を作る。どちらにも有利にならないよう点対称にする
    public static class MapGenerator
    {
        public static Board Generate(MapType type, Random rng, BattleRulesConfig config)
        {
            float blockedRate, highRate;
            switch (type)
            {
                case MapType.Mountain: blockedRate = 0.14f; highRate = 0.16f; break;
                case MapType.Desert: blockedRate = 0.08f; highRate = 0.06f; break;
                case MapType.Forest: blockedRate = 0.18f; highRate = 0.06f; break;
                default: blockedRate = 0.04f; highRate = 0.08f; break;
            }

            for (int attempt = 0; attempt < 50; attempt++)
            {
                var board = new Board(config.boardWidth, config.boardHeight, config.safeRows);
                board.MapType = type;
                foreach (var p in board.AllPositions())
                {
                    // セーフエリアは常に普通のマス
                    if (board.IsSafeArea(0, p) || board.IsSafeArea(1, p)) continue;
                    var mirror = Mirror(board, p);
                    // 盤面の後ろ半分は前半分を写すだけ
                    if (p.y > mirror.y || (p.y == mirror.y && p.x > mirror.x)) continue;

                    double r = rng.NextDouble();
                    var t = r < blockedRate ? TileType.Blocked
                        : r < blockedRate + highRate ? TileType.HighGround
                        : TileType.Normal;
                    board.Set(p, t);
                    board.Set(mirror, t);
                }
                if (IsConnected(board)) return board;
            }
            // どうしても繋がらなければ障害物なし
            var plain = new Board(config.boardWidth, config.boardHeight, config.safeRows);
            plain.MapType = type;
            return plain;
        }

        static GridPos Mirror(Board b, GridPos p) => new GridPos(b.Width - 1 - p.x, b.Height - 1 - p.y);

        // 通れるマスが全部つながっているか
        static bool IsConnected(Board board)
        {
            GridPos start = default;
            int total = 0;
            foreach (var p in board.AllPositions())
            {
                if (!Walkable(board, p)) continue;
                if (total == 0) start = p;
                total++;
            }
            var seen = new HashSet<GridPos> { start };
            var queue = new Queue<GridPos>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in GridPos.Dirs)
                {
                    var n = c + d;
                    if (!board.InBounds(n) || !Walkable(board, n) || !seen.Add(n)) continue;
                    queue.Enqueue(n);
                }
            }
            return seen.Count == total;
        }

        static bool Walkable(Board b, GridPos p) => b.Get(p) != TileType.Blocked && b.BarrierOwnerAt(p) < 0;
    }
}
