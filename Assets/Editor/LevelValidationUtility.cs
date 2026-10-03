using System;
using System.Collections.Generic;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Editor
{
    internal sealed class LevelValidationResult
    {
        public List<string> Errors { get; } = new();
        public int ActiveTiles { get; internal set; }
        public int MinimumMoves { get; internal set; } = int.MaxValue;
        public int ShortestPathLength { get; internal set; } = int.MaxValue;
        public int RouteCount { get; internal set; }
        public int ReachableTiles { get; internal set; }
        public bool SearchComplete { get; internal set; } = true;
        public List<Vector2Int> SolutionPath { get; } = new();
        public Dictionary<Vector2Int, int> SolutionRotations { get; } = new();
    }

    // Editor-only exhaustive solver: a route assigns one consistent orientation
    // per tile, preventing false solutions that reuse corners with different ports.
    internal static class LevelValidationUtility
    {
        private static readonly Direction[] Directions =
            { Direction.North, Direction.East, Direction.South, Direction.West };
        private const int MaximumSearchNodes = 200000;

        [MenuItem("PipeMuzzle/Validate All Levels")]
        private static void ValidateAllLevels()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition"))
            {
                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                LevelValidationResult result = Analyze(level);
                if (result.Errors.Count > 0)
                {
                    Debug.LogError($"{level.name}: {string.Join("; ", result.Errors)}", level);
                    continue;
                }
                float density = (float)result.ActiveTiles / (level.Width * level.Height);
                Debug.Log($"{level.name}: board={level.Width}x{level.Height}, pipes={result.ActiveTiles}, " +
                    $"density={density:P0}, routes={result.RouteCount}, path={result.ShortestPathLength}, " +
                    $"distractors={result.ActiveTiles - result.ShortestPathLength}, " +
                    $"minimum clockwise moves={result.MinimumMoves}", level);
            }
        }

        internal static LevelValidationResult Analyze(LevelDefinition level)
        {
            LevelValidationResult result = new();
            if (level == null) { result.Errors.Add("Missing level."); return result; }
            if (level.Width < 1 || level.Height < 1 || (long)level.Width * level.Height > 64)
                result.Errors.Add("Invalid board dimensions (editor search supports up to 64 cells).");
            if (level.Tiles == null) { result.Errors.Add("Missing tile list."); return result; }
            HashSet<Vector2Int> coordinates = new();
            int sources = 0, targets = 0;
            foreach (TileDefinition tile in level.Tiles)
            {
                if (tile == null) { result.Errors.Add("Null tile."); continue; }
                Vector2Int pos = new(tile.X, tile.Y);
                if (!coordinates.Add(pos)) result.Errors.Add($"Duplicate tile {pos}.");
                if (tile.X < 0 || tile.X >= level.Width || tile.Y < 0 || tile.Y >= level.Height)
                    result.Errors.Add($"Out-of-bounds tile {pos}.");
                bool valid = Enum.IsDefined(typeof(TileShape), tile.Shape) && Enum.IsDefined(typeof(TileRole), tile.Role) &&
                    tile.StartRotation >= 0 && tile.StartRotation <= 3;
                if (!valid) { result.Errors.Add($"Invalid tile data at {pos}."); continue; }
                if (tile.Role == TileRole.Source) sources++;
                if (tile.Role == TileRole.Target) targets++;
                if (tile.Role != TileRole.Normal && (!tile.IsLocked || tile.Shape == TileShape.Empty))
                    result.Errors.Add($"Endpoint {pos} must be a locked pipe.");
                // Authored spawn ports stay inside the board. Wrong ports during
                // player rotations remain permitted by existing gameplay rules.
                ConnectionMask mask = Mask(tile.Shape, tile.StartRotation);
                foreach (Direction direction in Directions)
                    if (mask.Has(direction.ToMask()) &&
                        (tile.X + direction.DeltaX() < 0 || tile.X + direction.DeltaX() >= level.Width ||
                         tile.Y + direction.DeltaY() < 0 || tile.Y + direction.DeltaY() >= level.Height))
                        result.Errors.Add($"Initial port leaves board at {pos} toward {direction}.");
            }
            if (coordinates.Count != (long)level.Width * level.Height)
                result.Errors.Add("Every cell must have one definition; unused cells use Empty.");
            if (sources != 1 || targets != 1) result.Errors.Add("Expected exactly one source and one target.");
            if (result.Errors.Count > 0) return result;
            BoardState board = BoardBuilder.Build(level);
            result = Analyze(board);
            if (ConnectionChecker.Evaluate(board)) result.Errors.Add("Level starts solved.");
            if (!result.SearchComplete) result.Errors.Add("Search budget exceeded; metrics are incomplete.");
            else if (result.RouteCount == 0) result.Errors.Add("No consistent source-to-target solution.");
            return result;
        }

        internal static LevelValidationResult Analyze(BoardState board)
        {
            LevelValidationResult result = new();
            if (board == null) return result;
            TileState source = board.FindTileByRole(TileRole.Source), target = board.FindTileByRole(TileRole.Target);
            if (source == null || target == null) return result;
            new Search(board, source, target, result).Run();
            return result;
        }

        private static ConnectionMask Mask(TileShape shape, int rotation)
        {
            ConnectionMask mask = shape.GetBaseConnections();
            for (int turn = 0; turn < rotation; turn++) mask = mask.RotateClockwise();
            return mask;
        }

        private sealed class Search
        {
            private readonly BoardState board;
            private readonly TileState source, target;
            private readonly LevelValidationResult result;
            private readonly TileState[] tiles;
            private readonly int[,,] costs, rotations;
            private readonly int[,] entryCosts, entryRotations;
            private readonly bool[] visited, reachable;
            private readonly List<int> route = new();
            private readonly int[] chosenRotations;
            private int nodes;

            internal Search(BoardState board, TileState source, TileState target, LevelValidationResult result)
            {
                this.board = board; this.source = source; this.target = target; this.result = result;
                int count = board.Width * board.Height;
                tiles = new TileState[count];
                costs = new int[count, 4, 4]; rotations = new int[count, 4, 4];
                entryCosts = new int[count, 4]; entryRotations = new int[count, 4];
                visited = new bool[count]; reachable = new bool[count]; chosenRotations = new int[count];
                for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    int index = y * board.Width + x;
                    TileState tile = tiles[index] = board.GetTile(x, y);
                    if (tile != null && tile.Shape != TileShape.Empty) result.ActiveTiles++;
                    for (int entry = 0; entry < 4; entry++)
                    {
                        entryCosts[index, entry] = int.MaxValue;
                        for (int exit = 0; exit < 4; exit++) costs[index, entry, exit] = int.MaxValue;
                        if (tile == null || tile.Shape == TileShape.Empty) continue;
                        for (int rotation = 0; rotation < 4; rotation++)
                        {
                            if (tile.IsLocked && rotation != tile.Rotation) continue;
                            ConnectionMask mask = Mask(tile.Shape, rotation);
                            if (!mask.Has(Directions[entry].ToMask())) continue;
                            int cost = (rotation - tile.Rotation + 4) % 4;
                            if (cost < entryCosts[index, entry])
                            { entryCosts[index, entry] = cost; entryRotations[index, entry] = rotation; }
                            for (int exit = 0; exit < 4; exit++)
                                if (exit != entry && mask.Has(Directions[exit].ToMask()) && cost < costs[index, entry, exit])
                                { costs[index, entry, exit] = cost; rotations[index, entry, exit] = rotation; }
                        }
                    }
                }
            }

            internal void Run()
            {
                int index = source.Y * board.Width + source.X;
                visited[index] = reachable[index] = true;
                chosenRotations[index] = source.Rotation;
                route.Add(index);
                for (int direction = 0; direction < 4 && result.SearchComplete; direction++)
                {
                    if (!source.Connections.Has(Directions[direction].ToMask())) continue;
                    int neighbor = Neighbor(source, direction);
                    if (neighbor >= 0) Visit(neighbor, (direction + 2) % 4, 0);
                }
                foreach (bool reached in reachable) if (reached) result.ReachableTiles++;
            }

            private int Neighbor(TileState tile, int direction)
            {
                int x = tile.X + Directions[direction].DeltaX(), y = tile.Y + Directions[direction].DeltaY();
                if (!board.IsInside(x, y)) return -1;
                int index = y * board.Width + x;
                return tiles[index] != null && tiles[index].Shape != TileShape.Empty ? index : -1;
            }

            private void Visit(int index, int entry, int moves)
            {
                if (++nodes > MaximumSearchNodes) { result.SearchComplete = false; return; }
                if (visited[index] || entryCosts[index, entry] == int.MaxValue) return;
                visited[index] = reachable[index] = true;
                route.Add(index);
                TileState tile = tiles[index];
                if (tile == target)
                {
                    result.RouteCount++;
                    result.ShortestPathLength = Math.Min(result.ShortestPathLength, route.Count);
                    int total = moves + entryCosts[index, entry];
                    if (total < result.MinimumMoves)
                    {
                        result.MinimumMoves = total;
                        chosenRotations[index] = entryRotations[index, entry];
                        result.SolutionPath.Clear(); result.SolutionRotations.Clear();
                        foreach (int cell in route)
                        {
                            Vector2Int pos = new(tiles[cell].X, tiles[cell].Y);
                            result.SolutionPath.Add(pos); result.SolutionRotations[pos] = chosenRotations[cell];
                        }
                    }
                }
                else for (int exit = 0; exit < 4 && result.SearchComplete; exit++)
                {
                    int cost = costs[index, entry, exit];
                    if (cost == int.MaxValue) continue;
                    int next = Neighbor(tile, exit);
                    if (next < 0 || visited[next]) continue;
                    chosenRotations[index] = rotations[index, entry, exit];
                    Visit(next, (exit + 2) % 4, moves + cost);
                }
                route.RemoveAt(route.Count - 1);
                visited[index] = false;
            }
        }
    }
}
