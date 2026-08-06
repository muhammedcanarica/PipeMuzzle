using System;
using System.Collections.Generic;
using PipeMuzzle.Data;

namespace PipeMuzzle.Board
{
    public static class ConnectionChecker
    {
        private static readonly Direction[] Directions =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        public static bool Evaluate(BoardState board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            ResetPowerStates(board);

            TileState source = board.FindTileByRole(TileRole.Source);

            if (source == null)
            {
                return false;
            }

            Queue<TileState> queue = new Queue<TileState>();
            bool[,] visited = new bool[board.Width, board.Height];

            queue.Enqueue(source);
            visited[source.X, source.Y] = true;
            source.SetPowered(true);

            bool targetReached = false;

            while (queue.Count > 0)
            {
                TileState current = queue.Dequeue();

                if (current.Role == TileRole.Target)
                {
                    targetReached = true;
                }

                foreach (Direction direction in Directions)
                {
                    if (!current.Connections.Has(direction.ToMask()))
                    {
                        continue;
                    }

                    int neighborX =
                        current.X + direction.DeltaX();

                    int neighborY =
                        current.Y + direction.DeltaY();

                    TileState neighbor =
                        board.GetTile(neighborX, neighborY);

                    if (neighbor == null)
                    {
                        continue;
                    }

                    if (visited[neighborX, neighborY])
                    {
                        continue;
                    }

                    ConnectionMask oppositeConnection =
                        direction.Opposite().ToMask();

                    if (!neighbor.Connections.Has(oppositeConnection))
                    {
                        continue;
                    }

                    visited[neighborX, neighborY] = true;
                    neighbor.SetPowered(true);
                    queue.Enqueue(neighbor);
                }
            }

            return targetReached;
        }

        private static void ResetPowerStates(BoardState board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    TileState tile = board.GetTile(x, y);

                    if (tile != null)
                    {
                        tile.SetPowered(false);
                    }
                }
            }
        }
    }
}