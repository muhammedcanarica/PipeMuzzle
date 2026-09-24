using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEditor;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class SakuraLevelDefinitionTests
    {
        private static readonly (int width, int height)[] ExpectedSizes =
        {
            (3, 3), (4, 3), (4, 3), (4, 4), (4, 4), (4, 4),
            (4, 4), (5, 4), (5, 4), (5, 5), (5, 5), (5, 5)
        };

        private static readonly int[] MinimumSolutionPathLengths =
        {
            3, 6, 8, 9, 10, 10, 10, 11, 11, 13, 15, 15
        };

        [TestCaseSource(nameof(LevelNumbers))]
        public void SakuraLevelUsesEveryBoardCellExactlyOnce(int levelNumber)
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
                    $"Level {levelNumber} contains a null tile definition.");
                Assert.That(tile.X, Is.InRange(0, level.Width - 1));
                Assert.That(tile.Y, Is.InRange(0, level.Height - 1));
                Assert.That(coordinates.Add((tile.X, tile.Y)), Is.True,
                    $"Level {levelNumber} duplicates ({tile.X},{tile.Y}).");
            }
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void SakuraLevelHasOneLockedSourceAndTarget(int levelNumber)
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
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void SakuraLevelNormalTilesAreRotatablePipes(int levelNumber)
        {
            LevelDefinition level = LoadLevel(levelNumber);
            IEnumerable<TileDefinition> normalTiles = level.Tiles
                .Where(tile => tile.Role == TileRole.Normal);

            Assert.That(normalTiles, Has.All.Matches<TileDefinition>(tile =>
                !tile.IsLocked && tile.Shape != TileShape.Empty));
        }

        [TestCaseSource(nameof(LevelNumbers))]
        public void SakuraLevelStartsUnsolvedAndHasValidSolution(
            int levelNumber)
        {
            LevelDefinition level = LoadLevel(levelNumber);
            BoardState board = BoardBuilder.Build(level);
            int solutionPathLength = FindShortestPossiblePathLength(board);

            Assert.That(ConnectionChecker.Evaluate(board), Is.False,
                $"Level {levelNumber} starts solved.");
            Assert.That(solutionPathLength, Is.GreaterThan(0),
                $"Level {levelNumber} has no rotatable Source-to-Target path.");
            Assert.That(solutionPathLength,
                Is.GreaterThanOrEqualTo(
                    MinimumSolutionPathLengths[levelNumber - 1]),
                $"Level {levelNumber} has a trivial alternate solution.");
            Assert.That(solutionPathLength, Is.LessThan(level.Tiles.Count),
                $"Level {levelNumber} has no decoy outside its shortest path.");
        }

        private static IEnumerable<int> LevelNumbers()
        {
            return Enumerable.Range(1, 12);
        }

        private static LevelDefinition LoadLevel(int levelNumber)
        {
            string path =
                $"Assets/Scripts/Data/Level_{levelNumber:000}.asset";
            LevelDefinition level =
                AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);

            Assert.That(level, Is.Not.Null, $"Missing {path}.");
            return level;
        }

        private static int FindShortestPossiblePathLength(BoardState board)
        {
            TileState source = board.FindTileByRole(TileRole.Source);
            TileState target = board.FindTileByRole(TileRole.Target);
            if (source == null || target == null)
            {
                return 0;
            }

            Queue<(TileState current, TileState previous, int length)> queue =
                new();
            HashSet<(int x, int y, int previousX, int previousY)> visited =
                new();
            queue.Enqueue((source, null, 1));

            while (queue.Count > 0)
            {
                (TileState current, TileState previous, int length) =
                    queue.Dequeue();
                if (current == target)
                {
                    return length;
                }

                foreach (Direction direction in Directions)
                {
                    TileState neighbor = board.GetTile(
                        current.X + direction.DeltaX(),
                        current.Y + direction.DeltaY());
                    if (neighbor == null ||
                        !CanConnect(current, previous, direction))
                    {
                        continue;
                    }

                    if (neighbor == target &&
                        !neighbor.Connections.Has(
                            direction.Opposite().ToMask()))
                    {
                        continue;
                    }

                    var state = (
                        neighbor.X, neighbor.Y, current.X, current.Y);
                    if (visited.Add(state))
                    {
                        queue.Enqueue((neighbor, current, length + 1));
                    }
                }
            }

            return 0;
        }

        private static bool CanConnect(
            TileState current,
            TileState previous,
            Direction outgoing)
        {
            if (current.Role == TileRole.Source || current.IsLocked)
            {
                return current.Connections.Has(outgoing.ToMask());
            }

            Direction incoming = DirectionBetween(current, previous);
            ConnectionMask connections = current.Shape.GetBaseConnections();

            for (int rotation = 0; rotation < 4; rotation++)
            {
                if (connections.Has(incoming.ToMask()) &&
                    connections.Has(outgoing.ToMask()))
                {
                    return true;
                }

                connections = connections.RotateClockwise();
            }

            return false;
        }

        private static Direction DirectionBetween(
            TileState from,
            TileState to)
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

        private static readonly Direction[] Directions =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };
    }
}
