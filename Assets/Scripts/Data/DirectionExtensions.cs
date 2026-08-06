using System;

namespace PipeMuzzle.Data
{
    public static class DirectionExtensions
    {
        public static Direction Opposite(this Direction direction)
        {
            return direction switch
            {
                Direction.North => Direction.South,
                Direction.East => Direction.West,
                Direction.South => Direction.North,
                Direction.West => Direction.East,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    null)
            };
        }

        public static ConnectionMask ToMask(this Direction direction)
        {
            return direction switch
            {
                Direction.North => ConnectionMask.North,
                Direction.East => ConnectionMask.East,
                Direction.South => ConnectionMask.South,
                Direction.West => ConnectionMask.West,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    null)
            };
        }

        public static int DeltaX(this Direction direction)
        {
            return direction switch
            {
                Direction.North => 0,
                Direction.East => 1,
                Direction.South => 0,
                Direction.West => -1,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    null)
            };
        }

        public static int DeltaY(this Direction direction)
        {
            return direction switch
            {
                Direction.North => 1,
                Direction.East => 0,
                Direction.South => -1,
                Direction.West => 0,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    null)
            };
        }
    }
}