using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using PipeMuzzle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class WorldNavigationTests
    {
        private static readonly string[] Keys =
        {
            "PipeMuzzle.Progress.World.SakuraGarden.Unlocked",
            "PipeMuzzle.Progress.World.BambooWorkshop.Unlocked",
            "PipeMuzzle.Progress.World.MoonShrine.Unlocked",
            "PipeMuzzle.Progress.World.SakuraGarden.Completed",
            "PipeMuzzle.Progress.World.BambooWorkshop.Completed",
            "PipeMuzzle.Progress.World.MoonShrine.Completed",
            "PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1",
            "PipeMuzzle.Progress.World.SakuraGarden.HighestUnlockedLevel"
        };

        private readonly Dictionary<string, (bool exists, int value)> original = new();
        private readonly List<UnityEngine.Object> created = new();

        [SetUp]
        public void SetUp()
        {
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

            foreach (string key in Keys)
            {
                if (original[key].exists) PlayerPrefs.SetInt(key, original[key].value);
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            original.Clear();
        }

        [Test]
        public void MapRefreshShowsPlayableBambooWithoutRebuildingCards()
        {
            GameObject root = new("WorldMapTest", typeof(RectTransform));
            created.Add(root);
            WorldMapUI map = root.AddComponent<WorldMapUI>();
            map.Initialize();
            WorldDefinition selected = null;
            map.WorldSelected += world => selected = world;

            Button sakura = root.transform.Find("Sakura Garden").GetComponent<Button>();
            Button bamboo = root.transform.Find("Bamboo Workshop").GetComponent<Button>();
            Button moon = root.transform.Find("Moon Shrine").GetComponent<Button>();
            Assert.That(sakura.interactable, Is.True);
            Assert.That(bamboo.interactable, Is.False);
            Assert.That(moon.interactable, Is.False);
            Assert.That(Status(moon), Is.EqualTo("LOCKED"));
            sakura.onClick.Invoke();
            Assert.That(selected, Is.SameAs(Sakura()));

            new WorldProgressService().MarkWorldCompleted(WorldId.SakuraGarden);
            int childCount = root.transform.childCount;
            map.Refresh();

            Assert.That(root.transform.childCount, Is.EqualTo(childCount));
            Assert.That(root.transform.Find("Bamboo Workshop").GetComponent<Button>(), Is.SameAs(bamboo));
            Assert.That(bamboo.interactable, Is.True);
            Assert.That(Status(bamboo), Is.EqualTo("12 LEVELS"));
            Image bambooSurface = bamboo.GetComponent<Image>();
            Assert.That(bambooSurface.sprite, Is.Not.Null);
            Assert.That(bambooSurface.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(bamboo.GetComponent<Shadow>(), Is.Not.Null);
            Assert.That(bamboo.transform.Find("AccentBand"), Is.Not.Null);
            Assert.That(bamboo.transform.Find("AccentBand").GetComponent<Image>().color,
                Is.EqualTo((Color)new Color32(103, 143, 86, 255)));
            Assert.That(Status(moon), Is.EqualTo("LOCKED"));

            bamboo.onClick.Invoke();
            Assert.That(selected, Is.SameAs(Bamboo()));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1080f, 1920f)]
        [TestCase(2560f, 1080f)]
        public void MapPresentationRestoresRoundedCardsAndSimpleRoutes(float width, float height)
        {
            GameObject root = new("WorldMapLayoutTest", typeof(RectTransform));
            created.Add(root);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(width, height);
            WorldMapUI map = root.AddComponent<WorldMapUI>();
            map.Initialize();
            Image background = root.transform.Find("Background").GetComponent<Image>();
            Assert.That(background.sprite, Is.Null);
            Assert.That(background.color, Is.EqualTo((Color)new Color32(255, 242, 237, 255)));
            Assert.That(root.transform.Find("SoftBackdrop"), Is.Null);
            Assert.That(root.transform.Find("MapSafeArea"), Is.Null);
            Button[] destinations = root.GetComponentsInChildren<Button>(true);
            Assert.That(destinations.Length, Is.EqualTo(3));
            List<Rect> visibleBounds = new();
            float[] positions = { -265f, 0f, 265f };
            for (int index = 0; index < destinations.Length; index++)
            {
                Button destination = destinations[index];
                RectTransform element = (RectTransform)destination.transform;
                Assert.That(element.parent, Is.SameAs(root.transform));
                Assert.That(element.anchoredPosition, Is.EqualTo(new Vector2(0f, positions[index])));
                Assert.That(element.sizeDelta, Is.EqualTo(new Vector2(610f, 154f)));
                Image surface = destination.GetComponent<Image>();
                Assert.That(surface.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(surface.sprite.name, Is.EqualTo("World Map Rounded Card"));
                Assert.That(destination.transform.Find("AccentBand"), Is.Not.Null);
                Assert.That(destination.transform.Find("StatusSurface"), Is.Not.Null);
                Assert.That(destination.transform.Find("WorldLabelBubble"), Is.Null);
                Vector3[] corners = new Vector3[4];
                element.GetWorldCorners(corners);
                Vector2 min = rootRect.InverseTransformPoint(corners[0]);
                Vector2 max = rootRect.InverseTransformPoint(corners[2]);
                Rect bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                Assert.That(rootRect.rect.Contains(min), Is.True, element.name + " leaves screen");
                Assert.That(rootRect.rect.Contains(max), Is.True, element.name + " leaves screen");
                foreach (Rect other in visibleBounds)
                    Assert.That(bounds.Overlaps(other), Is.False, element.name + " overlaps another card");
                visibleBounds.Add(bounds);
            }
            Assert.That(root.GetComponentsInChildren<Image>(true)
                .Count(image => image.name == "JourneyPath"), Is.EqualTo(2));
            Assert.That(root.GetComponentsInChildren<Canvas>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<TMP_Text>(true).All(text => !text.raycastTarget), Is.True);
            rootRect.sizeDelta = new Vector2(height, width);
            map.Refresh();
            Assert.That(root.GetComponentsInChildren<Button>(true), Is.EquivalentTo(destinations));
        }

        [Test]
        public void DuplicateAndUnknownWorldAssetsAreSkipped()
        {
            WorldDefinition unknown = ScriptableObject.CreateInstance<WorldDefinition>();
            created.Add(unknown);
            typeof(WorldDefinition).GetField("worldId",
                BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(unknown, (WorldId)999);
            WorldDefinition duplicate = ScriptableObject.CreateInstance<WorldDefinition>();
            created.Add(duplicate);
            MethodInfo validate = typeof(WorldMapUI).GetMethod("ValidateWorlds",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(validate, Is.Not.Null);

            LogAssert.Expect(LogType.Error, "WorldMapUI skipped a duplicate WorldId: SakuraGarden.");
            LogAssert.Expect(LogType.Error, "WorldMapUI skipped an unknown WorldId: 999.");
            LogAssert.Expect(LogType.Error, "WorldMapUI has no valid asset for WorldId: SakuraGarden.");
            LogAssert.Expect(LogType.Error, "WorldMapUI has no valid asset for WorldId: BambooWorkshop.");
            LogAssert.Expect(LogType.Error, "WorldMapUI has no valid asset for WorldId: MoonShrine.");
            var result = (IEnumerable<WorldDefinition>)validate.Invoke(null,
                new object[] { new[] { Sakura(), duplicate, unknown } });
            Assert.That(result, Is.Empty);

            LogAssert.Expect(LogType.Error, "WorldMapUI skipped a duplicate WorldId: SakuraGarden.");
            LogAssert.Expect(LogType.Error, "WorldMapUI has no valid asset for WorldId: SakuraGarden.");
            LogAssert.Expect(LogType.Error, "WorldMapUI has no valid asset for WorldId: BambooWorkshop.");
            LogAssert.Expect(LogType.Error, "WorldMapUI has no valid asset for WorldId: MoonShrine.");
            result = (IEnumerable<WorldDefinition>)validate.Invoke(null,
                new object[] { new[] { duplicate, Sakura() } });
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void LevelSelectConfiguresControllerBeforeButtonsAndRejectsUnavailableWorld()
        {
            GameObject controllerObject = new("Controller");
            created.Add(controllerObject);
            GameController controller = controllerObject.AddComponent<GameController>();

            GameObject uiObject = new("LevelSelect");
            uiObject.SetActive(false);
            created.Add(uiObject);
            LevelSelectUI select = uiObject.AddComponent<LevelSelectUI>();
            GameObject panel = new("Panel");
            panel.transform.SetParent(uiObject.transform);
            List<Button> buttons = new();
            for (int i = 0; i < 13; i++)
            {
                GameObject buttonObject = new($"Level{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(panel.transform);
                buttons.Add(buttonObject.GetComponent<Button>());
            }
            SerializedObject serialized = new(select);
            serialized.FindProperty("gameController").objectReferenceValue = controller;
            serialized.FindProperty("levelSelectPanel").objectReferenceValue = panel;
            SerializedProperty list = serialized.FindProperty("levelButtons");
            list.arraySize = buttons.Count;
            for (int i = 0; i < buttons.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            LogAssert.Expect(LogType.Warning, "LevelSelectUI has no Path Area; preserving existing node positions.");
            select.ConfigureForWorld(Sakura());
            Assert.That(controller.CurrentWorld, Is.SameAs(Sakura()));
            Assert.That(select.CurrentWorld, Is.SameAs(Sakura()));
            Assert.That(buttons[0].interactable, Is.True);
            Assert.That(buttons[12].interactable, Is.False);

            WorldDefinition noTheme = ScriptableObject.CreateInstance<WorldDefinition>();
            created.Add(noTheme);
            SerializedObject noThemeSerialized = new(noTheme);
            SerializedProperty noThemeLevels = noThemeSerialized.FindProperty("levels");
            noThemeLevels.arraySize = 12;
            for (int i = 0; i < 12; i++)
                noThemeLevels.GetArrayElementAtIndex(i).objectReferenceValue = Sakura().Levels[i];
            noThemeSerialized.ApplyModifiedPropertiesWithoutUndo();
            LogAssert.Expect(LogType.Error, "World '' has no level select theme.");
            LogAssert.Expect(LogType.Error, "GameController cannot configure an unavailable world.");
            select.ConfigureForWorld(noTheme);
            Assert.That(select.CurrentWorld, Is.Null);
            Assert.That(controller.CurrentWorld, Is.Null);
            Assert.That(buttons.All(button => !button.interactable), Is.True);

            LogAssert.Expect(LogType.Error, "GameController cannot configure an unavailable world.");
            select.ConfigureForWorld(Bamboo());
            Assert.That(select.CurrentWorld, Is.Null);
            Assert.That(controller.CurrentWorld, Is.Null);
            Assert.That(buttons.All(button => !button.interactable), Is.True);
        }

        [Test]
        public void MissingPresentationAssetsCannotOpenComic()
        {
            GameObject root = new("Navigation");
            root.SetActive(false);
            created.Add(root);
            ScreenManager screens = root.AddComponent<ScreenManager>();
            StoryNavigationCoordinator navigation = root.AddComponent<StoryNavigationCoordinator>();
            GameObject comic = new("Comic", typeof(RectTransform));
            comic.transform.SetParent(root.transform);
            comic.SetActive(false);
            ComicViewerUI viewer = comic.AddComponent<ComicViewerUI>();
            screens.Configure(null, comic, null, null);
            navigation.Configure(screens, viewer);

            WorldDefinition noPresentation = ScriptableObject.CreateInstance<WorldDefinition>();
            created.Add(noPresentation);
            SerializedObject serialized = new(noPresentation);
            SerializedProperty levels = serialized.FindProperty("levels");
            levels.arraySize = 12;
            for (int i = 0; i < 12; i++)
                levels.GetArrayElementAtIndex(i).objectReferenceValue = Sakura().Levels[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(noPresentation.IsContentReady, Is.True);

            LogAssert.Expect(LogType.Error,
                "StoryNavigationCoordinator cannot open a world with missing presentation assets.");
            navigation.OpenWorld(noPresentation);
            Assert.That(comic.activeSelf, Is.False);
        }

        private static string Status(Button button) =>
            button.GetComponentsInChildren<TMP_Text>(true).Last().text;

        private static WorldDefinition Sakura() => AssetDatabase.LoadAssetAtPath<WorldDefinition>(
            "Assets/Resources/Worlds/SakuraGarden.asset");

        private static WorldDefinition Bamboo() => AssetDatabase.LoadAssetAtPath<WorldDefinition>(
            "Assets/Resources/Worlds/BambooWorkshop.asset");

        private static WorldDefinition Moon() => AssetDatabase.LoadAssetAtPath<WorldDefinition>(
            "Assets/Resources/Worlds/MoonShrine.asset");


    }
}
