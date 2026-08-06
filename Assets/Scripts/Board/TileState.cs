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
                ConnectionMask connections = Shape.GetBaseConnections();

                for (int i = 0; i < Rotation; i++)
                {
                    connections = connections.RotateClockwise();
                }
                return connections;
            }
        }

        public bool IsLocked { get; }
        public bool IsPowered { get; private set; }

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

        public bool RotateClockwise()
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
//Bu kodda TileState sınıfı, bir oyun tahtasındaki her bir karonun durumunu temsil eder. Karo, belirli bir şekle (TileShape) ve role (TileRole) sahiptir ve belirli bir konumda (X, Y) bulunur. 
//Ayrıca, karonun döndürülme durumu (Rotation), kilitli olup olmadığı (IsLocked) ve güç durumunu (IsPowered) da içerir.