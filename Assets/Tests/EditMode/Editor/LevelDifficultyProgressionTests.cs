using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.Editor;
using PipeMuzzle.View;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class LevelDifficultyProgressionTests
    {
        [TestCase(1, 3, 0)]
        [TestCase(2, 6, 1)]
        public void SakuraOpeningTeachesRotationWithoutCrowding(int number, int pipes, int decoys)
        {
            LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                $"Assets/Scripts/Data/Level_{number:000}.asset");
            Assert.That(level.Tiles.Count(t => t.Shape != TileShape.Empty), Is.EqualTo(pipes));
            Assert.That(level.Tiles.Count(t => t.Shape == TileShape.ThreeWay || t.Shape == TileShape.Cross), Is.Zero);
            Assert.That(level.Tiles.Count(t => t.Shape == TileShape.Empty), Is.GreaterThan(0));
            LevelValidationResult result = LevelValidationUtility.Analyze(level);
            Assert.That(result.ActiveTiles - result.ShortestPathLength, Is.EqualTo(decoys));
        }

        public static IEnumerable<TestCaseData> Levels()
        {
            foreach (string world in new[] { "SakuraGarden", "BambooWorkshop", "MoonShrine" })
            for (int number = 1; number <= 12; number++) yield return new TestCaseData(world, number);
        }

        [TestCaseSource(nameof(Levels))]
        public void EveryLevelFlowsAlongRealSolutionToTargetBeforeCompletion(string worldName, int number)
        {
            WorldDefinition world = World(worldName);
            LevelDefinition level = world.Levels[number - 1];
            LevelValidationResult result = LevelValidationUtility.Analyze(level);
            Assert.That(result.Errors, Is.Empty);
            BoardState board = BoardBuilder.Build(level);
            GameObject root = new("TestLevelFlow");
            try
            {
                BoardView view = root.AddComponent<BoardView>();
                SerializedObject settings = new(view);
                settings.FindProperty("tilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/TilePrefab.prefab").GetComponent<TileView>();
                settings.ApplyModifiedPropertiesWithoutUndo();
                view.SetGameplayTheme(world.GameplayTheme);
                view.Build(board);
                EnergyFlowView flow = root.GetComponent<EnergyFlowView>();
                Assert.That(flow.IsPlaying, Is.False, "Unsolved boards have empty channels.");
                LevelDefinitionContentTests.ApplySolution(board, result);
                Assert.That(ConnectionChecker.Evaluate(board), Is.True);
                view.Build(board);
                List<TileState> path = new();
                Assert.That(ConnectionChecker.TryGetSolvedPath(board, path), Is.True);
                int completions = 0;
                Assert.That(view.PlayCompletionFeedback(path, () => completions++), Is.True);
                flow.Advance(flow.Duration);
                LineRenderer fill = root.GetComponent<LineRenderer>();
                TileView[] tileViews = root.GetComponentsInChildren<TileView>();
                TileView target = tileViews.Single(t => t.State.Role == TileRole.Target);
                Assert.That(fill.GetPosition(fill.positionCount - 1), Is.EqualTo(target.transform.position));
                Assert.That(completions, Is.Zero, "Target arrival must be visible before result UI can open.");
                HashSet<Vector2Int> route = new(path.Select(t => new Vector2Int(t.X, t.Y)));
                foreach (TileView tile in tileViews.Where(t => t.State.Shape != TileShape.Empty &&
                             !route.Contains(new Vector2Int(t.State.X, t.State.Y))))
                {
                    for (int vertex = 0; vertex < fill.positionCount; vertex++)
                        Assert.That(fill.GetPosition(vertex), Is.Not.EqualTo(tile.transform.position),
                            "Final success flow must not fill distractor pipes.");
                }
                flow.Advance(.13f);
                Assert.That(completions, Is.EqualTo(1));
                flow.Advance(2);
                Assert.That(completions, Is.EqualTo(1));
                view.Build(BoardBuilder.Build(level));
                Assert.That(fill.positionCount, Is.Zero, "Restart resets the previous filled route.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void EveryStoryArcIncreasesRealRouteAndMinimumRotationDifficulty(string name)
        {
            WorldDefinition world = World(name);
            LevelValidationResult[] levels = world.Levels.Select(LevelValidationUtility.Analyze).ToArray();
            for (int index = 1; index < levels.Length; index++)
            {
                Assert.That(levels[index].MinimumMoves, Is.GreaterThan(levels[index - 1].MinimumMoves));
                Assert.That(levels[index].ShortestPathLength, Is.GreaterThan(levels[index - 1].ShortestPathLength));
                Assert.That(levels[index].ActiveTiles, Is.GreaterThan(levels[index - 1].ActiveTiles));
            }
            foreach (int checkpoint in new[] { 3, 6, 9, 12 })
                Assert.That(world.GetStoryCheckpoint(checkpoint).Story, Is.Not.Null);
        }

        [Test]
        public void LaterWorldsResumeNearPriorFinalAndGrowBeyondIt()
        {
            WorldDefinition[] worlds = { World("SakuraGarden"), World("BambooWorkshop"), World("MoonShrine") };
            double previousMean = 0;
            for (int index = 0; index < worlds.Length; index++)
            {
                int[] costs = worlds[index].Levels.Select(l => LevelValidationUtility.Analyze(l).MinimumMoves).ToArray();
                Assert.That(costs.Average(), Is.GreaterThan(previousMean));
                previousMean = costs.Average();
                if (index == 0) continue;
                int previousFinal = LevelValidationUtility.Analyze(worlds[index - 1].Levels[11]).MinimumMoves;
                Assert.That(costs[0], Is.InRange(previousFinal * .8, previousFinal));
                Assert.That(costs[11], Is.GreaterThan(previousFinal));
            }
        }

        private static WorldDefinition World(string name) => AssetDatabase.LoadAssetAtPath<WorldDefinition>(
            $"Assets/Resources/Worlds/{name}.asset");
    }
}
