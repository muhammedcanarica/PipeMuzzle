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

        public ConnectionMask Connections
        {
            get
            {
                ConnectionMask connections = Shape.GetConnections();

                for (int i = 0; i < Rotation; i++)
                {
                    connections = connections.RotateClockwise();
                }
                return connections;
            }
        }

        public bool IsLocked { get; }
        public bool isPowered { get; private set; }

        // alttaki constructor oluyor.
        public TileState(int x, int y, TileShape shape, TileRole role, int rotation, bool isLocked)
        {
            X = x;
            Y = y;
            Shape = shape;
            Role = role;
            Rotation = rotation;
            IsLocked = isLocked;
            IsPowered = false;
        }

        public bool RotateClockWise()
        {
            if (IsLocked || Shape == TileShape.Empty)
            {
                return false;
            }
            Rotation = (Rotation + 1) % 4;
            return true;
        }
        public void SetPowered(bool powered)
        {
            IsPowered = powered;
        }
    }
}