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
using UnityEngine.EventSystems;
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
        public void MapRefreshShowsCompletionAndPlayableBambooWithoutRebuildingDestinations()
        {
            GameObject root = new("WorldMapTest", typeof(RectTransform));
            created.Add(root);
            WorldMapUI map = root.AddComponent<WorldMapUI>();
            map.Initialize();
            WorldDefinition selected = null;
            map.WorldSelected += world => selected = world;

            Button sakura = DestinationButton(root.transform, "Sakura Garden");
            Button bamboo = DestinationButton(root.transform, "Bamboo Workshop");
            Button moon = DestinationButton(root.transform, "Moon Shrine");
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
            Assert.That(DestinationButton(root.transform, "Bamboo Workshop"), Is.SameAs(bamboo));
            Assert.That(bamboo.interactable, Is.True);
            Assert.That(Status(bamboo), Is.EqualTo("12 LEVELS"));
            Assert.That(Status(sakura), Is.EqualTo("COMPLETE"));
            Image bambooArtwork = bamboo.transform.parent.Find("Artwork").GetComponent<Image>();
            Assert.That(bambooArtwork.sprite, Is.SameAs(Resources.Load<Sprite>("WorldMap/Journey/BambooWorkshop")));
            Assert.That(bambooArtwork.color, Is.EqualTo(Color.white));
            Assert.That(Status(moon), Is.EqualTo("LOCKED"));

            bamboo.onClick.Invoke();
            Assert.That(selected, Is.SameAs(Bamboo()));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1080f, 1920f)]
        [TestCase(2560f, 1080f)]
        [TestCase(1440f, 1080f)]
        [TestCase(800f, 600f)]
        public void IllustratedMapKeepsArtAndLabelsOnScreenWithoutOverlapping(float width, float height)
        {
            GameObject root = new("WorldMapLayoutTest", typeof(RectTransform));
            created.Add(root);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(width, height);
            WorldMapUI map = root.AddComponent<WorldMapUI>();
            map.Initialize();
            Image background = root.transform.Find("Background").GetComponent<Image>();
            Assert.That(background.color.r, Is.GreaterThan(.9f));
            Assert.That(background.color.g, Is.GreaterThan(.85f));
            Button[] destinations = root.GetComponentsInChildren<Button>(true);
            Assert.That(destinations.Length, Is.EqualTo(3));
            List<Rect> visibleBounds = new();
            WorldId[] order = { WorldId.SakuraGarden, WorldId.BambooWorkshop, WorldId.MoonShrine };
            for (int index = 0; index < destinations.Length; index++)
            {
                Button destination = destinations[index];
                RectTransform element = (RectTransform)destination.transform.parent;
                Assert.That(element.parent, Is.SameAs(root.transform));
                Image surface = element.GetComponent<Image>();
                Assert.That(surface, Is.Null, "Region has no card surface or selection button.");
                Image artwork = element.Find("Artwork").GetComponent<Image>();
                Assert.That(artwork.sprite, Is.SameAs(Resources.Load<Sprite>($"WorldMap/Journey/{order[index]}")));
                Assert.That(artwork.preserveAspect, Is.True);
                Assert.That(artwork.raycastTarget, Is.False);
                Assert.That(destination.GetComponent<Outline>(), Is.Null);
                Assert.That(element.Find("AccentBand"), Is.Null);
                Assert.That(element.Find("StatusSurface"), Is.Null, "No card-style status badge.");
                Assert.That(element.Find("WorldNode"), Is.Not.Null);
                Vector3[] corners = new Vector3[4];
                element.GetWorldCorners(corners);
                Vector2 min = rootRect.InverseTransformPoint(corners[0]);
                Vector2 max = rootRect.InverseTransformPoint(corners[2]);
                Rect bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                Assert.That(rootRect.rect.Contains(min), Is.True, element.name + " leaves screen");
                Assert.That(rootRect.rect.Contains(max), Is.True, element.name + " leaves screen");
                foreach (Rect other in visibleBounds)
                    Assert.That(bounds.Overlaps(other), Is.False, element.name + " overlaps another destination");
                visibleBounds.Add(bounds);
                foreach (RectTransform child in element.GetComponentsInChildren<RectTransform>(true))
                {
                    child.GetWorldCorners(corners);
                    Assert.That(rootRect.rect.Contains(rootRect.InverseTransformPoint(corners[0])), Is.True, child.name);
                    Assert.That(rootRect.rect.Contains(rootRect.InverseTransformPoint(corners[2])), Is.True, child.name);
                }
            }
            Assert.That(root.GetComponentsInChildren<Image>(true)
                .Count(image => image.name == "JourneyPath"), Is.GreaterThan(2));
            RectTransform heading = root.transform.Find("BrandTitle").GetComponent<RectTransform>();
            Vector3[] headingCorners = new Vector3[4];
            heading.GetWorldCorners(headingCorners);
            float headingBottom = rootRect.InverseTransformPoint(headingCorners[0]).y;
            Assert.That(visibleBounds.All(bounds => bounds.yMax < headingBottom), Is.True);
            Assert.That(root.GetComponentsInChildren<Canvas>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<TMP_Text>(true).All(text => !text.raycastTarget), Is.True);
            rootRect.sizeDelta = new Vector2(height, width);
            map.Refresh();
            Assert.That(root.GetComponentsInChildren<Button>(true), Is.EquivalentTo(destinations));
        }

        [Test]
        public void LockedWorldRemainsVisibleAndCannotDispatchSelection()
        {
            GameObject root = new("MapStateTest", typeof(RectTransform));
            created.Add(root);
            WorldMapUI map = root.AddComponent<WorldMapUI>();
            map.Initialize();
            WorldDefinition selected = null;
            map.WorldSelected += world => selected = world;
            Button moon = DestinationButton(root.transform, "Moon Shrine");
            moon.onClick.Invoke();
            Assert.That(selected, Is.Null);
            Image art = moon.transform.parent.Find("Artwork").GetComponent<Image>();
            Assert.That(art.enabled, Is.True);
            Assert.That(art.color.a, Is.InRange(.6f, .95f));
            Assert.That(art.material.GetFloat("_Saturation"), Is.LessThan(1f));
            new WorldProgressService().MarkWorldCompleted(WorldId.SakuraGarden);
            new WorldProgressService().MarkWorldCompleted(WorldId.BambooWorkshop);
            map.Refresh();
            Assert.That(moon.interactable, Is.True);
            Assert.That(art.color, Is.EqualTo(Color.white));
            moon.onClick.Invoke();
            Assert.That(selected, Is.SameAs(Moon()));
            Assert.That(Status(moon), Is.EqualTo("12 LEVELS"));
        }

        [TestCase("Sakura Garden", true)]
        [TestCase("Moon Shrine", false)]
        public void HoverAndPressStaySubtleAndOnlyAnimatePlayableDestinations(string name, bool playable)
        {
            GameObject root = new("MapInteractionTest", typeof(RectTransform));
            created.Add(root);
            root.AddComponent<WorldMapUI>().Initialize();
            GameObject eventObject = new("MapEventSystem", typeof(EventSystem));
            created.Add(eventObject);
            PointerEventData pointer = new(eventObject.GetComponent<EventSystem>());
            WorldMapDestinationButton button = DestinationButton(root.transform, name);
            button.OnPointerEnter(pointer);
            Assert.That(button.transform.parent.localScale.x, Is.EqualTo(1f));
            Transform node = button.transform;
            Assert.That(node.localScale.x, Is.EqualTo(playable ? 1.03f : 1f).Within(.001f));
            button.OnPointerDown(pointer);
            Assert.That(node.localScale.x, Is.EqualTo(playable ? .98f : 1f).Within(.001f));
            button.OnPointerUp(pointer);
            button.OnPointerExit(pointer);
            eventObject.GetComponent<EventSystem>().SetSelectedGameObject(null);
            Assert.That(button.transform.parent.localScale.x, Is.EqualTo(1f).Within(.001f));
            Assert.That(node.localScale.x, Is.EqualTo(1f).Within(.001f));
        }

        [Test]
        public void MarkerStartsAtSakuraIgnoresLockedAndFollowsUnlockedSelection()
        {
            GameObject root = new("MapMarkerTest", typeof(RectTransform));
            created.Add(root);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
            WorldMapUI map = root.AddComponent<WorldMapUI>();
            map.Initialize();
            RectTransform marker = (RectTransform)root.transform.Find("CharacterMarker");
            Vector2 start = marker.anchoredPosition;
            RectTransform sakura = (RectTransform)root.transform.Find("Sakura Garden");
            Assert.That(marker.anchoredPosition.x, Is.EqualTo(sakura.anchoredPosition.x +
                ((RectTransform)sakura.Find("WorldNode")).anchoredPosition.x));
            WorldDefinition selected = null;
            map.WorldSelected += world => selected = world;
            Button bamboo = DestinationButton(root.transform, "Bamboo Workshop");
            bamboo.onClick.Invoke();
            Assert.That(marker.anchoredPosition, Is.EqualTo(start));
            Assert.That(selected, Is.Null);

            new WorldProgressService().MarkWorldCompleted(WorldId.SakuraGarden);
            map.Refresh();
            Assert.That(marker.anchoredPosition, Is.EqualTo(start), "Unlocking alone does not move the player.");
            bamboo.onClick.Invoke();
            Assert.That(selected, Is.SameAs(Bamboo()));
            RectTransform bambooRect = (RectTransform)bamboo.transform.parent;
            Vector2 end = marker.anchoredPosition;
            Assert.That(end.x, Is.EqualTo(bambooRect.anchoredPosition.x +
                ((RectTransform)bambooRect.Find("WorldNode")).anchoredPosition.x));
            Assert.That(end, Is.Not.EqualTo(start));
            map.Refresh();
            Assert.That(marker.anchoredPosition, Is.EqualTo(end));
            Assert.That(marker.GetComponent<Image>().raycastTarget, Is.False);
            Assert.That(marker.GetComponent<Image>().sprite, Is.SameAs(Resources.Load<Sprite>("WorldMap/Journey/ChibiTraveler")));
            Assert.That(marker.Find("SilhouetteHead").gameObject.activeSelf, Is.False);
            Sprite replacement = Resources.Load<Sprite>("WorldMap/FinalSakuraGarden");
            map.SetMarkerSprite(replacement);
            Assert.That(marker.GetComponent<Image>().sprite, Is.SameAs(replacement));
            Assert.That(marker.Find("SilhouetteHead").gameObject.activeSelf, Is.False);
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

        [TestCase("Sakura Garden", WorldId.SakuraGarden)]
        [TestCase("Bamboo Workshop", WorldId.BambooWorkshop)]
        [TestCase("Moon Shrine", WorldId.MoonShrine)]
        public void DestinationSelectionDispatchesWorldAndOpensItsIntro(string destinationName, WorldId id)
        {
            PlayerPrefs.SetInt($"PipeMuzzle.Progress.World.{id}.Unlocked", 1);
            GameObject root = new("MapIntroNavigationTest", typeof(RectTransform));
            root.SetActive(false);
            created.Add(root);
            ScreenManager screens = root.AddComponent<ScreenManager>();
            StoryNavigationCoordinator navigation = root.AddComponent<StoryNavigationCoordinator>();
            GameObject mapObject = new("Map", typeof(RectTransform));
            mapObject.transform.SetParent(root.transform);
            WorldMapUI map = mapObject.AddComponent<WorldMapUI>();
            GameObject comic = new("Comic", typeof(RectTransform));
            comic.transform.SetParent(root.transform);
            comic.SetActive(false);
            ComicViewerUI viewer = comic.AddComponent<ComicViewerUI>();
            GameObject imageObject = new("Panel", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(comic.transform);
            Image image = imageObject.GetComponent<Image>();
            viewer.Configure(image, null, null, null, null, null);
            screens.Configure(mapObject, comic, null, null);
            navigation.Configure(screens, viewer);
            navigation.BindWorldMap(map);
            map.Initialize();
            WorldDefinition selected = null;
            map.WorldSelected += world => selected = world;

            DestinationButton(mapObject.transform, destinationName).onClick.Invoke();

            WorldDefinition expected = Resources.Load<WorldDefinition>($"Worlds/{id}");
            Assert.That(selected, Is.SameAs(expected));
            Assert.That(mapObject.activeSelf, Is.False);
            Assert.That(comic.activeSelf, Is.True);
            Assert.That(image.sprite, Is.SameAs(expected.GetStoryCheckpoint(0).Story.Panels[0]));
            Assert.That(typeof(ComicViewerUI).GetField("currentStory", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(viewer), Is.SameAs(expected.GetStoryCheckpoint(0).Story));
        }

        private static string Status(Button button) =>
            button.transform.parent.Find("Status").GetComponent<TMP_Text>().text;

        private static WorldMapDestinationButton DestinationButton(Transform root, string name) =>
            root.Find(name).Find("WorldNode").GetComponent<WorldMapDestinationButton>();

        private static WorldDefinition Sakura() => AssetDatabase.LoadAssetAtPath<WorldDefinition>(
            "Assets/Resources/Worlds/SakuraGarden.asset");

        private static WorldDefinition Bamboo() => AssetDatabase.LoadAssetAtPath<WorldDefinition>(
            "Assets/Resources/Worlds/BambooWorkshop.asset");

        private static WorldDefinition Moon() => AssetDatabase.LoadAssetAtPath<WorldDefinition>(
            "Assets/Resources/Worlds/MoonShrine.asset");


    }
}
