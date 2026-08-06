namespace PipeMuzzle.Data
{
    public static class ConnectionMaskExtensions
    {
        public static bool Has(
            this ConnectionMask mask,
            ConnectionMask connection) => (mask & connection) == connection;

        public static ConnectionMask RotateClockwise(
            this ConnectionMask mask)
        {
            ConnectionMask rotated = ConnectionMask.None;

            if (mask.Has(ConnectionMask.North))
            {
                rotated |= ConnectionMask.East;
            }

            if (mask.Has(ConnectionMask.East))
            {
                rotated |= ConnectionMask.South;
            }

            if (mask.Has(ConnectionMask.South))
            {
                rotated |= ConnectionMask.West;
            }

            if (mask.Has(ConnectionMask.West))
            {
                rotated |= ConnectionMask.North;
            }

            return rotated;
        }
    }
}