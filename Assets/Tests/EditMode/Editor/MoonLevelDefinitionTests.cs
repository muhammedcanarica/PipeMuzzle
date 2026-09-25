using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class MoonLevelDefinitionTests
    {
        private static readonly (int width, int height)[] ExpectedSizes =
        {
            (5, 5),
            (5, 5),
            (5, 5),
            (5, 5),
            (5, 5),
            (5, 5),
            (5, 5),
            (5, 5),
            (5, 5),
            (6, 5),
            (6, 5),
            (6, 5)
        };

        private static readonly Vector2Int[] ExpectedSources =
        {
            new(0, 1),
            new(2, 0),
            new(4, 1),
            new(1, 4),
            new(0, 3),
            new(4, 4),
            new(3, 0),
            new(0, 2),
            new(4, 2),
            new(1, 0),
            new(5, 1),
            new(0, 4)
        };

        private static readonly Vector2Int[] ExpectedTargets =
        {
            new(2, 4),
            new(4, 3),
            new(0, 3),
            new(3, 0),
            new(4, 2),
            new(0, 1),
            new(1, 4),
            new(4, 4),
            new(0, 0),
            new(5, 3),
            new(0, 4),
            new(5, 0)
        };

        private static readonly int[] ExpectedSolvedPathLengths =
        {
            22, 20, 19, 23, 24, 24, 23, 23, 23, 24, 25, 22
        };

        private static readonly int[] MinimumRotationLowerBounds =
        {
            13, 14, 15, 16, 17, 18, 18, 19, 20, 21, 22, 23
        };

        // Test-only intended rotations; production LevelDefinition assets contain no solution data.
        private static readonly int[][] SolutionRotations =
        {
            new[] { 0, 0, 1, 1, 3, 3, 0, 0, 1, 2, 1, 2, 1, 1, 3, 0, 1, 1, 1, 1, 0, 1, 3, 0, 0 },
            new[] { 0, 1, 2, 0, 3, 1, 1, 1, 2, 0, 0, 1, 1, 1, 2, 1, 1, 1, 1, 3, 0, 0, 0, 0, 0 },
            new[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 3, 1, 1, 1, 1, 3, 3, 0, 3, 0, 2, 1, 1, 0, 1, 0 },
            new[] { 3, 3, 0, 1, 3, 0, 1, 1, 3, 0, 3, 3, 0, 0, 0, 0, 2, 0, 0, 0, 1, 3, 1, 1, 2 },
            new[] { 0, 1, 1, 1, 3, 0, 0, 1, 1, 2, 0, 1, 1, 3, 0, 2, 0, 1, 2, 0, 0, 0, 1, 1, 1 },
            new[] { 0, 1, 1, 1, 3, 2, 0, 1, 3, 0, 0, 2, 0, 2, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 3 },
            new[] { 0, 1, 1, 2, 0, 1, 1, 1, 1, 3, 0, 1, 1, 1, 2, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1 },
            new[] { 0, 1, 1, 1, 3, 0, 0, 1, 1, 2, 2, 1, 1, 1, 3, 0, 0, 1, 1, 0, 0, 0, 1, 1, 3 },
            new[] { 1, 1, 1, 3, 0, 0, 1, 1, 2, 0, 0, 0, 1, 1, 3, 0, 1, 1, 1, 3, 0, 0, 1, 1, 1 },
            new[] { 0, 2, 0, 1, 1, 3, 1, 1, 2, 0, 1, 2, 0, 1, 1, 2, 0, 0, 0, 1, 1, 1, 2, 1, 0, 0, 0, 0, 0, 0 },
            new[] { 0, 3, 0, 0, 0, 3, 0, 3, 1, 1, 2, 1, 1, 2, 3, 3, 2, 3, 1, 1, 2, 1, 1, 0, 1, 1, 1, 1, 1, 2 },
            new[] { 0, 1, 1, 1, 3, 0, 0, 0, 1, 1, 2, 0, 0, 1, 1, 1, 3, 0, 0, 0, 0, 1, 0, 0, 2, 0, 0, 1, 1, 1 }
        };

        [TestCaseSource(nameof(LevelNumbers))]
        public void MoonLevelIsDenseWithUniqueInBoundsTiles(int number)
        {
            LevelDefinition level = LoadLevel(number);
            (int width, int height) expected = ExpectedSizes[number - 1];
            HashSet<(int x, int y)> coordinates = new();

            Assert.That(level.Width, Is.EqualTo(expected.width));
            Assert.That(level.Height, Is.EqualTo(expected.height));
            Assert.That(level.Tiles, Has.Count.EqualTo(level.Width * level.Height));

            foreach (TileDefinition tile in level.Tiles)
            {
                Assert.That(tile, Is.Not.Null);
                Assert.That(tile.X, Is.InRange(0, level.Width - 1));
                Assert.That(tile.Y, Is.InRange(0, level.Height - 1));
                Assert.That(coordinates.Add((tile.X, tile.Y)), Is.True,
                    $"Moon {number} duplicates ({tile.X},{tile.Y}).");
            }
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void MoonLevelHasLockedEndpointsAndRotatablePipes(int number)
        {
            LevelDefinition level = LoadLevel(number);
            TileDefinition[] sources = level.Tiles.Where(tile => tile.Role == TileRole.Source).ToArray();
            TileDefinition[] targets = level.Tiles.Where(tile => tile.Role == TileRole.Target).ToArray();
            Assert.That(sources, Has.Length.EqualTo(1));
            Assert.That(targets, Has.Length.EqualTo(1));
            Assert.That(sources[0].IsLocked, Is.True);
            Assert.That(targets[0].IsLocked, Is.True);
            Assert.That(new Vector2Int(sources[0].X, sources[0].Y),
                Is.EqualTo(ExpectedSources[number - 1]));
            Assert.That(new Vector2Int(targets[0].X, targets[0].Y),
                Is.EqualTo(ExpectedTargets[number - 1]));
            Assert.That(level.Tiles.Where(tile => tile.Role == TileRole.Normal),
                Has.All.Matches<TileDefinition>(tile => !tile.IsLocked && tile.Shape != TileShape.Empty));
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void MoonLevelStartsUnsolvedAndIntendedRotationsSolve(int number)
        {
            LevelDefinition level = LoadLevel(number);
            BoardState board = BoardBuilder.Build(level);
            Assert.That(ConnectionChecker.Evaluate(board), Is.False,
                $"Moon {number} starts solved.");

            int[] rotations = SolutionRotations[number - 1];
            Assert.That(rotations, Has.Length.EqualTo(level.Width * level.Height));
            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    TileState tile = board.GetTile(x, y);
                    int expected = rotations[y * level.Width + x];
                    if (tile.IsLocked)
                    {
                        Assert.That(tile.Rotation, Is.EqualTo(expected));
                        continue;
                    }
                    for (int turn = 0; tile.Rotation != expected && turn < 4; turn++)
                        Assert.That(tile.RotateClockwise(), Is.True);
                    Assert.That(tile.Rotation, Is.EqualTo(expected));
                }
            }

            Assert.That(ConnectionChecker.Evaluate(board), Is.True);
            List<TileState> path = new();
            Assert.That(ConnectionChecker.TryGetSolvedPath(board, path), Is.True);
            Assert.That(path, Has.Count.EqualTo(ExpectedSolvedPathLengths[number - 1]));
            Assert.That(path.Count, Is.LessThan(level.Tiles.Count));
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void MoonLevelCannotBeSolvedWithOneOrTwoRotations(int number)
        {
            LevelDefinition level = LoadLevel(number);
            BoardState board = BoardBuilder.Build(level);
            foreach (TileDefinition definition in level.Tiles)
            {
                if (definition.IsLocked)
                    continue;

                TileState tile = board.GetTile(definition.X, definition.Y);
                Assert.That(tile.RotateClockwise(), Is.True);
                Assert.That(ConnectionChecker.Evaluate(board), Is.False,
                    $"Moon {number} solves by rotating ({tile.X},{tile.Y}) once.");
                foreach (TileDefinition secondDefinition in level.Tiles)
                {
                    if (secondDefinition.IsLocked)
                        continue;

                    TileState second = board.GetTile(secondDefinition.X, secondDefinition.Y);
                    Assert.That(second.RotateClockwise(), Is.True);
                    Assert.That(ConnectionChecker.Evaluate(board), Is.False,
                        $"Moon {number} solves in two rotations: " +
                        $"({tile.X},{tile.Y}), ({second.X},{second.Y}).");
                    for (int turn = 0; turn < 3; turn++)
                        Assert.That(second.RotateClockwise(), Is.True);
                }
                for (int turn = 0; turn < 3; turn++)
                    Assert.That(tile.RotateClockwise(), Is.True);
            }
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void MoonLevelHasNoLowRotationShortcut(int number)
        {
            BoardState board = BoardBuilder.Build(LoadLevel(number));
            int lowerBound = MinimumRequiredRotationsLowerBound(board);
            Assert.That(lowerBound, Is.LessThan(int.MaxValue));
            Assert.That(lowerBound,
                Is.GreaterThanOrEqualTo(MinimumRotationLowerBounds[number - 1]),
                $"Moon {number} has an easier route than its authored difficulty.");
        }

        [Test]
        public void MoonWorldContainsTwelveDistinctPlayableLevelsInOrder()
        {
            WorldDefinition moon = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/MoonShrine.asset");
            Assert.That(moon, Is.Not.Null);
            Assert.That(moon.LevelCount, Is.EqualTo(12));
            Assert.That(moon.IsContentReady, Is.True);
            Assert.That(moon.Levels, Has.All.Not.Null);
            Assert.That(moon.Levels.Distinct().Count(), Is.EqualTo(12));
            Assert.That(moon.Levels.Select(level => level.name),
                Is.EqualTo(Enumerable.Range(1, 12).Select(number => $"Moon_Level_{number:000}")));
            foreach (LevelDefinition level in moon.Levels)
                Assert.That(AssetDatabase.GetAssetPath(level),
                    Does.StartWith("Assets/Scripts/Data/MoonLevels/"));
            foreach (string otherWorldPath in new[]
            {
                "Assets/Resources/Worlds/SakuraGarden.asset",
                "Assets/Resources/Worlds/BambooWorkshop.asset"
            })
            {
                WorldDefinition other = AssetDatabase.LoadAssetAtPath<WorldDefinition>(otherWorldPath);
                Assert.That(other.IsContentReady, Is.True);
                Assert.That(moon.Levels.Intersect(other.Levels), Is.Empty);
            }
        }

        private static IEnumerable<int> LevelNumbers() => Enumerable.Range(1, 12);

        private static LevelDefinition LoadLevel(int number)
        {
            string path = $"Assets/Scripts/Data/MoonLevels/Moon_Level_{number:000}.asset";
            LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            Assert.That(level, Is.Not.Null, $"Missing {path}.");
            return level;
        }

        // A relaxed shortest-path search: a revisited tile may choose a new
        // orientation for free, so the result cannot overstate real click cost.
        private static int MinimumRequiredRotationsLowerBound(BoardState board)
        {
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };
            ConnectionMask[] ports =
            {
                ConnectionMask.North, ConnectionMask.East,
                ConnectionMask.South, ConnectionMask.West
            };
            int stateCount = board.Width * board.Height * 4;
            int[] distances = Enumerable.Repeat(int.MaxValue, stateCount).ToArray();
            bool[] visited = new bool[stateCount];
            TileState source = board.FindTileByRole(TileRole.Source);
            TileState target = board.FindTileByRole(TileRole.Target);
            Assert.That(source, Is.Not.Null);
            Assert.That(target, Is.Not.Null);

            for (int direction = 0; direction < 4; direction++)
            {
                if (!source.Connections.Has(ports[direction]))
                    continue;
                int x = source.X + dx[direction];
                int y = source.Y + dy[direction];
                if (board.GetTile(x, y) != null)
                    distances[(y * board.Width + x) * 4 + (direction + 2) % 4] = 0;
            }

            for (int step = 0; step < stateCount; step++)
            {
                int current = -1;
                for (int state = 0; state < stateCount; state++)
                    if (!visited[state] && distances[state] != int.MaxValue &&
                        (current < 0 || distances[state] < distances[current]))
                        current = state;
                if (current < 0)
                    break;

                visited[current] = true;
                int cell = current / 4;
                int entry = current % 4;
                int x = cell % board.Width;
                int y = cell / board.Width;
                TileState tile = board.GetTile(x, y);
                if (tile == target && tile.Connections.Has(ports[entry]))
                    return distances[current];

                for (int exit = 0; exit < 4; exit++)
                {
                    if (exit == entry)
                        continue;
                    int cost = int.MaxValue;
                    for (int rotation = 0; rotation < 4; rotation++)
                    {
                        if (tile.IsLocked && rotation != tile.Rotation)
                            continue;
                        ConnectionMask mask = tile.Shape.GetBaseConnections();
                        for (int turn = 0; turn < rotation; turn++)
                            mask = mask.RotateClockwise();
                        if (mask.Has(ports[entry]) && mask.Has(ports[exit]))
                            cost = System.Math.Min(cost,
                                (rotation - tile.Rotation + 4) % 4);
                    }
                    if (cost == int.MaxValue)
                        continue;
                    int nextX = x + dx[exit];
                    int nextY = y + dy[exit];
                    if (board.GetTile(nextX, nextY) == null)
                        continue;
                    int next = (nextY * board.Width + nextX) * 4 + (exit + 2) % 4;
                    distances[next] = System.Math.Min(distances[next],
                        distances[current] + cost);
                }
            }
            return int.MaxValue;
        }
    }
}
