using System.Collections.Generic;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Editor
{
    internal static class LevelValidationUtility
    {
        [MenuItem("PipeMuzzle/Validate All Levels")]
        private static void ValidateAllLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelDefinition");
            foreach (string guid in guids)
            {
                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));
                BoardState board = BoardBuilder.Build(level);
                int sources = CountRole(level, TileRole.Source);
                int targets = CountRole(level, TileRole.Target);
                bool initiallySolved = ConnectionChecker.Evaluate(board);
                bool solvable = HasPossiblePath(board);

                Debug.Log($"{level.name}: source={sources}, target={targets}, " +
                    $"tiles={level.Tiles.Count}, locked={CountLocked(level)}, " +
                    $"solvable={solvable}, initiallySolved={initiallySolved}", level);
            }
        }

        private static bool HasPossiblePath(BoardState board)
        {
            TileState source = board.FindTileByRole(TileRole.Source);
            TileState target = board.FindTileByRole(TileRole.Target);
            if (source == null || target == null)
            {
                return false;
            }

            Queue<(TileState current, TileState previous)> queue = new();
            HashSet<string> visitedEdges = new();
            queue.Enqueue((source, null));

            while (queue.Count > 0)
            {
                (TileState current, TileState previous) = queue.Dequeue();
                if (current == target)
                {
                    return true;
                }

                foreach (Direction direction in Directions)
                {
                    TileState neighbor = board.GetTile(
                        current.X + direction.DeltaX(),
                        current.Y + direction.DeltaY());
                    if (neighbor == null || !CanExit(current, previous, direction))
                    {
                        continue;
                    }

                    if (neighbor == target &&
                        !neighbor.Connections.Has(direction.Opposite().ToMask()))
                    {
                        continue;
                    }

                    string edge = $"{current.X},{current.Y}>{neighbor.X},{neighbor.Y}";
                    if (visitedEdges.Add(edge))
                    {
                        queue.Enqueue((neighbor, current));
                    }
                }
            }

            return false;
        }

        private static bool CanExit(
            TileState current,
            TileState previous,
            Direction direction)
        {
            if (current.Role == TileRole.Source || current.IsLocked)
            {
                return current.Connections.Has(direction.ToMask());
            }

            if (previous == null)
            {
                return false;
            }

            Direction incoming = DirectionBetween(current, previous);
            for (int rotation = 0; rotation < 4; rotation++)
            {
                ConnectionMask mask = current.Shape.GetBaseConnections();
                for (int step = 0; step < rotation; step++)
                {
                    mask = mask.RotateClockwise();
                }

                if (mask.Has(incoming.ToMask()) && mask.Has(direction.ToMask()))
                {
                    return true;
                }
            }

            return false;
        }

        private static Direction DirectionBetween(TileState from, TileState to)
        {
            foreach (Direction direction in Directions)
            {
                if (from.X + direction.DeltaX() == to.X &&
                    from.Y + direction.DeltaY() == to.Y)
                {
                    return direction;
                }
            }

            throw new System.ArgumentException("Tiles must be adjacent.");
        }

        private static int CountRole(LevelDefinition level, TileRole role)
        {
            int count = 0;
            foreach (TileDefinition tile in level.Tiles)
            {
                if (tile.Role == role) count++;
            }
            return count;
        }

        private static int CountLocked(LevelDefinition level)
        {
            int count = 0;
            foreach (TileDefinition tile in level.Tiles)
            {
                if (tile.IsLocked) count++;
            }
            return count;
        }

        private static readonly Direction[] Directions =
        {
            Direction.North, Direction.East, Direction.South, Direction.West
        };
    }
}
