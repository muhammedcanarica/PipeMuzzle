using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class BambooLevelDefinitionTests
    {
        private static readonly (int width, int height)[] ExpectedSizes =
        {
            (4, 4), (4, 4), (4, 4),
            (5, 4), (5, 4), (5, 4),
            (5, 5), (5, 5), (5, 5), (5, 5), (5, 5), (5, 5)
        };

        private static readonly Vector2Int[] ExpectedSources =
        {
            new(0, 1), new(1, 3), new(3, 3), new(0, 3),
            new(2, 0), new(4, 0), new(0, 2), new(2, 4),
            new(4, 2), new(0, 4), new(4, 4), new(2, 0)
        };

        private static readonly Vector2Int[] ExpectedTargets =
        {
            new(3, 2), new(2, 0), new(0, 0), new(4, 1),
            new(4, 3), new(0, 2), new(4, 4), new(0, 0),
            new(1, 0), new(4, 0), new(0, 1), new(2, 4)
        };

        private static readonly int[] ExpectedSolvedPathLengths =
        {
            11, 13, 13, 15, 16, 17, 19, 19, 20, 21, 22, 23
        };

        private static readonly int[][] SolutionRotations =
        {
            new[] { 0, 2, 2, 0, 1, 3, 0, 3, 0, 2, 0, 1, 1, 1, 2, 2 },
            new[] { 0, 1, 2, 2, 1, 1, 1, 3, 2, 0, 3, 0, 1, 0, 1, 1 },
            new[] { 3, 0, 1, 3, 0, 0, 0, 1, 1, 2, 1, 3, 0, 0, 2, 1 },
            new[] { 3, 3, 3, 0, 3, 0, 0, 2, 0, 1, 0, 2, 0, 1, 2, 2, 2, 1, 1, 2 },
            new[] { 0, 3, 0, 2, 3, 0, 0, 1, 3, 0, 0, 0, 1, 2, 2, 1, 1, 1, 1, 1 },
            new[] { 0, 3, 0, 0, 1, 0, 0, 0, 2, 3, 2, 0, 0, 1, 3, 2, 1, 1, 1, 2 },
            new[] { 2, 3, 0, 1, 3, 0, 1, 1, 3, 0, 1, 3, 0, 2, 0, 1, 1, 2, 0, 2, 1, 2, 0, 1, 1 },
            new[] { 3, 0, 1, 1, 3, 1, 2, 0, 3, 0, 3, 3, 2, 0, 0, 3, 1, 3, 0, 2, 3, 2, 0, 2, 1 },
            new[] { 2, 0, 1, 3, 1, 0, 2, 0, 3, 3, 0, 0, 1, 1, 1, 0, 1, 1, 1, 2, 0, 1, 1, 0, 2 },
            new[] { 0, 1, 1, 1, 1, 1, 1, 1, 3, 0, 1, 3, 3, 1, 0, 0, 2, 1, 3, 0, 2, 1, 3, 1, 2 },
            new[] { 2, 0, 1, 1, 3, 3, 0, 0, 1, 2, 0, 0, 0, 3, 2, 0, 0, 3, 1, 3, 0, 1, 0, 2, 1 },
            new[] { 0, 2, 0, 2, 2, 0, 1, 2, 0, 2, 1, 1, 1, 0, 0, 0, 1, 0, 3, 0, 1, 1, 3, 0, 2 }
        };

        [TestCaseSource(nameof(LevelNumbers))]
        public void BambooLevelUsesEveryBoardCellExactlyOnce(int levelNumber)
        {
            LevelDefinition level = LoadLevel(levelNumber);
            (int width, int height) expected = ExpectedSizes[levelNumber - 1];
            HashSet<(int x, int y)> coordinates = new();

            Assert.That(level.Width, Is.EqualTo(expected.width));
            Assert.That(level.Height, Is.EqualTo(expected.height));
            Assert.That(level.Tiles, Has.Count.EqualTo(level.Width * level.Height));

            foreach (TileDefinition tile in level.Tiles)
            {
                Assert.That(tile, Is.Not.Null,
                    $"Bamboo {levelNumber} contains a null tile.");
                Assert.That(tile.X, Is.InRange(0, level.Width - 1));
                Assert.That(tile.Y, Is.InRange(0, level.Height - 1));
                Assert.That(coordinates.Add((tile.X, tile.Y)), Is.True,
                    $"Bamboo {levelNumber} duplicates ({tile.X},{tile.Y}).");
            }
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void BambooLevelHasExpectedLockedEndpoints(int levelNumber)
        {
            LevelDefinition level = LoadLevel(levelNumber);
            TileDefinition[] sources = level.Tiles
                .Where(tile => tile.Role == TileRole.Source).ToArray();
            TileDefinition[] targets = level.Tiles
                .Where(tile => tile.Role == TileRole.Target).ToArray();

            Assert.That(sources, Has.Length.EqualTo(1));
            Assert.That(targets, Has.Length.EqualTo(1));
            Assert.That(sources[0].IsLocked, Is.True);
            Assert.That(targets[0].IsLocked, Is.True);
            Assert.That(new Vector2Int(sources[0].X, sources[0].Y),
                Is.EqualTo(ExpectedSources[levelNumber - 1]));
            Assert.That(new Vector2Int(targets[0].X, targets[0].Y),
                Is.EqualTo(ExpectedTargets[levelNumber - 1]));
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void BambooLevelNormalTilesAreRotatablePipes(int levelNumber)
        {
            LevelDefinition level = LoadLevel(levelNumber);
            IEnumerable<TileDefinition> normalTiles = level.Tiles
                .Where(tile => tile.Role == TileRole.Normal);

            Assert.That(normalTiles, Has.All.Matches<TileDefinition>(tile =>
                !tile.IsLocked && tile.Shape != TileShape.Empty));
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void BambooLevelStartsUnsolvedAndFixtureSolves(int levelNumber)
        {
            LevelDefinition level = LoadLevel(levelNumber);
            BoardState board = BoardBuilder.Build(level);

            Assert.That(ConnectionChecker.Evaluate(board), Is.False,
                $"Bamboo {levelNumber} starts solved.");

            ApplySolution(board, SolutionRotations[levelNumber - 1]);

            Assert.That(ConnectionChecker.Evaluate(board), Is.True,
                $"Bamboo {levelNumber} fixture does not solve the board.");

            List<TileState> solvedPath = new();
            Assert.That(ConnectionChecker.TryGetSolvedPath(board, solvedPath),
                Is.True);
            Assert.That(solvedPath, Has.Count.EqualTo(
                ExpectedSolvedPathLengths[levelNumber - 1]));
            Assert.That(solvedPath.Count, Is.LessThan(level.Tiles.Count),
                $"Bamboo {levelNumber} needs at least one decoy tile.");
        }

        [Test]
        public void BambooWorldOwnsTwelveDistinctLevelsInExactOrder()
        {
            WorldDefinition bamboo =
                AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                    "Assets/Resources/Worlds/BambooWorkshop.asset");

            Assert.That(bamboo, Is.Not.Null);
            Assert.That(bamboo.LevelCount, Is.EqualTo(12));
            Assert.That(bamboo.IsContentReady, Is.True);
            Assert.That(bamboo.Levels, Has.All.Not.Null);
            Assert.That(bamboo.Levels.Distinct().Count(), Is.EqualTo(12));
            Assert.That(bamboo.Levels.Select(level => level.name),
                Is.EqualTo(Enumerable.Range(1, 12)
                    .Select(number => $"Bamboo_Level_{number:000}")));
        }

        [Test]
        public void MoonShrineNowContainsItsOwnPlayableLevels()
        {
            WorldDefinition moon =
                AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                    "Assets/Resources/Worlds/MoonShrine.asset");

            Assert.That(moon, Is.Not.Null);
            Assert.That(moon.LevelCount, Is.EqualTo(12));
            Assert.That(moon.IsContentReady, Is.True);
        }

        private static IEnumerable<int> LevelNumbers()
        {
            return Enumerable.Range(1, 12);
        }

        private static LevelDefinition LoadLevel(int levelNumber)
        {
            string path =
                $"Assets/Scripts/Data/BambooLevels/Bamboo_Level_{levelNumber:000}.asset";
            LevelDefinition level =
                AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);

            Assert.That(level, Is.Not.Null, $"Missing {path}.");
            return level;
        }

        private static void ApplySolution(
            BoardState board,
            IReadOnlyList<int> rotations)
        {
            Assert.That(rotations.Count, Is.EqualTo(
                board.Width * board.Height));

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    TileState tile = board.GetTile(x, y);
                    Assert.That(tile, Is.Not.Null);
                    int expected = rotations[y * board.Width + x];

                    if (tile.IsLocked)
                    {
                        Assert.That(tile.Rotation, Is.EqualTo(expected));
                        continue;
                    }

                    int safety = 0;
                    while (tile.Rotation != expected && safety++ < 4)
                    {
                        Assert.That(tile.RotateClockwise(), Is.True);
                    }

                    Assert.That(tile.Rotation, Is.EqualTo(expected));
                }
            }
        }
    }
}
