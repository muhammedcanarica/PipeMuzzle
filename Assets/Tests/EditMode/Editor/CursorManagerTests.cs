using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using PipeMuzzle.UI;
using PipeMuzzle.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class CursorManagerTests
    {
        private readonly List<Object> created = new();
        private Type managerType;
        private Component manager;
        private GameController controller;

        [SetUp]
        public void SetUp()
        {
            managerType = typeof(ScreenManager).Assembly.GetType("PipeMuzzle.UI.CursorManager");
            Assert.That(managerType, Is.Not.Null, "The central cursor manager must exist.");
            manager = Create("TestCursor").AddComponent(managerType);
            SetStatic("instance", manager);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
            if (managerType != null) SetStatic("instance", null);
        }

        [Test]
        public void ResourcesPrefabProvidesFourReadableNativeCursorTextures()
        {
            GameObject prefab = Resources.Load<GameObject>("UI/CursorManager");
            Assert.That(prefab, Is.Not.Null);
            Component configuration = prefab.GetComponent(managerType);
            Assert.That(configuration, Is.Not.Null);
            foreach (string variant in new[] { "default", "hover", "rotate", "pressed" })
            {
                Texture2D texture = (Texture2D)Get(configuration, variant + "Cursor");
                Assert.That(texture, Is.Not.Null, variant);
                Assert.That(texture.width, Is.EqualTo(32), variant);
                Assert.That(texture.height, Is.EqualTo(32), variant);
                Assert.That(texture.isReadable, Is.True, variant);
                Assert.That(texture.mipmapCount, Is.EqualTo(1), variant);
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Cursor));
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                Assert.That(importer.alphaIsTransparency, Is.True);
                var hotspot = (Vector2)Get(configuration, variant + "Hotspot");
                Assert.That(hotspot.x, Is.InRange(0, 7), "Hotspot must stay near the pointed upper-left tip.");
                Assert.That(hotspot.y, Is.InRange(0, 7));
                Assert.That(texture.GetPixel(0, 0).a, Is.Zero);
                Assert.That(texture.GetPixel((int)hotspot.x + 1, texture.height - 2 - (int)hotspot.y).a,
                    Is.GreaterThan(.05f), "Visible petal should start beside the hotspot.");
            }
            Assert.That(StaticResult("EnsureInstance"), Is.SameAs(manager));
        }

        [Test]
        public void RecoveryValidatesReusedMousePointerBeforeRestoringHover()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Pen pen = InputSystem.AddDevice<Pen>();
            try
            {
                Button button = CreateButton();
                var data = new ExtendedPointerEventData(null)
                {
                    device = mouse, pointerType = UIPointerType.MouseOrPen,
                    pointerCurrentRaycast = new RaycastResult { gameObject = button.gameObject }
                };
                Static("EnterButton", button, data);
                Tick(1);
                Assert.That(State, Is.EqualTo("Hover"));
                // InputSystemUIInputModule reuses this record when switching mouse to pen.
                data.device = pen;
                Tick(1);
                Assert.That(State, Is.EqualTo("Default"), "Pen reuse must release the old mouse hover.");
                Static("ResetState");
                Call("RecoverPointerTarget");
                Tick(1);
                Assert.That(State, Is.EqualTo("Default"), "A pen raycast must never recover mouse hover.");
                data.device = mouse;
                Static("EnterButton", button, data);
                Static("ResetState");
                Call("RecoverPointerTarget");
                Tick(1);
                Assert.That(State, Is.EqualTo("Hover"), "Stationary mouse recovery must still work.");
            }
            finally { InputSystem.RemoveDevice(pen); InputSystem.RemoveDevice(mouse); }
        }

        [Test]
        public void MissingDefaultUsesSystemCursorEvenWhenHoverTextureExists()
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            created.Add(texture);
            Set(manager, "hoverCursor", texture);
            Static("EnterButton", CreateButton(), MouseEvent());
            Tick(1);
            Assert.That(Get(manager, "appliedTexture"), Is.Null);
        }

        [Test]
        public void ButtonHoverAndOldExitPreserveNewestTarget()
        {
            Button old = CreateButton();
            Button current = CreateButton();
            Static("EnterButton", old, MouseEvent());
            Static("EnterButton", current, MouseEvent());
            Static("ExitTarget", old, MouseEvent());
            Tick(1);
            Assert.That(State, Is.EqualTo("Hover"));
            Static("ExitTarget", current, MouseEvent());
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
        }

        [Test]
        public void DisabledButtonAndCanvasGroupRemoveHoverWithoutPointerMovement()
        {
            Button button = CreateButton();
            Static("EnterButton", button, MouseEvent());
            Tick(1);
            Assert.That(State, Is.EqualTo("Hover"));
            button.interactable = false;
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
            button.interactable = true;
            var group = button.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
        }

        [Test]
        public void TouchAndPenEventsDoNotOverrideMouseHover()
        {
            Button button = CreateButton();
            Static("EnterButton", button, new PointerEventData(null) { pointerId = 4 });
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
            var pen = new ExtendedPointerEventData(null) { pointerType = UIPointerType.MouseOrPen };
            Static("EnterButton", button, pen);
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"), "MouseOrPen alone must not accept a pen.");
            Static("EnterButton", button, MouseEvent());
            Static("ExitTarget", button, new ExtendedPointerEventData(null) { pointerType = UIPointerType.Touch });
            Tick(1);
            Assert.That(State, Is.EqualTo("Hover"));
        }

        [Test]
        public void InputSystemMouseDoesNotDependOnLegacyPointerId()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                Static("EnterButton", CreateButton(), new ExtendedPointerEventData(null)
                    { pointerId = mouse.deviceId, pointerType = UIPointerType.MouseOrPen, device = mouse });
                Tick(1);
                Assert.That(State, Is.EqualTo("Hover"));
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        [Test]
        public void PressExpiresUsingUnscaledTimeAndReturnsToCurrentTarget()
        {
            Static("EnterButton", CreateButton(), MouseEvent());
            Call("BeginPress", 10f);
            Tick(10.02f);
            Assert.That(State, Is.EqualTo("Pressed"));
            Tick(10.1f);
            Assert.That(State, Is.EqualTo("Hover"));
            Static("ResetState");
            Tick(10.1f);
            Assert.That(State, Is.EqualTo("Default"));
        }

        [Test]
        public void MissingVariantsUseDefaultAndMissingDefaultUsesSystemCursor()
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            created.Add(texture);
            Set(manager, "defaultCursor", texture);
            Set(manager, "defaultHotspot", new Vector2(4, 5));
            Static("EnterButton", CreateButton(), MouseEvent());
            Tick(1);
            Assert.That(Get(manager, "appliedTexture"), Is.SameAs(texture));
            Assert.That(Get(manager, "appliedHotspot"), Is.EqualTo(new Vector2(4, 5)));
            Set(manager, "defaultCursor", null);
            Tick(1);
            Assert.That(Get(manager, "appliedTexture"), Is.Null);
            Assert.That(Get(manager, "appliedHotspot"), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ScreenChangeFocusLossAndDisableClearTargets()
        {
            Static("EnterButton", CreateButton(), MouseEvent());
            Call("OnApplicationFocus", false);
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
            Call("OnApplicationFocus", true);
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
            Static("EnterButton", CreateButton(), MouseEvent());
            var screens = Create("Screens").AddComponent<ScreenManager>();
            screens.ShowWorldMap();
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
            Static("EnterButton", CreateButton(), MouseEvent());
            Call("OnDisable");
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
        }

        [Test]
        public void DestroyedTargetReturnsToDefault()
        {
            Button button = CreateButton();
            Static("EnterButton", button, MouseEvent());
            Object.DestroyImmediate(button.gameObject);
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
        }

        [TestCase(TileRole.Normal, false, TileShape.Straight, true)]
        [TestCase(TileRole.Normal, true, TileShape.Straight, false)]
        [TestCase(TileRole.Source, false, TileShape.Straight, false)]
        [TestCase(TileRole.Target, false, TileShape.Straight, false)]
        [TestCase(TileRole.Normal, false, TileShape.Empty, false)]
        public void PipeEligibilityMatchesPuzzleRules(TileRole role, bool locked, TileShape shape, bool expected)
        {
            TileView tile = CreateTile(role, locked, shape);
            Assert.That(CanInteract(tile), Is.EqualTo(expected));
            Static("EnterTile", tile, MouseEvent());
            Tick(1);
            Assert.That(State, Is.EqualTo(expected ? "Rotate" : "Default"));
        }

        [Test]
        public void StationaryHintLockImmediatelyRemovesRotate()
        {
            TileView tile = CreateTile();
            Static("EnterTile", tile, MouseEvent());
            Tick(1);
            Assert.That(State, Is.EqualTo("Rotate"));
            Assert.That(tile.State.TryApplyHint(1), Is.True);
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
        }

        [TestCase("isCompleting")]
        [TestCase("isCompleted")]
        [TestCase("<IsPaused>k__BackingField")]
        public void ControllerStateRemovesRotate(string field)
        {
            TileView tile = CreateTile();
            Static("EnterTile", tile, MouseEvent());
            Set(controller, field, true);
            Tick(1);
            Assert.That(CanInteract(tile), Is.False);
            Assert.That(State, Is.EqualTo("Default"));
        }

        [Test]
        public void StaleBoardPendingRotationAndInactiveControllerRejectInteraction()
        {
            TileView tile = CreateTile();
            Set(tile, "queuedQuarterTurns", 1);
            Assert.That(CanInteract(tile), Is.False);
            Set(tile, "queuedQuarterTurns", 0);
            controller.enabled = false;
            Assert.That(CanInteract(tile), Is.False);
            controller.enabled = true;
            Set(controller, "board", new BoardState(1, 1));
            Assert.That(CanInteract(tile), Is.False);
        }

        [Test]
        public void UiTargetTakesPriorityAndDoesNotLeavePipeBehindIt()
        {
            TileView tile = CreateTile();
            Static("EnterTile", tile, MouseEvent());
            Button button = CreateButton();
            Static("EnterButton", button, MouseEvent());
            Tick(1);
            Assert.That(State, Is.EqualTo("Hover"));
            button.interactable = false;
            Tick(1);
            Assert.That(State, Is.EqualTo("Default"));
        }

        [Test]
        public void ExistingButtonAndTileClickListenersAreNotDuplicatedOrConsumed()
        {
            Button button = CreateButton();
            int clicks = 0;
            button.onClick.AddListener(() => clicks++);
            UiSfxFeedback.BindHierarchy(button.transform);
            UiSfxFeedback.BindHierarchy(button.transform);
            var feedback = button.GetComponent<UiSfxFeedback>();
            ExecuteEvents.Execute(feedback.gameObject, MouseEvent(), ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(feedback.gameObject, MouseEvent(), ExecuteEvents.pointerDownHandler);
            button.onClick.Invoke();
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(button.GetComponents<UiSfxFeedback>(), Has.Length.EqualTo(1));
            TileView tile = CreateTile();
            int rotations = 0;
            tile.Clicked += _ => rotations++;
            ExecuteEvents.Execute(tile.gameObject, MouseEvent(), ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(tile.gameObject, MouseEvent(), ExecuteEvents.pointerDownHandler);
            tile.OnPointerClick(MouseEvent());
            Assert.That(rotations, Is.EqualTo(1));
        }

        private TileView CreateTile(TileRole role = TileRole.Normal, bool locked = false, TileShape shape = TileShape.Straight)
        {
            controller = Create("CursorController").AddComponent<GameController>();
            var state = new TileState(0, 0, shape, role, 0, locked);
            var board = new BoardState(1, 1);
            board.SetTile(state);
            Set(controller, "board", board);
            Set(manager, "controller", controller);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TilePrefab.prefab");
            var tileObject = Object.Instantiate(prefab);
            created.Add(tileObject);
            TileView tile = tileObject.GetComponent<TileView>();
            tile.Initialize(state);
            return tile;
        }

        private bool CanInteract(TileView tile)
        {
            var method = typeof(GameController).GetMethod("CanInteractWithTile");
            Assert.That(method, Is.Not.Null, "Cursor eligibility must use gameplay guards.");
            return (bool)method.Invoke(controller, new object[] { tile });
        }
        private GameObject Create(string name) { var go = new GameObject(name); created.Add(go); return go; }
        private Button CreateButton() => Create("CursorButton").AddComponent<Button>();
        private static PointerEventData MouseEvent() => new(null) { pointerId = -1, button = PointerEventData.InputButton.Left };
        private string State => managerType.GetProperty("CurrentState").GetValue(manager).ToString();
        private void Tick(float now) => Call("RefreshState", now);
        private void Static(string name, params object[] args) => managerType.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        private object StaticResult(string name, params object[] args) => managerType.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        private void Call(string name, params object[] args) => managerType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, args);
        private void SetStatic(string name, object value) => managerType.GetField(name, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value);
        private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }
}
