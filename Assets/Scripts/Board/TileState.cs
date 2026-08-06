using PipeMuzzle.Data;

namespace PipeMuzzle.Board
{
    public class TileState
    {
        public int X { get; }
        public int Y { get; }

        public TileShape Shape { get; }
        public TileRole Role { get; }

        public int Rotation { get; private set; }

        public bool IsLocked { get; }
        public bool isPowered { get; private set; }

        public TileState(int x, int y, TileShape shape, TileRole role, int rotation, bool isLocked)
        {
            X = x;
            Y = y; 
            Shape = shape;
            Role = role;
            Rotation = rotation;
            IsLocked = isLocked;
            isPowered = false;
        }
    }
}