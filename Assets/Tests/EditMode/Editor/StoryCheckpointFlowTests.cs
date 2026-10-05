using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using PipeMuzzle.Board;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using PipeMuzzle.UI;
using PipeMuzzle.View;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class StoryCheckpointFlowTests
    {
        private static readonly string[] Worlds = { "SakuraGarden", "BambooWorkshop", "MoonShrine" };
        private static readonly int[] Checkpoints = { 3, 6, 9, 12 };
        private readonly List<Object> created = new();
        private readonly Dictionary<string, (bool exists, int value)> originalPrefs = new();
        private GameController controller;
        private GameUI gameUi;
        private ScreenManager screens;
        private LevelSelectUI levelSelect;
        private ComicViewerUI viewer;
        private GameObject comic, select, gameplay, completion;
        private TMP_Text completionText;
        private Image panelImage;
        private Button next;

        private static IEnumerable<TestCaseData> CompletionCases()
        {
            foreach (string world in Worlds)
            foreach (int checkpoint in Checkpoints)
            foreach (bool skip in new[] { false, true })
                yield return new TestCaseData(world, checkpoint, skip);
        }

        private static IEnumerable<TestCaseData> ProgressCases()
        {
            foreach (string world in Worlds)
            foreach (int checkpoint in Checkpoints)
                yield return new TestCaseData(world, checkpoint);
        }

        [SetUp]
        public void SetUp()
        {
            SaveAndClear("PipeMuzzle.Feedback.SoundEnabled");
            SaveAndClear(BasicRotationTutorialProgress.SeenKey);
            SaveAndClear("PipeMuzzle.HighestUnlockedLevel");
            SaveAndClear("PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1");
            foreach (string world in Worlds)
            {
                for (int level = 1; level <= 12; level++)
                    SaveAndClear($"PipeMuzzle.BestMoves.World.{world}.Level.{level}");
                foreach (string suffix in new[] { "HighestUnlockedLevel", "Unlocked", "Completed" })
                    SaveAndClear(ProgressKey(world, suffix));
                foreach (int checkpoint in new[] { 0, 3, 6, 9, 12 })
                    SaveAndClear($"PipeMuzzle.Story.World.{world}.Checkpoint.{checkpoint}.Viewed");
            }
            PlayerPrefs.Save();
            CreateHarness();
        }

        [TearDown]
        public void TearDown()
        {
            // The inactive harness binds explicitly, so unbind explicitly as well.
            if (gameUi != null) Invoke(gameUi, "OnDisable");
            for (int index = created.Count - 1; index >= 0; index--)
                if (created[index] != null) Object.DestroyImmediate(created[index]);
            created.Clear();
            foreach (var entry in originalPrefs)
            {
                if (entry.Value.exists) PlayerPrefs.SetInt(entry.Key, entry.Value.value);
                else PlayerPrefs.DeleteKey(entry.Key);
            }
            PlayerPrefs.Save();
            originalPrefs.Clear();
        }

        [TestCaseSource(nameof(CompletionCases))]
        public void FirstCompletionShowsAssignedComicAndReturnsToExpectedScreen(string worldName, int checkpoint, bool skip)
        {
            WorldDefinition world = PrepareLevel(worldName, checkpoint, false);
            Assert.That(completion.activeSelf, Is.False);
            Invoke(controller, "CompleteLevel");

            Assert.That(controller.IsCompleted, Is.True);
            Assert.That(comic.activeSelf, Is.True);
            Assert.That(gameplay.activeSelf, Is.False);
            Assert.That(completion.activeSelf, Is.False, "The overlay must wait for the comic.");
            Assert.That(GetField<ComicStoryDefinition>(viewer, "currentStory"),
                Is.SameAs(world.GetStoryCheckpoint(checkpoint).Story));
            Assert.That(panelImage.sprite, Is.SameAs(world.GetStoryCheckpoint(checkpoint).Story.Panels[0]));
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.False);

            FinishComic(skip);

            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.True);
            Assert.That(comic.activeSelf, Is.False);
            Assert.That(controller.CurrentWorld, Is.SameAs(world));
            if (checkpoint < world.LevelCount)
            {
                Assert.That(select.activeSelf, Is.True);
                Assert.That(gameplay.activeSelf, Is.False);
                Assert.That(levelSelect.CurrentWorld, Is.SameAs(world));
                Assert.That(controller.IsLevelUnlocked(checkpoint), Is.True);
            }
            else if (worldName == "MoonShrine") AssertJourney();
            else
            {
                Assert.That(gameplay.activeSelf, Is.True);
                Assert.That(select.activeSelf, Is.False);
                Assert.That(completion.activeSelf, Is.True);
                Assert.That(completionText.text, Does.Contain("WORLD COMPLETE"));
                Assert.That(completionText.text, Does.Contain(world.DisplayName));
                Assert.That(new WorldProgressService().IsWorldCompleted(world.WorldId), Is.True);
                Assert.That(next.gameObject.activeSelf, Is.EqualTo(worldName != "MoonShrine"));
                if (worldName == "SakuraGarden")
                    Assert.That(new WorldProgressService().IsWorldUnlocked(WorldId.BambooWorkshop), Is.True);
                if (worldName == "BambooWorkshop")
                    Assert.That(new WorldProgressService().IsWorldUnlocked(WorldId.MoonShrine), Is.True);
                Assert.That(completion.transform.Find("CompletionMapButton").gameObject.activeSelf, Is.True);
            }
        }

        [TestCaseSource(nameof(ProgressCases))]
        public void CheckpointBackReturnsToLevelSelectOrFinalCompletion(string worldName, int checkpoint)
        {
            WorldDefinition world = PrepareLevel(worldName, checkpoint, false);
            Invoke(controller, "CompleteLevel");
            Assert.That(comic.activeSelf, Is.True);
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.False);
            viewer.Back();

            Assert.That(comic.activeSelf, Is.False);
            Assert.That(controller.CurrentWorld, Is.SameAs(world));
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.True);
            if (checkpoint < world.LevelCount)
            {
                Assert.That(select.activeSelf, Is.True);
                Assert.That(gameplay.activeSelf, Is.False);
                Assert.That(levelSelect.CurrentWorld, Is.SameAs(world));
                Assert.That(controller.IsLevelUnlocked(checkpoint), Is.True);
            }
            else if (worldName == "MoonShrine") AssertJourney();
            else
            {
                Assert.That(select.activeSelf, Is.False);
                Assert.That(gameplay.activeSelf, Is.True);
                Assert.That(completion.activeSelf, Is.True);
                Assert.That(completionText.text, Does.Contain("WORLD COMPLETE"));
                Assert.That(completionText.text, Does.Contain(world.DisplayName));
                Assert.That(next.gameObject.activeSelf, Is.EqualTo(worldName != "MoonShrine"));
            }
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void WorldMapEntry_AlwaysPlaysIntro(string worldName)
        {
            WorldDefinition world = LoadWorld(worldName);
            string introKey = $"PipeMuzzle.Story.World.{worldName}.Checkpoint.0.Viewed";
            PlayerPrefs.SetInt(ProgressKey(worldName, "Unlocked"), 1);
            StoryNavigationCoordinator navigation = screens.GetComponent<StoryNavigationCoordinator>();
            navigation.OpenWorld(world);

            Assert.That(comic.activeSelf, Is.True);
            Assert.That(select.activeSelf, Is.False);
            Assert.That(GetField<ComicStoryDefinition>(viewer, "currentStory"),
                Is.SameAs(world.GetStoryCheckpoint(0).Story));
            Assert.That(panelImage.sprite, Is.SameAs(world.GetStoryCheckpoint(0).Story.Panels[0]));
            Assert.That(PlayerPrefs.HasKey(introKey), Is.False);

            viewer.Skip();

            Assert.That(comic.activeSelf, Is.False);
            Assert.That(select.activeSelf, Is.True);
            Assert.That(gameplay.activeSelf, Is.False);
            Assert.That(levelSelect.CurrentWorld, Is.SameAs(world));
            Assert.That(controller.CurrentWorld, Is.SameAs(world));

            screens.ShowWorldMap();
            PlayerPrefs.SetInt(introKey, 1); // Existing saves may retain the old Intro flag.
            navigation.OpenWorld(world);

            Assert.That(comic.activeSelf, Is.True);
            Assert.That(select.activeSelf, Is.False);
            Assert.That(GetField<ComicStoryDefinition>(viewer, "currentStory"),
                Is.SameAs(world.GetStoryCheckpoint(0).Story));
            Assert.That(PlayerPrefs.GetInt(introKey), Is.EqualTo(1));
            viewer.Skip();
            Assert.That(select.activeSelf, Is.True);
            Assert.That(levelSelect.CurrentWorld, Is.SameAs(world));
            Assert.That(controller.CurrentWorld, Is.SameAs(world));
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void Intro_DoesNotCreateViewedPersistence(string worldName)
        {
            WorldDefinition world = LoadWorld(worldName);
            string introKey = $"PipeMuzzle.Story.World.{worldName}.Checkpoint.0.Viewed";
            PlayerPrefs.SetInt(ProgressKey(worldName, "Unlocked"), 1);
            StoryNavigationCoordinator navigation = screens.GetComponent<StoryNavigationCoordinator>();
            Assert.That(PlayerPrefs.HasKey(introKey), Is.False);

            navigation.OpenWorld(world);
            viewer.Skip();
            screens.ShowWorldMap();
            navigation.OpenWorld(world);

            Assert.That(comic.activeSelf, Is.True);
            Assert.That(PlayerPrefs.HasKey(introKey), Is.False);
        }

        [TestCaseSource(nameof(ProgressCases))]
        public void ReplayAfterCheckpointDoesNotShowComic(string worldName, int checkpoint)
        {
            WorldDefinition world = PrepareLevel(worldName, checkpoint, false);
            Invoke(controller, "CompleteLevel");
            Assert.That(comic.activeSelf, Is.True);
            viewer.Skip();

            Assert.That(controller.ConfigureWorld(world), Is.True);
            screens.ShowGameplay();
            controller.LoadLevelByIndex(checkpoint - 1);
            Invoke(controller, "CompleteLevel");

            Assert.That(comic.activeSelf, Is.False);
            if (worldName == "MoonShrine" && checkpoint == 12) AssertJourney();
            else
            {
                Assert.That(gameplay.activeSelf, Is.True);
                Assert.That(completion.activeSelf, Is.True);
            }
        }

        [TestCaseSource(nameof(ProgressCases))]
        public void UnviewedCheckpointPlaysEvenIfLevelWasPreviouslyUnlocked(string worldName, int checkpoint)
        {
            WorldDefinition world = PrepareLevel(worldName, checkpoint, true);
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.False);
            Invoke(controller, "CompleteLevel");

            Assert.That(comic.activeSelf, Is.True);
            Assert.That(completion.activeSelf, Is.False);
            Assert.That(GetField<ComicStoryDefinition>(viewer, "currentStory"),
                Is.SameAs(world.GetStoryCheckpoint(checkpoint).Story));
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.False);
            viewer.Skip();
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.True);
            if (checkpoint < world.LevelCount)
            {
                Assert.That(select.activeSelf, Is.True);
                Assert.That(levelSelect.CurrentWorld, Is.SameAs(world));
            }
            else if (worldName == "MoonShrine") AssertJourney();
            else
            {
                Assert.That(completion.activeSelf, Is.True);
                Assert.That(completionText.text, Does.Contain("WORLD COMPLETE"));
            }
        }

        [TestCase(3)]
        [TestCase(6)]
        [TestCase(9)]
        public void MigratedLegacySakuraProgressDoesNotSuppressUnviewedCheckpoint(int checkpoint)
        {
            PlayerPrefs.SetInt("PipeMuzzle.HighestUnlockedLevel", checkpoint);
            WorldDefinition world = LoadWorld("SakuraGarden");
            Assert.That(controller.ConfigureWorld(world), Is.True);
            screens.ShowGameplay();
            controller.LoadLevelByIndex(checkpoint - 1);
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(checkpoint));
            Invoke(controller, "CompleteLevel");

            Assert.That(comic.activeSelf, Is.True);
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.False);
            viewer.Skip();
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.True);
        }

        [TestCaseSource(nameof(ProgressCases))]
        public void LevelEntryDoesNotPlayAndResetRearmsCompletedCheckpoint(string worldName, int checkpoint)
        {
            WorldDefinition world = PrepareLevel(worldName, checkpoint, false);
            Assert.That(comic.activeSelf, Is.False);
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.False);
            Invoke(controller, "CompleteLevel");
            viewer.Skip();
            StoryCheckpointProgress.ResetViewedCheckpointsForWorld(world.WorldId);
            PrepareLevel(worldName, checkpoint, true);
            Invoke(controller, "CompleteLevel");
            Assert.That(comic.activeSelf, Is.True);
            Assert.That(GetField<ComicStoryDefinition>(viewer, "currentStory"), Is.SameAs(world.GetStoryCheckpoint(checkpoint).Story));
            viewer.Skip();
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.True);
        }

        [TestCaseSource(nameof(ProgressCases))]
        public void InterruptedCheckpointRemainsUnviewedAndCanReplay(string worldName, int checkpoint)
        {
            WorldDefinition world = PrepareLevel(worldName, checkpoint, false);
            Invoke(controller, "CompleteLevel");
            screens.ShowWorldMap();
            viewer.Skip(); // A late terminal event from a hidden comic must not consume it.
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.False);
            PrepareLevel(worldName, checkpoint, true);
            Invoke(controller, "CompleteLevel");
            Assert.That(comic.activeSelf, Is.True);
            viewer.Back();
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, checkpoint), Is.True);
        }

        [Test]
        public void MissingPresentationDoesNotConsumeCheckpointAndUsesNormalCompletion()
        {
            WorldDefinition world = PrepareLevel("SakuraGarden", 3, false);
            SetField(viewer, "panelImage", null);
            Invoke(controller, "CompleteLevel");
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, 3), Is.False);
            Assert.That(comic.activeSelf, Is.False);
            Assert.That(completion.activeSelf, Is.True);
        }

        [Test]
        public void OpeningIntroAbandonsCheckpointWithoutConsumingIt()
        {
            WorldDefinition world = PrepareLevel("SakuraGarden", 3, false);
            Invoke(controller, "CompleteLevel");
            screens.GetComponent<StoryNavigationCoordinator>().OpenWorld(world);
            viewer.Skip();
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, 3), Is.False);
            Assert.That(select.activeSelf, Is.True);
        }

        [Test]
        public void MoonFinalUsesJourneyContextAndCreditsReturnWithoutLosingProgress()
        {
            PrepareLevel("MoonShrine", 12, false);
            Invoke(controller, "CompleteLevel");
            Assert.That(comic.activeSelf, Is.True);
            viewer.Skip();
            Transform journey = gameplay.transform.parent.Find("JourneyComplete");
            Assert.That(journey, Is.Not.Null, "Moon finale needs its own navigation context.");
            Assert.That(journey.gameObject.activeSelf, Is.True);
            Assert.That(gameplay.activeSelf, Is.False);
            Assert.That(completion.activeSelf, Is.False);
            journey.Find("SafeArea/Surface/Journey/CreditsButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(journey.Find("SafeArea/Surface/Credits").gameObject.activeSelf, Is.True);
            journey.Find("SafeArea/Surface/Credits/BackButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(journey.Find("SafeArea/Surface/Journey").gameObject.activeSelf, Is.True);
            journey.Find("SafeArea/Surface/Journey/WorldMapButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(journey.gameObject.activeSelf, Is.False);
            Assert.That(new WorldProgressService().IsWorldCompleted(WorldId.MoonShrine), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MoonFinalWaitsForFlowAndComicAndRespectsAssistedBest(bool assisted)
        {
            PrepareLevel("MoonShrine", 12, false);
            BoardState board = GetField<BoardState>(controller, "board");
            TileState selected = HintSelector.Select(board, controller.CurrentLevelDefinition);
            foreach (LevelSolutionStep step in controller.CurrentLevelDefinition.SolutionPath)
            {
                TileState tile = board.GetTile(step.Position.x, step.Position.y);
                if (tile.IsLocked || assisted && tile == selected) continue;
                var solved = new TileState(tile.X, tile.Y, tile.Shape, tile.Role, step.Rotation, false);
                while (tile.Connections != solved.Connections) tile.RotateClockwise();
            }
            BoardView view = GetField<BoardView>(controller, "boardView");
            view.StopTransientEffects();
            for (int i = 0; i < 7; i++) board.IncrementMoveCount();
            BestMovesProgress.TrySetBest(WorldId.MoonShrine, 12, 18);
            if (assisted) Assert.That(controller.TryShowHint(), Is.True);
            else
            {
                Assert.That(ConnectionChecker.Evaluate(board), Is.True);
                Invoke(controller, "BeginCompletion");
            }
            Assert.That(controller.IsCompletionPending, Is.True);
            Assert.That(comic.activeSelf, Is.False);
            Assert.That(screens.IsJourneyCompleteOpen, Is.False);
            EnergyFlowView flow = view.GetComponent<EnergyFlowView>();
            flow.Advance(2f);
            Assert.That(comic.activeSelf, Is.False, "Arrival hold must finish before the comic.");
            flow.Advance(.13f);
            Assert.That(comic.activeSelf, Is.True);
            Assert.That(screens.IsJourneyCompleteOpen, Is.False);
            viewer.Skip();
            AssertJourney();
            Assert.That(BestMovesProgress.GetBest(WorldId.MoonShrine, 12), Is.EqualTo(assisted ? 18 : 7));
            Assert.That(controller.WasNewBest, Is.EqualTo(!assisted));
            Assert.That(controller.TryPause(), Is.False);
        }

        [Test]
        public void ResetClearsDerivedJourneyCompletionAndPreservesSound()
        {
            PrepareLevel("MoonShrine", 12, false);
            Invoke(controller, "CompleteLevel");
            viewer.Skip();
            AssertJourney();
            PlayerPrefs.SetInt("PipeMuzzle.Feedback.SoundEnabled", 0);
            ProgressResetService.ResetAllProgress();
            var progress = new WorldProgressService();
            Assert.That(progress.IsJourneyCompleted, Is.False);
            Assert.That(progress.IsWorldUnlocked(WorldId.SakuraGarden), Is.True);
            Assert.That(progress.IsWorldUnlocked(WorldId.BambooWorkshop), Is.False);
            Assert.That(progress.IsWorldUnlocked(WorldId.MoonShrine), Is.False);
            Assert.That(PlayerPrefs.GetInt("PipeMuzzle.Feedback.SoundEnabled", 1), Is.Zero);
            Assert.That(new StoryCheckpointProgress().HasViewed(WorldId.MoonShrine, 12), Is.False);
            screens.ShowWorldMap();
            Assert.That(screens.IsJourneyCompleteOpen, Is.False);
        }

        [TestCase("SakuraGarden", 12)]
        [TestCase("BambooWorkshop", 12)]
        [TestCase("MoonShrine", 9)]
        public void OtherCompletionsCannotOpenJourney(string world, int level)
        {
            PrepareLevel(world, level, false);
            Assert.That(screens.TryShowJourneyComplete(controller), Is.False);
            Invoke(controller, "CompleteLevel");
            viewer.Skip();
            Assert.That(screens.TryShowJourneyComplete(controller), Is.False);
            Assert.That(screens.IsJourneyCompleteOpen, Is.False);
        }

        [TestCase(1920, 1080)]
        [TestCase(1024, 768)]
        [TestCase(640, 360)]
        [TestCase(720, 1280)]
        public void JourneyAndCreditsFitAvailableArea(int width, int height)
        {
            PrepareLevel("MoonShrine", 12, false);
            Assert.That(screens.TryShowJourneyComplete(controller), Is.False, "Unsolved Moon 12 cannot open the finale.");
            Invoke(controller, "CompleteLevel");
            viewer.Skip();
            JourneyCompleteUI ui = gameplay.transform.parent.GetComponentInChildren<JourneyCompleteUI>(true);
            RectTransform area = (RectTransform)ui.transform.Find("SafeArea");
            area.anchorMin = area.anchorMax = Vector2.zero;
            area.sizeDelta = new Vector2(width, height);
            Invoke(ui, "FitSurface");
            RectTransform surface = (RectTransform)area.Find("Surface");
            Assert.That(surface.rect.width * surface.localScale.x, Is.LessThanOrEqualTo(width - 48));
            Assert.That(surface.rect.height * surface.localScale.y, Is.LessThanOrEqualTo(height - 48));
            foreach (TMP_Text text in surface.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate(true);
                Assert.That(text.isTextOverflowing, Is.False, text.text);
            }
            foreach (Image artwork in surface.Find("Journey").GetComponentsInChildren<Image>(true)
                .Where(image => image.name.StartsWith("World") && !image.name.Contains("Button")))
                Assert.That(artwork.sprite, Is.Not.Null);
        }

        private void AssertJourney()
        {
            Assert.That(screens.IsJourneyCompleteOpen, Is.True);
            Assert.That(gameplay.activeSelf, Is.False);
            Assert.That(completion.activeSelf, Is.False);
            Assert.That(select.activeSelf, Is.False);
            Assert.That(new WorldProgressService().IsJourneyCompleted, Is.True);
            TMP_Text count = gameplay.transform.parent.Find("JourneyComplete/SafeArea/Surface/Journey/LevelCount").GetComponent<TMP_Text>();
            Assert.That(count.text, Is.EqualTo("36 / 36 LEVELS"));
        }

        private WorldDefinition PrepareLevel(string worldName, int checkpoint, bool completedBefore)
        {
            WorldDefinition world = LoadWorld(worldName);
            // Legitimate Moon access follows completion of both previous worlds.
            if (worldName == "MoonShrine")
                foreach (string previous in new[] { "SakuraGarden", "BambooWorkshop" })
                {
                    PlayerPrefs.SetInt(ProgressKey(previous, "Unlocked"), 1);
                    PlayerPrefs.SetInt(ProgressKey(previous, "Completed"), 1);
                }
            PlayerPrefs.SetInt(ProgressKey(worldName, "Unlocked"), 1);
            PlayerPrefs.SetInt(ProgressKey(worldName, "HighestUnlockedLevel"),
                completedBefore && checkpoint < world.LevelCount ? checkpoint : checkpoint - 1);
            if (completedBefore && checkpoint == world.LevelCount)
                PlayerPrefs.SetInt(ProgressKey(worldName, "Completed"), 1);
            Assert.That(world.GetStoryCheckpoint(checkpoint), Is.Not.Null);
            Assert.That(controller.ConfigureWorld(world), Is.True);
            screens.ShowGameplay();
            controller.LoadLevelByIndex(checkpoint - 1);
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(checkpoint));
            return world;
        }

        private void FinishComic(bool skip)
        {
            if (skip) viewer.Skip();
            else
            {
                // EditMode has no frame-driven fades. Set the final-panel state and exercise
                // the real public Advance terminal branch and StoryCompleted subscriber.
                ComicStoryDefinition story = GetField<ComicStoryDefinition>(viewer, "currentStory");
                SetField(viewer, "currentPanelIndex", story.PanelCount - 1);
                viewer.Advance();
            }
        }

        private void CreateHarness()
        {
            BoardView board = Create("CheckpointBoard").AddComponent<BoardView>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TilePrefab.prefab");
            Assert.That(prefab, Is.Not.Null);
            SetField(board, "tilePrefab", prefab.GetComponent<TileView>());
            GameObject cameraObject = Create("CheckpointCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            BoardCameraFitter fitter = cameraObject.AddComponent<BoardCameraFitter>();
            SetField(fitter, "targetCamera", camera);
            SetField(fitter, "boardView", board);
            controller = Create("CheckpointController").AddComponent<GameController>();
            SetField(controller, "boardView", board);
            SetField(controller, "boardCameraFitter", fitter);

            // Avoid scene startup logic; production navigation and event handlers are used.
            GameObject root = Create("CheckpointUiRoot");
            root.SetActive(false);
            screens = root.AddComponent<ScreenManager>();
            levelSelect = root.AddComponent<LevelSelectUI>();
            StoryNavigationCoordinator navigation = root.AddComponent<StoryNavigationCoordinator>();
            GameObject map = Ui("WorldMap", root.transform);
            comic = Ui("Comic", root.transform);
            select = Ui("LevelSelect", root.transform);
            gameplay = Ui("Gameplay", root.transform);
            screens.Configure(map, comic, select, gameplay);

            viewer = comic.AddComponent<ComicViewerUI>();
            panelImage = Ui("PanelImage", comic.transform).AddComponent<Image>();
            viewer.Configure(panelImage, comic.AddComponent<CanvasGroup>(),
                Button("Advance", comic.transform), Button("Skip", comic.transform),
                Button("Back", comic.transform), Text("Continue", comic.transform));
            navigation.Configure(screens, viewer);
            SetField(levelSelect, "gameController", controller);
            SetField(levelSelect, "levelSelectPanel", select);
            SetField(levelSelect, "gameplayHUD", gameplay);
            SetField(levelSelect, "screenManager", screens);
            RectTransform path = Ui("PathArea", select.transform).GetComponent<RectTransform>();
            SetField(levelSelect, "pathArea", path);
            var buttons = new List<Button>();
            for (int index = 0; index < 12; index++) buttons.Add(Button($"Level{index + 1}", path));
            SetField(levelSelect, "levelButtons", buttons);

            gameUi = gameplay.AddComponent<GameUI>();
            completion = Ui("CompletionPanel", gameplay.transform);
            completion.AddComponent<Image>();
            completionText = Text("CompletionText", completion.transform);
            next = Button("NextButton", completion.transform);
            SetField(gameUi, "gameController", controller);
            SetField(gameUi, "levelText", Text("LevelText", gameplay.transform));
            SetField(gameUi, "moveCountText", Text("Moves", gameplay.transform));
            SetField(gameUi, "completionPanel", completion);
            SetField(gameUi, "completionText", completionText);
            SetField(gameUi, "restartButton", Button("Restart", gameplay.transform));
            SetField(gameUi, "nextButton", next);
            Invoke(gameUi, "OnEnable");
            screens.ShowGameplay();
        }

        private void SaveAndClear(string key)
        {
            originalPrefs.Add(key, (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key)));
            PlayerPrefs.DeleteKey(key);
        }

        private static string ProgressKey(string world, string suffix) => $"PipeMuzzle.Progress.World.{world}.{suffix}";
        private static WorldDefinition LoadWorld(string name)
        {
            WorldDefinition world = AssetDatabase.LoadAssetAtPath<WorldDefinition>($"Assets/Resources/Worlds/{name}.asset");
            Assert.That(world, Is.Not.Null);
            return world;
        }

        private GameObject Create(string name)
        {
            GameObject result = new(name);
            created.Add(result);
            return result;
        }

        private GameObject Ui(string name, Transform parent)
        {
            GameObject result = new(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            return result;
        }

        private Button Button(string name, Transform parent)
        {
            GameObject result = Ui(name, parent);
            result.AddComponent<Image>();
            return result.AddComponent<Button>();
        }

        private TMP_Text Text(string name, Transform parent) => Ui(name, parent).AddComponent<TextMeshProUGUI>();
        private static void SetField(object target, string name, object value) => Field(target, name).SetValue(target, value);
        private static T GetField<T>(object target, string name) => (T)Field(target, name).GetValue(target);
        private static FieldInfo Field(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return field;
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, null);
        }
    }
}
