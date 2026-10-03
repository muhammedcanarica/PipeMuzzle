using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.Editor;
using PipeMuzzle.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class HintAndBestMovesTests
    {
        private readonly Dictionary<string, (bool exists, int value)> saved = new();
        private Type Records => typeof(GameController).Assembly.GetType("PipeMuzzle.Gameplay.BestMovesProgress");

        [SetUp]
        public void SetUp()
        {
            foreach (WorldId world in Enum.GetValues(typeof(WorldId)))
            for (int level = 1; level <= 12; level++) Save(Key(world, level));
            Save("PipeMuzzle.Feedback.SoundEnabled");
        }

        private void Save(string key)
        {
            saved[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
            PlayerPrefs.DeleteKey(key);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in saved)
                if (item.Value.exists) PlayerPrefs.SetInt(item.Key, item.Value.value);
                else PlayerPrefs.DeleteKey(item.Key);
            saved.Clear();
            PlayerPrefs.Save();
        }

        private static string Key(WorldId world, int level) => $"PipeMuzzle.BestMoves.World.{world}.Level.{level}";
        private object Call(string name, params object[] args)
        {
            Assert.That(Records, Is.Not.Null, "Best move persistence is missing.");
            return Records.GetMethod(name).Invoke(null, args);
        }

        [Test]
        public void RecordOnlyImprovesAcrossReplayAndRejectsInvalidMoves()
        {
            Assert.That(Call("GetBest", WorldId.SakuraGarden, 1), Is.Null);
            Assert.That(Call("TrySetBest", WorldId.SakuraGarden, 1, 22), Is.EqualTo(true));
            Assert.That(Call("TrySetBest", WorldId.SakuraGarden, 1, 27), Is.EqualTo(false));
            Assert.That(Call("GetBest", WorldId.SakuraGarden, 1), Is.EqualTo(22));
            Assert.That(Call("TrySetBest", WorldId.SakuraGarden, 1, 18), Is.EqualTo(true));
            Assert.That(Call("TrySetBest", WorldId.SakuraGarden, 1, 18), Is.EqualTo(false));
            Assert.That(Call("TrySetBest", WorldId.SakuraGarden, 1, 0), Is.EqualTo(false));
            Assert.That(Call("TrySetBest", WorldId.SakuraGarden, 1, -1), Is.EqualTo(false));
            Assert.That(Call("GetBest", WorldId.SakuraGarden, 1), Is.EqualTo(18));
        }

        [Test]
        public void RecordsAreIsolatedByLevelAndWorldAndResetKeepsSound()
        {
            Call("TrySetBest", WorldId.SakuraGarden, 1, 22);
            Call("TrySetBest", WorldId.SakuraGarden, 2, 18);
            Call("TrySetBest", WorldId.BambooWorkshop, 1, 30);
            Call("TrySetBest", WorldId.MoonShrine, 12, 42);
            Assert.That(Call("GetBest", WorldId.SakuraGarden, 1), Is.EqualTo(22));
            Assert.That(Call("GetBest", WorldId.SakuraGarden, 2), Is.EqualTo(18));
            Assert.That(Call("GetBest", WorldId.BambooWorkshop, 1), Is.EqualTo(30));
            Assert.That(Call("GetBest", WorldId.MoonShrine, 12), Is.EqualTo(42));
            PlayerPrefs.SetInt("PipeMuzzle.Feedback.SoundEnabled", 0);
            Call("ResetAll");
            Assert.That(Call("GetBest", WorldId.SakuraGarden, 1), Is.Null);
            Assert.That(Call("GetBest", WorldId.SakuraGarden, 2), Is.Null);
            Assert.That(Call("GetBest", WorldId.BambooWorkshop, 1), Is.Null);
            Assert.That(Call("GetBest", WorldId.MoonShrine, 12), Is.Null);
            Assert.That(PlayerPrefs.GetInt("PipeMuzzle.Feedback.SoundEnabled", 1), Is.Zero);
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void HintUsesFirstWrongSolutionTileWithoutRotatingOrCountingMoves(string worldName)
        {
            Type selector = typeof(GameController).Assembly.GetType("PipeMuzzle.Gameplay.HintSelector");
            Assert.That(selector, Is.Not.Null, "Solution hint selection is missing.");
            MethodInfo select = selector.GetMethod("Select");
            WorldDefinition world = Resources.Load<WorldDefinition>("Worlds/" + worldName);
            foreach (LevelDefinition level in world.Levels)
            {
                BoardState board = BoardBuilder.Build(level);
                var solution = LevelValidationUtility.Analyze(level);
                int hints = 0;
                foreach (Vector2Int pos in solution.SolutionPath)
                {
                    TileState tile = board.GetTile(pos.x, pos.y);
                    if (tile.Role != TileRole.Normal || tile.IsLocked || tile.Shape == TileShape.Empty) continue;
                    TileState solved = new(tile.X, tile.Y, tile.Shape, tile.Role, solution.SolutionRotations[pos], false);
                    if (tile.Connections == solved.Connections) continue;
                    int rotation = tile.Rotation;
                    Assert.That(select.Invoke(null, new object[] { board, level }), Is.SameAs(tile), level.name);
                    Assert.That(tile.Rotation, Is.EqualTo(rotation));
                    Assert.That(board.MoveCount, Is.Zero);
                    while (tile.Connections != solved.Connections) tile.RotateClockwise();
                    hints++;
                }
                Assert.That(hints, Is.GreaterThan(0), level.name);
                Assert.That(select.Invoke(null, new object[] { board, level }), Is.Null, "Aligned path must not highlight distractors.");
                Assert.That(ConnectionChecker.Evaluate(board), Is.True, level.name);
            }
        }

        [TestCase(TileRole.Source, TileShape.Straight, false)]
        [TestCase(TileRole.Target, TileShape.Straight, false)]
        [TestCase(TileRole.Normal, TileShape.Straight, true)]
        [TestCase(TileRole.Normal, TileShape.Empty, false)]
        public void HintNeverSelectsRolesLockedOrEmptyTiles(TileRole role, TileShape shape, bool locked)
        {
            LevelDefinition level = Resources.Load<WorldDefinition>("Worlds/SakuraGarden").Levels[0];
            BoardState board = BoardBuilder.Build(level);
            board.SetTile(new TileState(1, 1, shape, role, 1, locked));
            Assert.That(HintSelector.Select(board, level), Is.Null);
        }

        [Test]
        public void EquivalentStraightOrientationDoesNotRequestAHint()
        {
            LevelDefinition level = Resources.Load<WorldDefinition>("Worlds/SakuraGarden").Levels[0];
            BoardState board = BoardBuilder.Build(level);
            board.SetTile(new TileState(1, 1, TileShape.Straight, TileRole.Normal, 2, false));
            Assert.That(HintSelector.Select(board, level), Is.Null);
        }

        [TestCase(0)]
        [TestCase(13)]
        [TestCase(-1)]
        public void InvalidLevelNumberDoesNotCreateARecord(int level)
        {
            Assert.That(BestMovesProgress.TrySetBest(WorldId.SakuraGarden, level, 10), Is.False);
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, level), Is.Null);
        }

        [Test]
        public void InvalidWorldAndCorruptRecordAreIgnored()
        {
            Assert.That(BestMovesProgress.TrySetBest((WorldId)99, 1, 10), Is.False);
            Assert.That(BestMovesProgress.GetBest((WorldId)99, 1), Is.Null);
            PlayerPrefs.SetInt(Key(WorldId.SakuraGarden, 1), -5);
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, 1), Is.Null);
            Assert.That(BestMovesProgress.TrySetBest(WorldId.SakuraGarden, 1, 22), Is.True);
        }

        [Test]
        public void HintFeedbackSuppressesDuplicatesAndCleansUpWithoutChangingTile()
        {
            Type type = typeof(GameController).Assembly.GetType("PipeMuzzle.View.PipeHintFeedback");
            Assert.That(type, Is.Not.Null, "Hint feedback is missing.");
            var root = new GameObject("HintFeedbackTest");
            var tileObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TilePrefab.prefab"));
            try
            {
                var tile = tileObject.GetComponent<PipeMuzzle.View.TileView>();
                var state = new TileState(0, 0, TileShape.Corner, TileRole.Normal, 0, false);
                tile.Initialize(state);
                Component feedback = root.AddComponent(type);
                MethodInfo show = type.GetMethod("TryShow");
                Assert.That(show.Invoke(feedback, new object[] { tile, 1f, Color.magenta }), Is.EqualTo(true));
                Assert.That(show.Invoke(feedback, new object[] { tile, 1f, Color.magenta }), Is.EqualTo(false));
                type.GetMethod("Advance").Invoke(feedback, new object[] { .6f });
                Assert.That(state.Rotation, Is.Zero);
                Assert.That(tile.transform.localScale, Is.EqualTo(Vector3.one));
                type.GetMethod("Advance").Invoke(feedback, new object[] { .7f });
                Assert.That(type.GetProperty("IsPlaying").GetValue(feedback), Is.EqualTo(false), "Animation must end at 1.2 seconds.");
                Assert.That(root.GetComponentInChildren<LineRenderer>().enabled, Is.False, "Completed overlay must be hidden.");
                Assert.That(show.Invoke(feedback, new object[] { tile, 1f, Color.magenta }), Is.EqualTo(true));
                root.SetActive(false);
                // EditMode does not dispatch runtime lifecycle messages for these components.
                type.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(feedback, null);
                Assert.That(type.GetProperty("IsPlaying").GetValue(feedback), Is.EqualTo(false), "Disable must cancel the animation.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(tileObject); }
        }
    }
}
