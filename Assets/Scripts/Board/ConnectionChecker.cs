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
                    if (!TryGetConnectedNeighbor(
                            board,
                            current,
                            direction,
                            out TileState neighbor) ||
                        visited[neighbor.X, neighbor.Y])
                    {
                        continue;
                    }

                    visited[neighbor.X, neighbor.Y] = true;
                    neighbor.SetPowered(true);
                    queue.Enqueue(neighbor);
                }
            }

            return targetReached;
        }

        public static bool TryGetSolvedPath(
            BoardState board,
            List<TileState> path)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            path.Clear();

            TileState source = board.FindTileByRole(TileRole.Source);
            TileState target = board.FindTileByRole(TileRole.Target);

            if (source == null || target == null)
            {
                return false;
            }

            Queue<TileState> queue = new Queue<TileState>();
            bool[,] visited = new bool[board.Width, board.Height];
            TileState[,] predecessors =
                new TileState[board.Width, board.Height];

            queue.Enqueue(source);
            visited[source.X, source.Y] = true;

            while (queue.Count > 0)
            {
                TileState current = queue.Dequeue();

                if (current == target)
                {
                    BuildPath(source, target, predecessors, path);
                    return true;
                }

                foreach (Direction direction in Directions)
                {
                    if (!TryGetConnectedNeighbor(
                            board,
                            current,
                            direction,
                            out TileState neighbor) ||
                        visited[neighbor.X, neighbor.Y])
                    {
                        continue;
                    }

                    visited[neighbor.X, neighbor.Y] = true;
                    predecessors[neighbor.X, neighbor.Y] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        private static bool TryGetConnectedNeighbor(
            BoardState board,
            TileState current,
            Direction direction,
            out TileState neighbor)
        {
            neighbor = null;

            if (!current.Connections.Has(direction.ToMask()))
            {
                return false;
            }

            int neighborX = current.X + direction.DeltaX();
            int neighborY = current.Y + direction.DeltaY();
            neighbor = board.GetTile(neighborX, neighborY);

            return neighbor != null &&
                   neighbor.Connections.Has(
                       direction.Opposite().ToMask()
                   );
        }

        private static void BuildPath(
            TileState source,
            TileState target,
            TileState[,] predecessors,
            List<TileState> path)
        {
            TileState current = target;

            while (current != null)
            {
                path.Add(current);

                if (current == source)
                {
                    break;
                }

                current = predecessors[current.X, current.Y];
            }

            path.Reverse();
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
