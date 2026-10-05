using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using PipeMuzzle.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class FirstLevelTutorialTests
    {
        private const string SeenKey = "PipeMuzzle.Tutorial.BasicRotationSeen";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Dictionary<string, (bool exists, int value)> original = new();
        private readonly List<Object> created = new();
        private GameController controller;
        private BoardView boardView;
        private Component tutorial;

        [SetUp]
        public void SetUp()
        {
            Type type = typeof(GameController).Assembly.GetType("PipeMuzzle.UI.FirstLevelTutorial");
            Assert.That(type, Is.Not.Null, "First-level onboarding component is not implemented yet.");
            SaveAndClear(SeenKey);
            SaveAndClear("PipeMuzzle.HighestUnlockedLevel");
            SaveAndClear("PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1");
            foreach (string world in new[] { "SakuraGarden", "BambooWorkshop", "MoonShrine" })
            {
                foreach (string suffix in new[] { "HighestUnlockedLevel", "Unlocked", "Completed" })
                    SaveAndClear($"PipeMuzzle.Progress.World.{world}.{suffix}");
                foreach (int n in new[] { 0, 3, 6, 9, 12 })
                    SaveAndClear($"PipeMuzzle.Story.World.{world}.Checkpoint.{n}.Viewed");
            }

            boardView = Create("TutorialTestBoard").AddComponent<BoardView>();
            SetReference(boardView, "tilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/TilePrefab.prefab").GetComponent<TileView>());
            Camera camera = Create("TutorialTestCamera").AddComponent<Camera>();
            camera.orthographic = true;
            BoardCameraFitter fitter = camera.gameObject.AddComponent<BoardCameraFitter>();
            SetReference(fitter, "targetCamera", camera);
            SetReference(fitter, "boardView", boardView);
            controller = Create("TutorialTestController").AddComponent<GameController>();
            SetReference(controller, "boardView", boardView);
            SetReference(controller, "boardCameraFitter", fitter);
            Invoke(controller, "Start");

            GameObject hud = Create("TutorialTestHud", typeof(RectTransform), typeof(Canvas));
            hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            tutorial = hud.AddComponent(type);
            type.GetMethod("Configure").Invoke(tutorial, new object[] { controller, boardView, camera });
            Load("SakuraGarden", 1);
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode does not dispatch lifecycle callbacks for this runtime component.
            if (tutorial != null) { Invoke(tutorial, "OnDisable"); Invoke(tutorial, "OnDestroy"); }
            for (int index = created.Count - 1; index >= 0; index--)
                if (created[index] != null) Object.DestroyImmediate(created[index]);
            created.Clear();
            foreach (var entry in original)
            {
                if (entry.Value.exists) PlayerPrefs.SetInt(entry.Key, entry.Value.value);
                else PlayerPrefs.DeleteKey(entry.Key);
            }
            PlayerPrefs.Save();
            original.Clear();
        }

        [Test]
        public void FreshSakuraOneHighlightsTheCentralRotatablePipeWithoutBlockingInput()
        {
            Assert.That(Showing, Is.True);
            TileView selected = Selected;
            Assert.That((selected.State.X, selected.State.Y), Is.EqualTo((1, 1)));
            Assert.That(selected.State.IsLocked, Is.False);
            Assert.That(controller.CurrentMoveCount, Is.Zero);
            Assert.That(PlayerPrefs.HasKey(SeenKey), Is.False);
            Assert.That(tutorial.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget), Is.True);
            CanvasGroup group = tutorial.GetComponentInChildren<CanvasGroup>();
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(group.interactable, Is.False);
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        public void FirstSuccessfulRotationOfEitherSolutionOrDistractorDismissesAndPersists(int x, int y)
        {
            Advance(.6f);
            TileView selected = Selected;
            Click(x, y);
            Assert.That(controller.CurrentMoveCount, Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt(SeenKey), Is.EqualTo(1));
            Advance(.125f);
            Assert.That(Showing, Is.True, "Dismissal fades instead of disappearing instantly.");
            Assert.That(tutorial.GetComponentInChildren<CanvasGroup>().alpha, Is.InRange(.01f, .99f));
            Advance(.125f);
            Assert.That(Showing, Is.False);
            if (!selected.HasPendingRotation) Assert.That(selected.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [TestCase(1, 0)]
        [TestCase(1, 2)]
        [TestCase(0, 0)]
        public void LockedAndEmptyClicksDoNotCompleteTheTutorial(int x, int y)
        {
            Click(x, y);
            Assert.That(controller.CurrentMoveCount, Is.Zero);
            Assert.That(Showing, Is.True);
            Assert.That(PlayerPrefs.HasKey(SeenKey), Is.False);
        }

        [Test]
        public void RightClickDoesNotCompleteTheTutorial()
        {
            Selected.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Right });
            Assert.That(controller.CurrentMoveCount, Is.Zero);
            Assert.That(Showing, Is.True);
            Assert.That(PlayerPrefs.HasKey(SeenKey), Is.False);
        }

        [Test]
        public void RestartBeforeRotationRecreatesTheGuidanceOnTheNewBoard()
        {
            TileView old = Selected;
            Advance(.6f);
            controller.RestartLevel();
            Assert.That(Showing, Is.True);
            Assert.That(Selected, Is.Not.SameAs(old));
            Assert.That(old == null, Is.True, "Restart destroys the previous board's tile views.");
            Assert.That(Selected.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(PlayerPrefs.HasKey(SeenKey), Is.False);
        }

        [Test]
        public void RestartAfterRotationDoesNotRepeatTheTutorial()
        {
            Click(2, 1);
            controller.RestartLevel();
            Assert.That(Showing, Is.False);
            Assert.That(PlayerPrefs.GetInt(SeenKey), Is.EqualTo(1));
        }

        [Test]
        public void SeenTutorialDoesNotReturnOnReloadOrHudReenable()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            controller.RestartLevel();
            tutorial.gameObject.SetActive(false);
            Invoke(tutorial, "OnDisable");
            tutorial.gameObject.SetActive(true);
            Invoke(tutorial, "OnEnable");
            Assert.That(Showing, Is.False);
        }

        [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)]
        [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)] [TestCase(12)]
        public void LaterSakuraLevelsNeverShowTutorial(int number)
        {
            Load("SakuraGarden", number);
            Assert.That(Showing, Is.False);
            Assert.That(PlayerPrefs.HasKey(SeenKey), Is.False);
        }

        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void OtherWorldsNeverShowTutorial(string world)
        {
            Load(world, 1);
            Assert.That(Showing, Is.False);
        }

        [Test]
        public void LeavingGameplayClearsPulseAndAllowsAnUnfinishedTutorialOnReturn()
        {
            TileView selected = Selected;
            Advance(.6f);
            tutorial.gameObject.SetActive(false);
            Invoke(tutorial, "OnDisable");
            Assert.That(Showing, Is.False);
            Assert.That(selected.transform.localScale, Is.EqualTo(Vector3.one));
            tutorial.gameObject.SetActive(true);
            Invoke(tutorial, "OnEnable");
            Assert.That(Showing, Is.True);
        }

        [Test]
        public void DebugResetDeletesOnlyTutorialFlagAndRearmsOnRestart()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            var saved = original.Keys.Where(k => k != SeenKey)
                .ToDictionary(k => k, k => (exists: PlayerPrefs.HasKey(k), value: PlayerPrefs.GetInt(k)));
            Type menu = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("PipeMuzzle.Editor.PipeMuzzleTutorialDebugMenu")).First(t => t != null);
            menu.GetMethod("ResetTutorial").Invoke(null, null);
            Assert.That(PlayerPrefs.HasKey(SeenKey), Is.False);
            foreach (var item in saved)
                Assert.That((PlayerPrefs.HasKey(item.Key), PlayerPrefs.GetInt(item.Key)), Is.EqualTo(item.Value));
            controller.RestartLevel();
            Assert.That(Showing, Is.True);
        }

        private bool Showing => (bool)tutorial.GetType().GetProperty("IsShowing").GetValue(tutorial);
        private TileView Selected => (TileView)tutorial.GetType().GetProperty("HighlightedTile").GetValue(tutorial);
        private void Advance(float delta) => Invoke(tutorial, "Advance", delta);
        private void Click(int x, int y) => boardView.GetComponentsInChildren<TileView>()
            .Single(t => t.State.X == x && t.State.Y == y)
            .OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });

        private void Load(string name, int number)
        {
            PlayerPrefs.SetInt($"PipeMuzzle.Progress.World.{name}.Unlocked", 1);
            WorldDefinition world = Resources.Load<WorldDefinition>("Worlds/" + name);
            new ProgressService(world.WorldId, world.LevelCount).UnlockLevel(number - 1);
            Assert.That(controller.ConfigureWorld(world), Is.True);
            controller.LoadLevelByIndex(number - 1);
        }

        private GameObject Create(string name, params Type[] components)
        {
            GameObject root = new(name, components);
            created.Add(root);
            return root;
        }
        private void SaveAndClear(string key)
        {
            original[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
            PlayerPrefs.DeleteKey(key);
        }
        private static void SetReference(Object target, string field, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, PrivateInstance).Invoke(target, args);
    }
}
