using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public sealed class GameplayPresentationTests
    {
        private static readonly string[] ProgressKeys =
        {
            "PipeMuzzle.HighestUnlockedLevel",
            "PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1",
            "PipeMuzzle.Progress.World.SakuraGarden.HighestUnlockedLevel",
            "PipeMuzzle.Progress.World.SakuraGarden.Unlocked",
            "PipeMuzzle.Progress.World.SakuraGarden.Completed",
            "PipeMuzzle.Progress.World.BambooWorkshop.Unlocked",
            "PipeMuzzle.Progress.World.MoonShrine.Unlocked"
        };

        private readonly List<Object> created = new();
        private readonly Dictionary<GameObject, string> originalTags = new();
        private readonly Dictionary<string, (bool exists, int value)>
            originalProgress = new();

        [SetUp]
        public void SetUp()
        {
            foreach (string key in ProgressKeys)
            {
                originalProgress[key] = (
                    PlayerPrefs.HasKey(key),
                    PlayerPrefs.GetInt(key)
                );
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();

            foreach (GameObject mainCamera in
                     GameObject.FindGameObjectsWithTag("MainCamera"))
            {
                originalTags[mainCamera] = mainCamera.tag;
                mainCamera.tag = "Untagged";
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    Object.DestroyImmediate(created[index]);
                }
            }

            foreach ((GameObject gameObject, string tag) in originalTags)
            {
                if (gameObject != null)
                {
                    gameObject.tag = tag;
                }
            }

            created.Clear();
            originalTags.Clear();

            foreach (string key in ProgressKeys)
            {
                if (originalProgress[key].exists)
                    PlayerPrefs.SetInt(key, originalProgress[key].value);
                else
                    PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            originalProgress.Clear();
        }

        [Test]
        public void ReconfiguringWorldKeepsOneCalmedBackground()
        {
            GameObject cameraObject = Create("PresentationCamera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;

            GameObject controllerObject = Create("PresentationController");
            WorldPresentationController controller =
                controllerObject.AddComponent<WorldPresentationController>();
            WorldDefinition sakura = World("SakuraGarden");
            WorldDefinition bamboo = World("BambooWorkshop");

            controller.Configure(sakura);
            GameObject duplicate = Create("WorldGameplayBackground");
            duplicate.transform.SetParent(camera.transform, false);
            duplicate.AddComponent<SpriteRenderer>();

            controller.Configure(bamboo);

            SpriteRenderer[] backgrounds = camera
                .GetComponentsInChildren<SpriteRenderer>(true)
                .Where(renderer =>
                    renderer.gameObject.name == "WorldGameplayBackground")
                .ToArray();

            Assert.That(backgrounds, Has.Length.EqualTo(1));
            Assert.That(backgrounds[0].sprite,
                Is.SameAs(bamboo.GameplayTheme.BackgroundSprite));
            Assert.That(backgrounds[0].color.a, Is.InRange(0.68f, 0.78f));
            Assert.That(backgrounds[0].transform.parent, Is.SameAs(camera.transform));
            Assert.That(backgrounds[0].transform.localPosition,
                Is.EqualTo(new Vector3(0f, 0f, 20f)));
            Assert.That(backgrounds[0].transform.localRotation,
                Is.EqualTo(Quaternion.identity));
            Assert.That(backgrounds[0].sortingOrder, Is.EqualTo(-1000));
            Assert.That(camera.backgroundColor,
                Is.EqualTo(bamboo.GameplayTheme.CameraBackgroundColor));
        }

        [Test]
        public void ApplyingThemeKeepsHudQuietAndWorldAware()
        {
            GameObject root = Create("GameplayUI");
            root.SetActive(false);
            GameUI gameUi = root.AddComponent<GameUI>();

            TMP_Text level = CreateText("LevelText", root.transform);
            TMP_Text moves = CreateText("MoveCountText", root.transform);
            GameObject completion = CreateUiObject("CompletionPanel", root.transform);
            completion.AddComponent<Image>();
            TMP_Text completionText = CreateText(
                "CompletionText",
                completion.transform
            );
            Button restart = CreateButton("RestartButton", root.transform);
            Button next = CreateButton("NextButton", completion.transform);

            SetField(gameUi, "levelText", level);
            SetField(gameUi, "moveCountText", moves);
            SetField(gameUi, "completionPanel", completion);
            SetField(gameUi, "completionText", completionText);
            SetField(gameUi, "restartButton", restart);
            SetField(gameUi, "nextButton", next);

            WorldGameplayTheme moon = World("MoonShrine").GameplayTheme;
            WorldGameplayTheme bamboo = World("BambooWorkshop").GameplayTheme;

            gameUi.ApplyTheme(moon);
            Color moonSecondary = restart.GetComponent<Image>().color;

            Assert.That(level.color,
                Is.EqualTo((Color)new Color32(54, 50, 61, 255)));
            Assert.That(moves.color,
                Is.EqualTo((Color)new Color32(54, 50, 61, 255)));
            Assert.That(next.GetComponent<Image>().color,
                Is.EqualTo(moon.PrimaryButtonColor));
            Assert.That(moonSecondary, Is.Not.EqualTo(moon.SecondaryButtonColor));

            gameUi.ApplyTheme(bamboo);
            gameUi.ApplyTheme(moon);
            gameUi.ApplyTheme(bamboo);

            Assert.That(restart.GetComponent<Image>().color,
                Is.Not.EqualTo(moonSecondary));
            Assert.That(next.GetComponent<Image>().color,
                Is.EqualTo(bamboo.PrimaryButtonColor));
            Assert.That(
                root.transform.Find("LevelSurface").GetSiblingIndex() + 1,
                Is.EqualTo(level.transform.GetSiblingIndex())
            );
            Assert.That(
                root.transform.Find("MovesSurface").GetSiblingIndex() + 1,
                Is.EqualTo(moves.transform.GetSiblingIndex())
            );
        }

        [Test]
        public void ExistingGameplayActionsRemainBound()
        {
            GameController controller = CreateController();
            Assert.That(controller.ConfigureWorld(World("SakuraGarden")), Is.True);
            controller.LoadLevelByIndex(0);

            GameObject root = Create("GameplayUI");
            root.SetActive(false);
            GameUI gameUi = root.AddComponent<GameUI>();
            TMP_Text level = CreateText("LevelText", root.transform);
            TMP_Text moves = CreateText("MoveCountText", root.transform);
            GameObject completion = CreateUiObject("CompletionPanel", root.transform);
            completion.AddComponent<Image>();
            TMP_Text completionText = CreateText(
                "CompletionText",
                completion.transform
            );
            Button restart = CreateButton("RestartButton", root.transform);
            Button next = CreateButton("NextButton", completion.transform);

            SetField(gameUi, "gameController", controller);
            SetField(gameUi, "levelText", level);
            SetField(gameUi, "moveCountText", moves);
            SetField(gameUi, "completionPanel", completion);
            SetField(gameUi, "completionText", completionText);
            SetField(gameUi, "restartButton", restart);
            SetField(gameUi, "nextButton", next);

            int loaded = 0;
            controller.LevelLoaded += (_, _) => loaded++;
            Invoke(gameUi, "OnEnable");

            restart.onClick.Invoke();
            Assert.That(loaded, Is.EqualTo(1));

            CompleteLoadedLevel(controller);
            next.onClick.Invoke();
            Assert.That(loaded, Is.EqualTo(2));
            Assert.That(level.text, Does.Contain("02"));
            Assert.That(moves.text, Does.Contain("0"));

            Button completionRestart = completion.transform
                .Find("CompletionRestartButton")
                .GetComponent<Button>();
            completionRestart.onClick.Invoke();
            Assert.That(loaded, Is.EqualTo(3));
        }

        private GameController CreateController()
        {
            GameObject boardObject = Create("TestBoardView");
            BoardView boardView = boardObject.AddComponent<BoardView>();
            GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/TilePrefab.prefab"
            );
            SerializedObject boardSerialized = new(boardView);
            boardSerialized.FindProperty("tilePrefab").objectReferenceValue =
                tilePrefab.GetComponent<TileView>();
            boardSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject cameraObject = Create("TestBoardCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            BoardCameraFitter fitter =
                cameraObject.AddComponent<BoardCameraFitter>();
            SerializedObject fitterSerialized = new(fitter);
            fitterSerialized.FindProperty("targetCamera").objectReferenceValue = camera;
            fitterSerialized.FindProperty("boardView").objectReferenceValue = boardView;
            fitterSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject controllerObject = Create("TestGameController");
            GameController controller =
                controllerObject.AddComponent<GameController>();
            SerializedObject controllerSerialized = new(controller);
            controllerSerialized.FindProperty("boardView").objectReferenceValue =
                boardView;
            controllerSerialized.FindProperty("boardCameraFitter").objectReferenceValue =
                fitter;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static void CompleteLoadedLevel(GameController controller)
        {
            MethodInfo complete = typeof(GameController).GetMethod(
                "CompleteLevel",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.That(complete, Is.Not.Null);
            complete.Invoke(controller, null);
        }

        private WorldDefinition World(string name)
        {
            WorldDefinition world =
                AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                    $"Assets/Resources/Worlds/{name}.asset"
                );
            Assert.That(world, Is.Not.Null);
            return world;
        }

        private GameObject Create(string name)
        {
            GameObject gameObject = new(name);
            created.Add(gameObject);
            return gameObject;
        }

        private GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            created.Add(gameObject);
            return gameObject;
        }

        private TMP_Text CreateText(string name, Transform parent)
        {
            GameObject gameObject = new(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );
            gameObject.transform.SetParent(parent, false);
            created.Add(gameObject);
            return gameObject.GetComponent<TMP_Text>();
        }

        private Button CreateButton(string name, Transform parent)
        {
            GameObject gameObject = new(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );
            gameObject.transform.SetParent(parent, false);
            created.Add(gameObject);
            return gameObject.GetComponent<Button>();
        }

        private static void SetField(
            object target,
            string fieldName,
            Object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }
    }
}
