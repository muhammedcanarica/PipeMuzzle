using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.Editor;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public abstract class LevelDefinitionContentTests
    {
        protected abstract string WorldName { get; }
        protected abstract string LevelPrefix { get; }
        protected abstract (int width, int height, int path, int decoys, int moves, int tees, int crosses)[] Budgets { get; }
        public static IEnumerable<int> LevelNumbers() => Enumerable.Range(1, 12);
        private WorldDefinition World => AssetDatabase.LoadAssetAtPath<WorldDefinition>($"Assets/Resources/Worlds/{WorldName}.asset");
        private LevelDefinition Level(int number) => World.Levels[number - 1];

        [TestCaseSource(typeof(LevelDefinitionContentTests), nameof(LevelNumbers))]
        public void BoardHasUniqueInBoundsDefinitionsAndNoOutwardSpawnPorts(int number)
        {
            LevelDefinition level = Level(number);
            var budget = Budgets[number - 1];
            Assert.That(level.Width, Is.EqualTo(budget.width));
            Assert.That(level.Height, Is.EqualTo(budget.height));
            Assert.That(level.Tiles, Has.Count.EqualTo(level.Width * level.Height));
            Assert.That(level.Tiles.Select(t => (t.X, t.Y)).Distinct().Count(), Is.EqualTo(level.Tiles.Count));
            Assert.That(LevelValidationUtility.Analyze(level).Errors, Is.Empty);
        }

        [TestCaseSource(typeof(LevelDefinitionContentTests), nameof(LevelNumbers))]
        public void EndpointsAreLockedAndNormalPipesCanRotate(int number)
        {
            LevelDefinition level = Level(number);
            foreach (TileRole role in new[] { TileRole.Source, TileRole.Target })
            {
                TileDefinition[] endpoints = level.Tiles.Where(t => t.Role == role).ToArray();
                Assert.That(endpoints, Has.Length.EqualTo(1));
                Assert.That(endpoints[0].IsLocked, Is.True);
                Assert.That(endpoints[0].Shape, Is.Not.EqualTo(TileShape.Empty));
            }
            Assert.That(level.Tiles.Where(t => t.Role == TileRole.Normal),
                Has.All.Matches<TileDefinition>(t => !t.IsLocked));
        }

        [TestCaseSource(typeof(LevelDefinitionContentTests), nameof(LevelNumbers))]
        public void InitiallyUnsolvedBoardHasOneConsistentSolutionAndRealCheckerAcceptsIt(int number)
        {
            LevelDefinition level = Level(number);
            BoardState board = BoardBuilder.Build(level);
            Assert.That(ConnectionChecker.Evaluate(board), Is.False);
            LevelValidationResult result = LevelValidationUtility.Analyze(level);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.SearchComplete, Is.True);
            Assert.That(result.RouteCount, Is.EqualTo(1), "Distractor branches must not create a shortcut to the target.");
            ApplySolution(board, result);
            Assert.That(board.MoveCount, Is.EqualTo(result.MinimumMoves));
            Assert.That(ConnectionChecker.Evaluate(board), Is.True);
            List<TileState> path = new();
            Assert.That(ConnectionChecker.TryGetSolvedPath(board, path), Is.True);
            Assert.That(path.Select(t => new Vector2Int(t.X, t.Y)), Is.EqualTo(result.SolutionPath));
            foreach (TileState tile in path)
            foreach (Direction direction in new[] { Direction.North, Direction.East, Direction.South, Direction.West })
                if (tile.Connections.Has(direction.ToMask()))
                    Assert.That(board.IsInside(tile.X + direction.DeltaX(), tile.Y + direction.DeltaY()), Is.True,
                        "Solved route ports must stay inside the board.");
        }

        [TestCaseSource(typeof(LevelDefinitionContentTests), nameof(LevelNumbers))]
        public void ExactDifficultyAndPlausibleDistractorsMatchAuthoredBudget(int number)
        {
            LevelDefinition level = Level(number);
            LevelValidationResult result = LevelValidationUtility.Analyze(level);
            var budget = Budgets[number - 1];
            Assert.That(result.MinimumMoves, Is.EqualTo(budget.moves));
            Assert.That(result.ShortestPathLength, Is.EqualTo(budget.path));
            Assert.That(result.ActiveTiles - result.ShortestPathLength, Is.EqualTo(budget.decoys));
            Assert.That(result.ReachableTiles, Is.EqualTo(result.ActiveTiles),
                "Every distractor must plausibly connect to the source, rather than be isolated clutter.");
            Assert.That(level.Tiles.Count(t => t.Shape == TileShape.ThreeWay), Is.EqualTo(budget.tees));
            Assert.That(level.Tiles.Count(t => t.Shape == TileShape.Cross), Is.EqualTo(budget.crosses));
            Assert.That(result.ActiveTiles, Is.LessThan(level.Width * level.Height), "Keep breathing room on the board.");
        }

        [Test]
        public void WorldOwnsTwelveDistinctLevelsInExistingOrder()
        {
            Assert.That(World.IsContentReady, Is.True);
            Assert.That(World.LevelCount, Is.EqualTo(12));
            Assert.That(World.Levels.Distinct().Count(), Is.EqualTo(12));
            Assert.That(World.Levels.Select(t => t.name),
                Is.EqualTo(Enumerable.Range(1, 12).Select(n => $"{LevelPrefix}Level_{n:000}")));
            foreach (string name in new[] { "SakuraGarden", "BambooWorkshop", "MoonShrine" })
            {
                if (name == WorldName) continue;
                WorldDefinition other = AssetDatabase.LoadAssetAtPath<WorldDefinition>($"Assets/Resources/Worlds/{name}.asset");
                Assert.That(World.Levels.Intersect(other.Levels), Is.Empty);
            }
        }

        internal static void ApplySolution(BoardState board, LevelValidationResult result)
        {
            foreach (var entry in result.SolutionRotations)
            {
                TileState tile = board.GetTile(entry.Key.x, entry.Key.y);
                int turns = (entry.Value - tile.Rotation + 4) % 4;
                for (int i = 0; i < turns; i++) Assert.That(board.TryRotateTile(tile.X, tile.Y), Is.True);
            }
        }
    }
}
