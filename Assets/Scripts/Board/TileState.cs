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
        public bool IsHintLocked { get; private set; }
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
            if (IsLocked || IsHintLocked || Shape == TileShape.Empty)
            {
                return false;
            }
            Rotation = (Rotation + 1) % 4;
            return true;
        }

        public bool TryApplyHint(int solutionRotation)
        {
            if (IsLocked || IsHintLocked || Role != TileRole.Normal ||
                Shape == TileShape.Empty || solutionRotation < 0 || solutionRotation > 3)
                return false;

            ConnectionMask solved = Shape.GetBaseConnections();
            for (int i = 0; i < solutionRotation; i++) solved = solved.RotateClockwise();
            // Compare ports, so equivalent straight rotations and symmetric crosses are skipped.
            if (Connections == solved) return false;
            Rotation = solutionRotation;
            IsHintLocked = true;
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
