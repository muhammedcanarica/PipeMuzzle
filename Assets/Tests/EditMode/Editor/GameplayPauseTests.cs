using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using PipeMuzzle.UI;
using PipeMuzzle.View;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class GameplayPauseTests
    {
        private readonly List<Object> created = new();
        private GameController controller;
        private BoardView boardView;
        private readonly Dictionary<string, (bool exists, int value)> saved = new();

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            foreach (string key in new[] { "PipeMuzzle.BestMoves.World.SakuraGarden.Level.1",
                "PipeMuzzle.Progress.World.SakuraGarden.HighestUnlockedLevel",
                "PipeMuzzle.HighestUnlockedLevel",
                "PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1" })
            {
                saved[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
                PlayerPrefs.DeleteKey(key);
            }
            boardView = Create("PauseBoard").AddComponent<BoardView>();
            Set(boardView, "tilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/TilePrefab.prefab").GetComponent<TileView>());
            GameObject cameraObject = Create("PauseCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            BoardCameraFitter fitter = cameraObject.AddComponent<BoardCameraFitter>();
            Set(fitter, "targetCamera", camera);
            Set(fitter, "boardView", boardView);
            controller = Create("PauseController").AddComponent<GameController>();
            Set(controller, "boardView", boardView);
            Set(controller, "boardCameraFitter", fitter);
            Assert.That(controller.ConfigureWorld(Resources.Load<WorldDefinition>("Worlds/SakuraGarden")), Is.True);
            controller.LoadLevelByIndex(0);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
            Time.timeScale = 1f;
            foreach (var item in saved)
                if (item.Value.exists) PlayerPrefs.SetInt(item.Key, item.Value.value);
                else PlayerPrefs.DeleteKey(item.Key);
            saved.Clear();
            PlayerPrefs.Save();
        }

        [Test]
        public void PauseRejectsRotationAndHintAndResumePreservesBoard()
        {
            new ProgressService(WorldId.SakuraGarden, 12).UnlockLevel(11);
            controller.ConfigureWorld(Resources.Load<WorldDefinition>("Worlds/SakuraGarden"));
            controller.LoadLevelByIndex(11);
            BoardState board = Get<BoardState>(controller, "board");
            Assert.That(controller.TryShowHint(), Is.True);
            TileView tile = boardView.GetComponentsInChildren<TileView>()
                .First(t => t.State.Role == TileRole.Normal && !t.State.IsLocked && !t.State.IsHintLocked && t.State.Shape != TileShape.Empty);
            int rotation = tile.State.Rotation;
            Assert.That(Pause(), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(boardView.IsHintPlaying, Is.False);
            Assert.That(controller.CanHint, Is.False);
            Assert.That(controller.TryShowHint(), Is.False);
            CallPrivate(controller, "HandleTileClicked", tile);
            Assert.That(tile.State.Rotation, Is.EqualTo(rotation));
            Assert.That(controller.CurrentMoveCount, Is.Zero);
            Call(controller, "Resume");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Get<BoardState>(controller, "board"), Is.SameAs(board));
            Assert.That(controller.CanHint, Is.False, "Pause must not bypass hint cooldown.");
            Set(controller, "hintAvailableAt", Time.unscaledTime - .1f);
            Assert.That(controller.CanHint, Is.True);
            CallPrivate(controller, "HandleTileClicked", tile);
            Assert.That(controller.CurrentMoveCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FlowAndCompletionRejectPause(bool finish)
        {
            BoardState solved = new(3, 1);
            solved.SetTile(new TileState(0, 0, TileShape.Straight, TileRole.Source, 1, true));
            solved.SetTile(new TileState(1, 0, TileShape.Straight, TileRole.Normal, 1, false));
            solved.SetTile(new TileState(2, 0, TileShape.Straight, TileRole.Target, 1, true));
            Set(controller, "board", solved);
            boardView.Build(solved);
            CallPrivate(controller, "BeginCompletion");
            EnergyFlowView flow = boardView.GetComponent<EnergyFlowView>();
            if (finish) { flow.Advance(2f); flow.Advance(.6f); }
            Assert.That(Pause(), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(controller.IsCompletionPending || controller.IsCompleted, Is.True);
        }

        [Test]
        public void RestartClearsPauseMovesAndHintButKeepsBest()
        {
            BestMovesProgress.TrySetBest(WorldId.SakuraGarden, 1, 7);
            new ProgressService(WorldId.SakuraGarden, 12).UnlockLevel(11);
            controller.ConfigureWorld(Resources.Load<WorldDefinition>("Worlds/SakuraGarden"));
            controller.LoadLevelByIndex(11);
            Get<BoardState>(controller, "board").IncrementMoveCount();
            controller.TryShowHint();
            Assert.That(Pause(), Is.True);
            controller.RestartLevel();
            Assert.That(IsPaused(), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(controller.CurrentMoveCount, Is.Zero);
            Assert.That(boardView.IsHintPlaying, Is.False);
            Assert.That(boardView.GetComponent<EnergyFlowView>().IsPlaying, Is.False);
            Assert.That(BestMovesProgress.GetBest(WorldId.SakuraGarden, 1), Is.EqualTo(7));
        }

        [TestCase("disable")]
        [TestCase("destroy")]
        [TestCase("configure")]
        [TestCase("load")]
        public void LeavingGameplayOrLoadingAnotherBoardRestoresTime(string action)
        {
            Assert.That(Pause(), Is.True);
            // EditMode fixtures do not dispatch runtime MonoBehaviour callbacks.
            if (action == "disable") { controller.gameObject.SetActive(false); CallPrivate(controller, "OnDisable"); }
            else if (action == "destroy") { CallPrivate(controller, "OnDestroy"); Object.DestroyImmediate(controller.gameObject); }
            else if (action == "configure") controller.ConfigureWorld(controller.CurrentWorld);
            else controller.LoadLevelByIndex(0);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            if (controller != null) Assert.That(IsPaused(), Is.False);
        }

        [Test]
        public void GameplaySettingsBackKeepsPauseAndLeavingClearsReturnContext()
        {
            GameObject root = Create("Screens", typeof(RectTransform));
            ScreenManager screens = root.AddComponent<ScreenManager>();
            GameObject map = CreateChild("Map", root.transform);
            GameObject hud = CreateChild("HUD", root.transform);
            GameObject settingsObject = CreateChild("Settings", root.transform);
            SettingsUI settings = settingsObject.AddComponent<SettingsUI>();
            settings.Initialize(screens, null);
            screens.Configure(map, null, null, hud);
            screens.ConfigureSettings(settingsObject);
            screens.ShowGameplay();
            Assert.That(Pause(), Is.True);
            Call(screens, "ShowGameplaySettings");
            Assert.That(hud.activeSelf, Is.True, "Gameplay stays suspended behind Settings.");
            Assert.That(settingsObject.activeSelf, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            settingsObject.transform.Find("Content/Surface/BackButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(hud.activeSelf, Is.True);
            Assert.That(settingsObject.activeSelf, Is.False);
            Assert.That(IsPaused(), Is.True);
            Call(screens, "ShowGameplaySettings");
            screens.ShowWorldMap();
            screens.ShowSettings();
            settingsObject.transform.Find("Content/Surface/BackButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(map.activeSelf, Is.True, "Map settings must not retain a gameplay return.");
        }

        [TestCase(1920f, 1080f)]
        [TestCase(390f, 844f)]
        [TestCase(844f, 390f)]
        public void MenuFitsViewportAndContainsOnlyFourActions(float width, float height)
        {
            var ui = BuildPauseUI(width, height);
            ui.hud.transform.Find("PauseButton").GetComponent<Button>().onClick.Invoke();
            RectTransform overlay = (RectTransform)ui.hud.transform.Find("PauseOverlay");
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            Assert.That(overlay.GetComponentsInChildren<Button>().Select(b => b.GetComponent<TMP_Text>().text),
                Is.EquivalentTo(new[] { "RESUME", "RESTART", "LEVELS", "SETTINGS" }));
            Canvas.ForceUpdateCanvases();
            RectTransform paper = (RectTransform)overlay.Find("Paper");
            Vector3[] corners = new Vector3[4];
            paper.GetWorldCorners(corners);
            Assert.That(((RectTransform)ui.hud.transform).rect.Contains(ui.hud.transform.InverseTransformPoint(corners[0])), Is.True);
            Assert.That(((RectTransform)ui.hud.transform).rect.Contains(ui.hud.transform.InverseTransformPoint(corners[2])), Is.True);
            Assert.That(ui.levels.interactable, Is.False);
            paper.Find("ResumeButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(IsPaused(), Is.False);
            Assert.That(overlay.gameObject.activeSelf, Is.False);
            Assert.That(ui.levels.interactable, Is.True);
        }

        [Test]
        public void EscapeToggleIgnoresSettingsConfirmationAndRestoresTimeOnNavigation()
        {
            var ui = BuildPauseUI();
            Call(ui.pause, "HandleEscape");
            Assert.That(IsPaused(), Is.True);
            Call(ui.pause, "HandleEscape");
            Assert.That(IsPaused(), Is.False);
            Call(ui.pause, "HandleEscape");
            ui.hud.transform.Find("PauseOverlay/Paper/SettingsButton").GetComponent<Button>().onClick.Invoke();
            ui.settings.transform.Find("Content/Surface/ResetProgressButton").GetComponent<Button>().onClick.Invoke();
            Call(ui.pause, "HandleEscape");
            Assert.That(IsPaused(), Is.True);
            Assert.That(ui.settings.transform.Find("Content/Confirmation").gameObject.activeSelf, Is.True);
            Assert.That(ui.hud.transform.Find("PauseOverlay").GetComponent<CanvasGroup>().interactable, Is.False);
            ui.hud.transform.Find("PauseOverlay/Paper/ResumeButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(IsPaused(), Is.True, "Settings must isolate hidden pause actions.");
            Assert.That(ui.settings.transform.Find("Content/Surface/BackButton").GetComponent<Button>().interactable, Is.True);
            ui.settings.transform.Find("Content/Confirmation/CancelButton").GetComponent<Button>().onClick.Invoke();
            ui.settings.transform.Find("Content/Surface/BackButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(ui.hud.transform.Find("PauseOverlay").gameObject.activeSelf, Is.True);
            Assert.That(ui.hud.transform.Find("PauseOverlay").GetComponent<CanvasGroup>().interactable, Is.True);
            ui.screens.ShowWorldMap();
            CallPrivate(ui.pause, "OnDisable");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(IsPaused(), Is.False);
            ui.screens.ShowGameplay();
            Assert.That(ui.hud.transform.Find("PauseOverlay").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void MenuRestartUsesControllerOnceAfterRepeatedEnableAndPause()
        {
            var ui = BuildPauseUI();
            int loaded = 0;
            controller.LevelLoaded += (_, _) => loaded++;
            for (int i = 0; i < 3; i++)
            {
                ui.hud.SetActive(false);
                CallPrivate(ui.pause, "OnDisable");
                ui.hud.SetActive(true);
                CallPrivate(ui.pause, "OnEnable");
                Call(ui.pause, "HandleEscape");
                ui.hud.transform.Find("PauseOverlay/Paper/RestartButton").GetComponent<Button>().onClick.Invoke();
            }
            Assert.That(loaded, Is.EqualTo(3), "Restart must dispatch once per click.");
            Assert.That(controller.CurrentMoveCount, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void MenuLevelsReusesExistingButtonAndClearsPause()
        {
            var ui = BuildPauseUI();
            int navigationCount = 0;
            ui.levels.onClick.AddListener(() => { navigationCount++; ui.screens.ShowLevelSelect(); });
            Call(ui.pause, "HandleEscape");
            ui.hud.transform.Find("PauseOverlay/Paper/LevelsButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(navigationCount, Is.EqualTo(1));
            Assert.That(ui.hud.activeSelf, Is.False);
            Assert.That(IsPaused(), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(controller.CurrentWorld.WorldId, Is.EqualTo(WorldId.SakuraGarden));
        }

        [Test]
        public void UnlabelledRestartTemplateStillBuildsPauseMenu()
        {
            var ui = BuildPauseUI(addLabel: false);
            Assert.That(ui.hud.transform.Find("PauseButton").GetComponentInChildren<TMP_Text>().text, Is.EqualTo("PAUSE"));
            Call(ui.pause, "HandleEscape");
            Assert.That(ui.hud.transform.Find("PauseOverlay").gameObject.activeSelf, Is.True);
        }

        private (GameObject hud, Component pause, ScreenManager screens, Button levels, GameObject settings) BuildPauseUI(float width = 1920f, float height = 1080f, bool addLabel = true)
        {
            GameObject root = Create("PauseScreens", typeof(RectTransform));
            ScreenManager screens = root.AddComponent<ScreenManager>();
            GameObject hud = CreateChild("HUD", root.transform);
            ((RectTransform)hud.transform).sizeDelta = new Vector2(width, height);
            GameObject template = CreateChild("RestartTemplate", hud.transform);
            template.AddComponent<Image>();
            Button button = template.AddComponent<Button>();
            if (addLabel) CreateChild("Label", template.transform).AddComponent<TextMeshProUGUI>();
            GameObject levels = CreateChild("LevelsButton", hud.transform);
            levels.AddComponent<Image>();
            Button levelsButton = levels.AddComponent<Button>();
            GameObject map = CreateChild("Map", root.transform);
            GameObject settings = CreateChild("Settings", root.transform);
            settings.SetActive(false);
            settings.AddComponent<SettingsUI>().Initialize(screens, null);
            screens.Configure(map, null, null, hud);
            screens.ConfigureSettings(settings);
            screens.ShowGameplay();
            Type type = typeof(GameUI).Assembly.GetType("PipeMuzzle.UI.GameplayPauseUI");
            Assert.That(type, Is.Not.Null, "Gameplay pause UI is missing.");
            Component pause = hud.AddComponent(type);
            type.GetMethod("Initialize").Invoke(pause, new object[] { controller, button, levelsButton, screens });
            return (hud, pause, screens, levelsButton, settings);
        }

        [Test]
        public void CanvasHostedGameUICreatesPauseOnHUDAndCannotPauseHiddenGameplay()
        {
            GameObject canvas = Create("Canvas", typeof(RectTransform));
            canvas.SetActive(false);
            GameObject hud = CreateChild("GameplayHUD", canvas.transform);
            GameUI gameUi = canvas.AddComponent<GameUI>();
            GameObject restart = CreateChild("Restart", hud.transform);
            restart.AddComponent<Image>();
            Button template = restart.AddComponent<Button>();
            CreateChild("Label", restart.transform).AddComponent<TextMeshProUGUI>();
            Set(gameUi, "restartButton", template);
            Set(gameUi, "gameController", controller);
            CallPrivate(gameUi, "EnsurePauseControls");
            Assert.That(canvas.GetComponents<Component>().Any(c => c != null && c.GetType().Name == "GameplayPauseUI"), Is.False);
            Component pause = hud.GetComponents<Component>().First(c => c != null && c.GetType().Name == "GameplayPauseUI");
            Assert.That(hud.transform.Find("PauseOverlay"), Is.Not.Null);
            Call(pause, "HandleEscape");
            Assert.That(IsPaused(), Is.False, "Inactive HUD must ignore ESC with a loaded board.");
        }

        private bool Pause() => (bool)Call(controller, "TryPause");
        private bool IsPaused()
        {
            PropertyInfo property = typeof(GameController).GetProperty("IsPaused");
            Assert.That(property, Is.Not.Null, "Controller pause state is missing.");
            return (bool)property.GetValue(controller);
        }
        private static object Call(object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method);
            Assert.That(info, Is.Not.Null, method + " is missing.");
            return info.Invoke(target, null);
        }
        private static void CallPrivate(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
        private static T Get<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private GameObject Create(string name, params Type[] types)
        {
            GameObject obj = new(name, types);
            created.Add(obj);
            return obj;
        }
        private GameObject CreateChild(string name, Transform parent)
        {
            GameObject obj = Create(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }
    }
}
