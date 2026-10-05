using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.Board;
using PipeMuzzle.Gameplay;
using PipeMuzzle.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class GameControllerWorldTests
    {
        private static readonly string[] Keys =
        {
            "PipeMuzzle.HighestUnlockedLevel",
            "PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1",
            "PipeMuzzle.Progress.World.SakuraGarden.HighestUnlockedLevel",
            "PipeMuzzle.Progress.World.BambooWorkshop.HighestUnlockedLevel",
            "PipeMuzzle.Progress.World.MoonShrine.HighestUnlockedLevel",
            "PipeMuzzle.Progress.World.SakuraGarden.Unlocked",
            "PipeMuzzle.Progress.World.BambooWorkshop.Unlocked",
            "PipeMuzzle.Progress.World.MoonShrine.Unlocked",
            "PipeMuzzle.Progress.World.SakuraGarden.Completed",
            "PipeMuzzle.Progress.World.BambooWorkshop.Completed",
            "PipeMuzzle.Progress.World.MoonShrine.Completed"
        };

        private readonly Dictionary<string, (bool exists, int value)> original = new();
        private readonly List<UnityEngine.Object> created = new();

        [SetUp]
        public void SetUp()
        {
            foreach (WorldId world in Enum.GetValues(typeof(WorldId)))
            for (int level = 1; level <= 12; level++)
            {
                string key = $"PipeMuzzle.BestMoves.World.{world}.Level.{level}";
                original[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
                PlayerPrefs.DeleteKey(key);
            }
            foreach (string key in Keys)
            {
                original[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) UnityEngine.Object.DestroyImmediate(created[i]);
            created.Clear();

            foreach (string key in original.Keys)
            {
                if (original[key].exists) PlayerPrefs.SetInt(key, original[key].value);
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            original.Clear();
        }

        [Test]
        public void SelectedWorldDeterminesLoadedDefinitionAndProgressNamespace()
        {
            GameController controller = CreateController();
            WorldDefinition sakura = Sakura();
            WorldDefinition bamboo = CreateCompleteWorld(WorldId.BambooWorkshop);
            WorldProgressService worlds = new();
            int loadCount = 0;
            controller.LevelLoaded += (_, _) => loadCount++;

            Assert.That(controller.LevelCount, Is.Zero);
            Assert.That(controller.ConfigureWorld(sakura), Is.True);
            Assert.That(controller.CurrentWorld, Is.SameAs(sakura));
            Assert.That(controller.LevelCount, Is.EqualTo(12));
            controller.LoadLevelByIndex(0);
            Assert.That(controller.CurrentLevelDefinition, Is.SameAs(sakura.Levels[0]));
            Assert.That(loadCount, Is.EqualTo(1));

            new ProgressService(WorldId.SakuraGarden, 12).UnlockLevel(1);
            worlds.MarkWorldCompleted(WorldId.SakuraGarden);
            Assert.That(controller.ConfigureWorld(bamboo), Is.True);
            Assert.That(controller.CurrentLevelNumber, Is.Zero);
            Assert.That(controller.IsLevelUnlocked(1), Is.False);
            controller.LoadLevelByIndex(0);
            Assert.That(controller.CurrentLevelDefinition, Is.SameAs(bamboo.Levels[0]));
            Assert.That(controller.CurrentLevelDefinition, Is.Not.SameAs(sakura.Levels[0]));
            Assert.That(loadCount, Is.EqualTo(2));
        }

        [Test]
        public void InvalidOrUnavailableWorldClearsPreviousGameplayContext()
        {
            GameController controller = CreateController();
            Assert.That(controller.ConfigureWorld(Sakura()), Is.True);
            controller.LoadLevelByIndex(0);

            LogAssert.Expect(LogType.Error,
                "GameController cannot configure an unavailable world.");
            Assert.That(controller.ConfigureWorld(null), Is.False);
            Assert.That(controller.CurrentWorld, Is.Null);
            Assert.That(controller.CurrentLevelDefinition, Is.Null);
            Assert.That(controller.CurrentLevelNumber, Is.Zero);
            Assert.That(controller.LevelCount, Is.Zero);

            WorldDefinition incomplete = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/BambooWorkshop.asset");
            LogAssert.Expect(LogType.Error,
                "GameController cannot configure an unavailable world.");
            Assert.That(controller.ConfigureWorld(incomplete), Is.False);

            WorldDefinition lockedCompleteMoon = CreateCompleteWorld(WorldId.MoonShrine);
            LogAssert.Expect(LogType.Error,
                "GameController cannot configure an unavailable world.");
            Assert.That(controller.ConfigureWorld(lockedCompleteMoon), Is.False);
        }

        [Test]
        public void OutOfRangeLevelCannotLoad()
        {
            GameController controller = CreateController();
            Assert.That(controller.ConfigureWorld(Sakura()), Is.True);
            int loaded = 0;
            controller.LevelLoaded += (_, _) => loaded++;

            controller.LoadLevelByIndex(-1);
            controller.LoadLevelByIndex(12);

            Assert.That(loaded, Is.Zero);
            Assert.That(controller.CurrentLevelDefinition, Is.Null);
        }

        [Test]
        public void OrdinaryCompletionUnlocksOnlyNextLevel()
        {
            GameController controller = CreateController();
            Assert.That(controller.ConfigureWorld(Sakura()), Is.True);
            controller.LoadLevelByIndex(0);

            CompleteLoadedLevel(controller);

            Assert.That(new ProgressService(WorldId.SakuraGarden, 12)
                .IsLevelUnlocked(1), Is.True);
            Assert.That(new WorldProgressService()
                .IsWorldUnlocked(WorldId.BambooWorkshop), Is.False);
        }

        [Test]
        public void FinalCompletionAdvancesWorldChainWithoutLevelThirteen()
        {
            GameController controller = CreateController();
            WorldProgressService worlds = new();
            bool? lastHasNext = null;
            int loaded = 0;
            controller.LevelCompleted += hasNext => lastHasNext = hasNext;
            controller.LevelLoaded += (_, _) => loaded++;

            CompleteFinalLevel(controller, Sakura(), WorldId.SakuraGarden);
            Assert.That(worlds.IsWorldUnlocked(WorldId.BambooWorkshop), Is.True);
            Assert.That(worlds.IsWorldUnlocked(WorldId.MoonShrine), Is.False);

            CompleteFinalLevel(controller,
                CreateCompleteWorld(WorldId.BambooWorkshop), WorldId.BambooWorkshop);
            Assert.That(worlds.IsWorldUnlocked(WorldId.MoonShrine), Is.True);

            WorldDefinition moon = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/MoonShrine.asset");
            CompleteFinalLevel(controller, moon, WorldId.MoonShrine);
            Assert.That(worlds.IsWorldCompleted(WorldId.MoonShrine), Is.True);
            Assert.That(controller.HasNextLevel, Is.False);
            Assert.That(lastHasNext, Is.False);
            Assert.That(controller.LevelCount, Is.EqualTo(12));
            Assert.That(controller.CurrentLevelDefinition, Is.SameAs(moon.Levels[11]));
            int loadedAfterFinale = loaded;
            controller.LoadLevelByIndex(12);
            Assert.That(loaded, Is.EqualTo(loadedAfterFinale));
            Assert.That(controller.CurrentLevelDefinition, Is.SameAs(moon.Levels[11]));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                worlds.IsWorldUnlocked((WorldId)3));
        }

        [Test]
        public void BestIsSavedAfterFlowAndRestartPreservesRecord()
        {
            GameController controller = CreateController();
            controller.ConfigureWorld(Sakura());
            controller.LoadLevelByIndex(0);
            BoardView view = GetField<BoardView>(controller, "boardView");
            PrepareSolvedBoard(controller, view);
            BoardState board = GetField<BoardState>(controller, "board");
            for (int i = 0; i < 22; i++) board.IncrementMoveCount();
            Invoke(controller, "BeginCompletion");
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, 1), Is.Null);
            EnergyFlowView flow = view.GetComponent<EnergyFlowView>();
            Advance(flow, 2f);
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, 1), Is.Null);
            Advance(flow, .13f);
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, 1), Is.EqualTo(22));
            controller.RestartLevel();
            Assert.That(controller.CurrentMoveCount, Is.Zero);
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, 1), Is.EqualTo(22));
        }

        [Test]
        public void ReplayedCompletionOnlyMarksNewBestForAnImprovement()
        {
            GameController controller = CreateController();
            controller.ConfigureWorld(Sakura());
            foreach (int moves in new[] { 20, 15, 24 })
            {
                controller.LoadLevelByIndex(0);
                BoardView view = GetField<BoardView>(controller, "boardView");
                PrepareSolvedBoard(controller, view);
                BoardState board = GetField<BoardState>(controller, "board");
                for (int i = 0; i < moves; i++) board.IncrementMoveCount();
                Invoke(controller, "BeginCompletion");
                Advance(view.GetComponent<EnergyFlowView>(), 2f);
                Advance(view.GetComponent<EnergyFlowView>(), .13f);
                Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, 1), Is.EqualTo(moves == 20 ? 20 : 15));
                Assert.That(controller.WasNewBest, Is.EqualTo(moves != 24));
            }
            controller.LoadNextLevel();
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, 1), Is.EqualTo(15));
            Assert.That(controller.WasNewBest, Is.False);
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, 2), Is.Null);
        }

        [Test]
        public void ControllerHintDoesNotCountMovesAndRejectsCompletedPendingAndEmptyContext()
        {
            GameController controller = CreateController();
            MethodInfo show = typeof(GameController).GetMethod("TryShowHint");
            Assert.That(show, Is.Not.Null, "Gameplay hint action is missing.");
            Assert.That(show.Invoke(controller, null), Is.EqualTo(false));
            controller.ConfigureWorld(Sakura());
            controller.LoadLevelByIndex(0);
            Assert.That(show.Invoke(controller, null), Is.EqualTo(true));
            Assert.That(show.Invoke(controller, null), Is.EqualTo(false));
            Assert.That(controller.CurrentMoveCount, Is.Zero);
            controller.RestartLevel();
            Assert.That(show.Invoke(controller, null), Is.EqualTo(true));
            BoardView view = GetField<BoardView>(controller, "boardView");
            PrepareSolvedBoard(controller, view);
            Invoke(controller, "BeginCompletion");
            Assert.That(show.Invoke(controller, null), Is.EqualTo(false));
            Advance(view.GetComponent<EnergyFlowView>(), 2f);
            Advance(view.GetComponent<EnergyFlowView>(), .13f);
            Assert.That(show.Invoke(controller, null), Is.EqualTo(false));
        }

        private static WorldDefinition Sakura() =>
            AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/SakuraGarden.asset");

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(6)]
        [TestCase(9)]
        [TestCase(12)]
        public void HintSolvesAndLocksWithoutMovesThenRunsNormalCompletionWithoutBest(int levelNumber)
        {
            GameController controller = CreateController();
            new ProgressService(WorldId.SakuraGarden, 12).UnlockLevel(levelNumber - 1);
            controller.ConfigureWorld(Sakura());
            controller.LoadLevelByIndex(levelNumber - 1);
            BoardState board = GetField<BoardState>(controller, "board");
            TileState selected = HintSelector.Select(board, controller.CurrentLevelDefinition);
            var step = controller.CurrentLevelDefinition.SolutionPath.First(s => s.Position == new Vector2Int(selected.X, selected.Y));
            // Leave exactly one wrong solution pipe for the hint to finish.
            foreach (LevelSolutionStep other in controller.CurrentLevelDefinition.SolutionPath)
            {
                TileState tile = board.GetTile(other.Position.x, other.Position.y);
                if (tile == selected || tile.IsLocked) continue;
                var solved = new TileState(tile.X, tile.Y, tile.Shape, tile.Role, other.Rotation, false);
                while (tile.Connections != solved.Connections) tile.RotateClockwise();
            }
            GetField<BoardView>(controller, "boardView").StopTransientEffects();
            for (int i = 0; i < 7; i++) board.IncrementMoveCount();
            BestMovesProgress.TrySetBest(WorldId.SakuraGarden, levelNumber, 18);
            int moveEvents = 0;
            controller.MoveCountChanged += _ => moveEvents++;
            Assert.That(controller.TryShowHint(), Is.True);
            Assert.That(selected.Rotation, Is.EqualTo(step.Rotation));
            Assert.That(selected.IsHintLocked, Is.True);
            Assert.That(board.TryRotateTile(selected.X, selected.Y), Is.False);
            TileView tileView = GetField<BoardView>(controller, "boardView").GetComponentsInChildren<TileView>().First(t => t.State == selected);
            typeof(GameController).GetMethod("HandleTileClicked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { tileView });
            Assert.That(controller.CurrentMoveCount, Is.EqualTo(7));
            Assert.That(moveEvents, Is.Zero);
            Assert.That(controller.IsCompletionPending, Is.True);
            Assert.That(GetField<BoardView>(controller, "boardView").IsHintPlaying, Is.True);
            Assert.That(controller.TryShowHint(), Is.False);
            EnergyFlowView flow = GetField<BoardView>(controller, "boardView").GetComponent<EnergyFlowView>();
            flow.Advance(2f);
            flow.Advance(.13f);
            Assert.That(controller.IsCompleted, Is.True);
            if (levelNumber < 12) Assert.That(controller.IsLevelUnlocked(levelNumber), Is.True);
            else Assert.That(new WorldProgressService().IsWorldCompleted(WorldId.SakuraGarden), Is.True);
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, levelNumber), Is.EqualTo(18));
            Assert.That(controller.WasNewBest, Is.False);
            if (levelNumber < 12) controller.LoadNextLevel();
            else controller.RestartLevel();
            Assert.That(controller.RemainingHints, Is.EqualTo(3));
            Assert.That(controller.UsedHintThisAttempt, Is.False);
            Assert.That(GetField<BoardState>(controller, "board").GetTile(selected.X, selected.Y), Is.Not.SameAs(selected));
            controller.LoadLevelByIndex(levelNumber - 1);
            Assert.That(GetField<BoardState>(controller, "board").GetTile(selected.X, selected.Y).IsHintLocked, Is.False);
            Assert.That(controller.RemainingHints, Is.EqualTo(3));
            Assert.That(controller.UsedHintThisAttempt, Is.False);
        }

        [Test]
        public void ThreeHintsSelectDifferentPipesEnforceCooldownAndRestartResetsAttempt()
        {
            GameController controller = CreateController();
            new ProgressService(WorldId.SakuraGarden, 12).UnlockLevel(11);
            controller.ConfigureWorld(Sakura());
            controller.LoadLevelByIndex(11);
            BoardState board = GetField<BoardState>(controller, "board");
            var selected = new HashSet<TileState>();
            var buttonObject = new GameObject("HintButton", typeof(RectTransform), typeof(UnityEngine.UI.Button));
            created.Add(buttonObject);
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            TMPro.TMP_Text label = labelObject.GetComponent<TMPro.TMP_Text>();
            var hintUI = buttonObject.AddComponent<PipeMuzzle.UI.GameplayHintUI>();
            hintUI.Configure(controller);
            Assert.That(controller.RemainingHints, Is.EqualTo(3));
            Assert.That(label.text, Is.EqualTo("HINT 3/3"));
            for (int i = 0; i < 3; i++)
            {
                TileState tile = HintSelector.Select(board, controller.CurrentLevelDefinition);
                Assert.That(tile, Is.Not.Null);
                Assert.That(selected.Add(tile), Is.True);
                Assert.That(controller.TryShowHint(), Is.True);
                Assert.That(controller.RemainingHints, Is.EqualTo(2 - i));
                hintUI.Configure(controller);
                Assert.That(label.text, Is.EqualTo($"HINT {2 - i}/3"));
                Assert.That(buttonObject.GetComponent<UnityEngine.UI.Button>().interactable, Is.False);
                Assert.That(controller.TryShowHint(), Is.False);
                controller.CancelHint();
                Assert.That(controller.CanHint, Is.False, "Cancelling feedback must not bypass cooldown.");
                typeof(GameController).GetField("hintAvailableAt", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(controller, Time.unscaledTime - .1f);
            }
            Assert.That(controller.TryShowHint(), Is.False);
            controller.RestartLevel();
            Assert.That(controller.RemainingHints, Is.EqualTo(3));
            Assert.That(controller.UsedHintThisAttempt, Is.False);
            board = GetField<BoardState>(controller, "board");
            foreach (TileDefinition definition in controller.CurrentLevelDefinition.Tiles)
            {
                TileState tile = board.GetTile(definition.X, definition.Y);
                Assert.That(tile.Rotation, Is.EqualTo(definition.StartRotation));
                Assert.That(tile.IsHintLocked, Is.False);
            }
            hintUI.Configure(controller);
            Assert.That(label.text, Is.EqualTo("HINT 3/3"));
            Assert.That(buttonObject.GetComponent<UnityEngine.UI.Button>().interactable, Is.True);
            Assert.That(controller.TryShowHint(), Is.True);
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(6)]
        [TestCase(9)]
        [TestCase(12)]
        public void SolvedBoardWaitsForFlowBeforeCompletionAndProgress(int levelNumber)
        {
            GameController controller = CreateController();
            new ProgressService(WorldId.SakuraGarden, 12).UnlockLevel(levelNumber - 1);
            controller.ConfigureWorld(Sakura());
            controller.LoadLevelByIndex(levelNumber - 1);
            BoardView view = GetField<BoardView>(controller, "boardView");
            EnergyFlowView flow = view.GetComponent<EnergyFlowView>();
            Assert.That(flow.IsPlaying, Is.False, "Unsolved boards must remain empty.");
            int completed = 0;
            controller.LevelCompleted += _ => completed++;
            PrepareSolvedBoard(controller, view);
            Invoke(controller, "BeginCompletion");

            Assert.That(flow.IsPlaying, Is.True);
            Assert.That(controller.IsCompleted, Is.False);
            Assert.That(completed, Is.Zero);
            if (levelNumber < 12) Assert.That(controller.IsLevelUnlocked(levelNumber), Is.False);
            Assert.That(new WorldProgressService().IsWorldCompleted(WorldId.SakuraGarden), Is.False);
            Advance(flow, .1f);
            Assert.That(completed, Is.Zero);
            Advance(flow, 2f);
            Assert.That(completed, Is.Zero, "Even a long frame must leave target arrival visible.");
            Advance(flow, .13f);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(controller.IsCompleted, Is.True);
            if (levelNumber < 12) Assert.That(controller.IsLevelUnlocked(levelNumber), Is.True);
            else Assert.That(new WorldProgressService().IsWorldCompleted(WorldId.SakuraGarden), Is.True);
            Advance(flow, 2f);
            Assert.That(completed, Is.EqualTo(1), "Completion must be emitted exactly once.");
            if (levelNumber < 12)
            {
                controller.LoadNextLevel();
                Assert.That(flow.IsPlaying, Is.False);
                Assert.That(view.GetComponent<LineRenderer>().positionCount, Is.Zero,
                    "The next board must not inherit filled channels.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LeavingOrRestartingCancelsPendingFlow(bool restart)
        {
            GameController controller = CreateController();
            controller.ConfigureWorld(Sakura());
            controller.LoadLevelByIndex(0);
            BoardView view = GetField<BoardView>(controller, "boardView");
            EnergyFlowView flow = view.GetComponent<EnergyFlowView>();
            PrepareSolvedBoard(controller, view);
            int completed = 0;
            controller.LevelCompleted += _ => completed++;
            Invoke(controller, "BeginCompletion");
            Advance(flow, .1f);
            if (restart) controller.RestartLevel();
            else controller.CancelTransientVisuals();
            Advance(flow, 2f);
            Assert.That(flow.IsPlaying, Is.False);
            Assert.That(completed, Is.Zero);
            Assert.That(controller.IsCompleted, Is.False);
            Assert.That(controller.CurrentMoveCount, Is.Zero);
            Assert.That(controller.IsLevelUnlocked(1), Is.False);
        }

        private static void PrepareSolvedBoard(GameController controller, BoardView view)
        {
            // A small fixture changes runtime state only; authored level data stays untouched.
            BoardState board = new(3, 1);
            board.SetTile(new TileState(0, 0, TileShape.Straight, TileRole.Source, 1, true));
            board.SetTile(new TileState(1, 0, TileShape.Straight, TileRole.Normal, 1, false));
            board.SetTile(new TileState(2, 0, TileShape.Straight, TileRole.Target, 1, true));
            Assert.That(ConnectionChecker.Evaluate(board), Is.True);
            typeof(GameController).GetField("board", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, board);
            view.Build(board);
        }

        private static T GetField<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void Invoke(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name + " is required for the delayed completion path.");
            method.Invoke(target, null);
        }

        private static void Advance(EnergyFlowView flow, float delta)
        {
            MethodInfo method = typeof(EnergyFlowView).GetMethod("Advance");
            Assert.That(method, Is.Not.Null, "Flow needs a deterministic animation clock.");
            method.Invoke(flow, new object[] { delta });
        }

        private WorldDefinition CreateCompleteWorld(WorldId id)
        {
            WorldDefinition world = ScriptableObject.CreateInstance<WorldDefinition>();
            created.Add(world);
            SerializedObject serialized = new(world);
            serialized.FindProperty("worldId").enumValueIndex = (int)id;
            SerializedProperty list = serialized.FindProperty("levels");
            list.arraySize = 12;
            LevelDefinition[] levels = Sakura().Levels.Reverse().ToArray();
            for (int i = 0; i < levels.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return world;
        }

        private GameController CreateController()
        {
            GameObject boardObject = new("TestBoardView");
            created.Add(boardObject);
            BoardView boardView = boardObject.AddComponent<BoardView>();
            GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/TilePrefab.prefab");
            Assert.That(tilePrefab, Is.Not.Null);
            SerializedObject boardSerialized = new(boardView);
            boardSerialized.FindProperty("tilePrefab").objectReferenceValue =
                tilePrefab.GetComponent<TileView>();
            boardSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject cameraObject = new("TestBoardCamera");
            created.Add(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            BoardCameraFitter fitter = cameraObject.AddComponent<BoardCameraFitter>();
            SerializedObject fitterSerialized = new(fitter);
            fitterSerialized.FindProperty("targetCamera").objectReferenceValue = camera;
            fitterSerialized.FindProperty("boardView").objectReferenceValue = boardView;
            fitterSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject controllerObject = new("TestGameController");
            created.Add(controllerObject);
            GameController controller = controllerObject.AddComponent<GameController>();
            SerializedObject controllerSerialized = new(controller);
            controllerSerialized.FindProperty("boardView").objectReferenceValue = boardView;
            controllerSerialized.FindProperty("boardCameraFitter").objectReferenceValue = fitter;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static void CompleteLoadedLevel(GameController controller)
        {
            MethodInfo complete = typeof(GameController).GetMethod("CompleteLevel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(complete, Is.Not.Null);
            complete.Invoke(controller, null);
        }

        private static void CompleteFinalLevel(
            GameController controller, WorldDefinition world, WorldId id)
        {
            new ProgressService(id, 12).UnlockLevel(11);
            Assert.That(controller.ConfigureWorld(world), Is.True);
            controller.LoadLevelByIndex(11);
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(12));
            CompleteLoadedLevel(controller);
            Assert.That(new WorldProgressService().IsWorldCompleted(id), Is.True);
        }
    }
}
