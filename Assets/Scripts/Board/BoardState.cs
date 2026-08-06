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
            if (tiles == null || !IsInside(tile.X, tile.Y))
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
    }
}