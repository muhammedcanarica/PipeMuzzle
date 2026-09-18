using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using UnityEditor;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class Level002DefinitionTests
    {
        private const string LevelPath =
            "Assets/Scripts/Data/Level_002.asset";

        [Test]
        public void SakuraLevel2UsesEveryBoardCellExactlyOnce()
        {
            LevelDefinition level = LoadLevel();
            HashSet<(int x, int y)> coordinates = new();

            Assert.That(level.Width, Is.EqualTo(4));
            Assert.That(level.Height, Is.EqualTo(3));
            Assert.That(level.Tiles, Has.Count.EqualTo(12));

            foreach (TileDefinition tile in level.Tiles)
            {
                Assert.That(tile.X, Is.InRange(0, level.Width - 1));
                Assert.That(tile.Y, Is.InRange(0, level.Height - 1));
                Assert.That(coordinates.Add((tile.X, tile.Y)), Is.True,
                    $"Duplicate tile at ({tile.X},{tile.Y}).");
            }

            Assert.That(coordinates, Has.Count.EqualTo(12));
        }

        [Test]
        public void SakuraLevel2HasSeparatedLockedEndpointsAndRotatablePipes()
        {
            LevelDefinition level = LoadLevel();
            TileDefinition source = level.Tiles.Single(
                tile => tile.Role == TileRole.Source);
            TileDefinition target = level.Tiles.Single(
                tile => tile.Role == TileRole.Target);

            Assert.That(source.IsLocked, Is.True);
            Assert.That(target.IsLocked, Is.True);
            Assert.That(
                System.Math.Abs(source.X - target.X) +
                System.Math.Abs(source.Y - target.Y),
                Is.GreaterThan(1));
            Assert.That(
                level.Tiles.Where(tile => tile.Role == TileRole.Normal),
                Has.All.Matches<TileDefinition>(tile =>
                    !tile.IsLocked && tile.Shape != TileShape.Empty));
        }

        [Test]
        public void SakuraLevel2StartsUnsolvedAndHasSevenMoveDesignedSolution()
        {
            BoardState board = BoardBuilder.Build(LoadLevel());

            Assert.That(ConnectionChecker.Evaluate(board), Is.False);

            Rotate(board, 1, 2, 1);
            Rotate(board, 2, 2, 2);
            Rotate(board, 2, 1, 1);
            Rotate(board, 1, 1, 1);
            Rotate(board, 1, 0, 1);
            Rotate(board, 2, 0, 1);

            Assert.That(board.MoveCount, Is.EqualTo(7));
            Assert.That(ConnectionChecker.Evaluate(board), Is.True);

            List<TileState> solvedPath = new();
            Assert.That(
                ConnectionChecker.TryGetSolvedPath(board, solvedPath),
                Is.True);
            Assert.That(
                solvedPath.Select(tile => (tile.X, tile.Y)),
                Is.EqualTo(new[]
                {
                    (0, 2), (1, 2), (2, 2), (2, 1),
                    (1, 1), (1, 0), (2, 0), (3, 0)
                }));
        }

        private static LevelDefinition LoadLevel()
        {
            LevelDefinition level =
                AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);

            Assert.That(level, Is.Not.Null);
            return level;
        }

        private static void Rotate(
            BoardState board,
            int x,
            int y,
            int times)
        {
            for (int i = 0; i < times; i++)
            {
                Assert.That(board.TryRotateTile(x, y), Is.True,
                    $"Tile at ({x},{y}) could not rotate.");
            }
        }
    }
}
