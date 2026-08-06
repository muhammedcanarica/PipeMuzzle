using PipeMuzzle.Data;

namespace PipeMuzzle.Board
{
    public class BoardState
    {
        private readonly TileState[,] tiles;

        public int Width { get; }
        public int Height { get; }

        public int MoveCount { get; private set; }

        public BoardState(int width, int height)
        {
            Width = width;
            Height = height;

            tiles = new TileState[width, height];

            MoveCount = 0;
        }
        public bool IsInside(int x, int y)
        {
            return x >= 0 &&
                   x < Width &&
                   y >= 0 &&
                   y < Height;
        }

        public bool SetTile(TileState tile)
        {
            if (tile == null || !IsInside(tile.X, tile.Y))
            {
                return false;
            }

            tiles[tile.X, tile.Y] = tile;
            return true;
        }

        public TileState GetTile(int x, int y)
        {
            if (!IsInside(x, y))
            {
                return null;
            }
            return tiles[x, y];
        }

        public void IncrementMoveCount()
        {
            MoveCount++;
        }

        public void ResetMoveCount()
        {
            MoveCount = 0;
        }

        public bool TryRotateTile(int x, int y)
        {
            TileState tile = GetTile(x, y);

            if (tile == null)
            {
                return false;
            }

            bool rotated = tile.RotateClockwise();
            if (!rotated)
            {
                return false;
            }
            IncrementMoveCount();
            return true;
        }
        public TileState FindTileByRole(TileRole role)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    TileState tile = tiles[x, y];

                    if (tile != null && tile.Role == role)
                    {
                        return tile;
                    }
                }
            }

            return null;
        }
    }
}