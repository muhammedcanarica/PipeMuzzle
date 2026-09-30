using NUnit.Framework;
using System.Linq;
using System.Reflection;
using PipeMuzzle.Data;
using PipeMuzzle.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PipeMuzzle.Tests.EditMode
{
    public class ComicStoryDefinitionTests
    {
        private const string IntroKey = "PipeMuzzle.Story.World.SakuraGarden.Checkpoint.0.Viewed";
        private bool introExisted;
        private int introValue;

        [SetUp]
        public void IsolateIntroProgress()
        {
            introExisted = PlayerPrefs.HasKey(IntroKey);
            introValue = PlayerPrefs.GetInt(IntroKey);
            PlayerPrefs.DeleteKey(IntroKey);
        }

        [TearDown]
        public void RestoreIntroProgress()
        {
            if (introExisted) PlayerPrefs.SetInt(IntroKey, introValue);
            else PlayerPrefs.DeleteKey(IntroKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void ViewerFitsOneVisualWithoutAnOpaquePageFrame()
        {
            GameObject root = new("ComicViewer", typeof(RectTransform));
            try
            {
                ComicViewerUI viewer = root.AddComponent<ComicViewerUI>();
                typeof(ComicViewerUI).GetMethod("EnsureContent", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(viewer, null);
                ComicStoryDefinition story = AssetDatabase.LoadAssetAtPath<ComicStoryDefinition>(
                    "Assets/Data/Stories/SakuraStory.asset");
                viewer.Play(story);

                Image[] visuals = root.GetComponentsInChildren<Image>().Where(image => image.sprite != null).ToArray();
                Assert.That(visuals.Length, Is.EqualTo(1));
                Assert.That(visuals[0].sprite, Is.SameAs(story.Panels[0]));
                Assert.That(visuals[0].preserveAspect, Is.True);
                RectTransform frame = (RectTransform)visuals[0].transform.parent;
                Image page = frame.GetComponent<Image>();
                Assert.That(page == null || !page.enabled || page.color.a == 0f, Is.True,
                    "Unused image space must reveal the dark backdrop, not a white page.");
                Assert.That(frame.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(frame.anchorMax, Is.EqualTo(Vector2.one));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase("Sakura")]
        [TestCase("Bamboo")]
        [TestCase("Moon")]
        public void StorySpritesStayInsideTheirIntendedArtworkPanels(string world)
        {
            // Hand-checked artwork bounds in top-left pixel coordinates, in reading order.
            Rect[] bounds = world switch
            {
                "Sakura" => new[] {
                    new Rect(18, 12, 529, 451), new Rect(560, 13, 539, 450),
                    new Rect(20, 470, 525, 400), new Rect(553, 470, 544, 400),
                    new Rect(20, 877, 487, 495), new Rect(520, 877, 579, 495) },
                "Bamboo" => new[] {
                    new Rect(18, 11, 535, 444), new Rect(568, 11, 537, 444),
                    new Rect(19, 465, 532, 401), new Rect(566, 465, 536, 401),
                    new Rect(19, 875, 492, 505), new Rect(526, 875, 577, 505) },
                _ => new[] {
                    new Rect(9, 9, 530, 474), new Rect(547, 9, 529, 474),
                    new Rect(10, 487, 529, 464), new Rect(546, 488, 530, 463),
                    new Rect(10, 955, 527, 476), new Rect(546, 955, 530, 476) }
            };
            ComicStoryDefinition story = AssetDatabase.LoadAssetAtPath<ComicStoryDefinition>(
                $"Assets/Data/Stories/{world}Story.asset");
            for (int index = 0; index < bounds.Length; index++)
            {
                Sprite sprite = story.Panels[index];
                Rect rect = sprite.rect;
                Rect topLeftRect = new(rect.x, sprite.texture.height - rect.yMax, rect.width, rect.height);
                Assert.That(topLeftRect.xMin, Is.GreaterThanOrEqualTo(bounds[index].xMin), sprite.name);
                Assert.That(topLeftRect.yMin, Is.GreaterThanOrEqualTo(bounds[index].yMin), sprite.name);
                Assert.That(topLeftRect.xMax, Is.LessThanOrEqualTo(bounds[index].xMax), sprite.name);
                Assert.That(topLeftRect.yMax, Is.LessThanOrEqualTo(bounds[index].yMax), sprite.name);
                Assert.That(topLeftRect.Contains(bounds[index].center), Is.True, sprite.name);
                Assert.That(rect.width * rect.height, Is.GreaterThan(bounds[index].width * bounds[index].height * .95f),
                    "Keep the story artwork instead of cropping away its content.");
            }
        }

        [Test]
        public void EmptyStoryHasNoPanels()
        {
            ComicStoryDefinition story = ScriptableObject.CreateInstance<ComicStoryDefinition>();

            Assert.That(story.PanelCount, Is.Zero);
        }

        [Test]
        public void WorldDefinitionExposesItsComicStory()
        {
            WorldDefinition world = ScriptableObject.CreateInstance<WorldDefinition>();

            Assert.That(world.Story, Is.Null);
        }

        [Test]
        public void EmptyStoryCompletesImmediately()
        {
            GameObject viewerObject = new("ComicViewer");
            ComicViewerUI viewer = viewerObject.AddComponent<ComicViewerUI>();
            bool completed = false;
            viewer.StoryCompleted += () => completed = true;

            viewer.Play(null);

            Assert.That(completed, Is.True);
            Object.DestroyImmediate(viewerObject);
        }

        [Test]
        public void ComicScreenHidesEveryOtherMajorScreen()
        {
            GameObject managerObject = new("ScreenManager");
            ScreenManager manager = managerObject.AddComponent<ScreenManager>();
            GameObject worldMap = new("WorldMap");
            GameObject comic = new("Comic");
            GameObject levelSelect = new("LevelSelect");
            GameObject gameplay = new("Gameplay");

            manager.Configure(worldMap, comic, levelSelect, gameplay);
            manager.ShowComic();

            Assert.That(worldMap.activeSelf, Is.False);
            Assert.That(comic.activeSelf, Is.True);
            Assert.That(levelSelect.activeSelf, Is.False);
            Assert.That(gameplay.activeSelf, Is.False);

            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(worldMap);
            Object.DestroyImmediate(comic);
            Object.DestroyImmediate(levelSelect);
            Object.DestroyImmediate(gameplay);
        }

        [TestCase("SakuraStory", "Assets/Data/Stories/SakuraStory.asset", "Assets/Resources/Worlds/SakuraGarden.asset")]
        [TestCase("BambooStory", "Assets/Data/Stories/BambooStory.asset", "Assets/Resources/Worlds/BambooWorkshop.asset")]
        [TestCase("MoonStory", "Assets/Data/Stories/MoonStory.asset", "Assets/Resources/Worlds/MoonShrine.asset")]
        public void WorldAssetReferencesItsSixPanelComicStory(
            string expectedStoryName,
            string storyPath,
            string worldPath)
        {
            ComicStoryDefinition story = AssetDatabase.LoadAssetAtPath<ComicStoryDefinition>(storyPath);
            WorldDefinition world = AssetDatabase.LoadAssetAtPath<WorldDefinition>(worldPath);

            Assert.That(story, Is.Not.Null);
            Assert.That(story.name, Is.EqualTo(expectedStoryName));
            Assert.That(story.PanelCount, Is.EqualTo(6));
            Assert.That(world, Is.Not.Null);
            Assert.That(world.Story, Is.SameAs(story));
            Assert.That(world.LevelCount, Is.EqualTo(12));
        }

        [Test]
        public void StoryNavigatorWithoutLevelSelectKeepsComicOpenAfterSkip()
        {
            GameObject managerObject = new("ScreenManager");
            ScreenManager manager = managerObject.AddComponent<ScreenManager>();
            GameObject worldMap = new("WorldMap");
            GameObject comic = new("Comic");
            GameObject levelSelect = new("LevelSelect");
            GameObject gameplay = new("Gameplay");
            manager.Configure(worldMap, comic, levelSelect, gameplay);

            GameObject viewerObject = new("ComicViewer");
            ComicViewerUI viewer = viewerObject.AddComponent<ComicViewerUI>();
            GameObject navigatorObject = new("StoryNavigationCoordinator");
            StoryNavigationCoordinator navigator = navigatorObject.AddComponent<StoryNavigationCoordinator>();
            navigator.Configure(manager, viewer);

            WorldDefinition world = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/SakuraGarden.asset"
            );
            navigator.OpenWorld(world);

            Assert.That(comic.activeSelf, Is.True);
            LogAssert.Expect(LogType.Error,
                "StoryNavigationCoordinator cannot open level select without its selected world.");
            viewer.Skip();
            Assert.That(levelSelect.activeSelf, Is.False);
            Assert.That(comic.activeSelf, Is.True);

            Object.DestroyImmediate(navigatorObject);
            Object.DestroyImmediate(viewerObject);
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(worldMap);
            Object.DestroyImmediate(comic);
            Object.DestroyImmediate(levelSelect);
            Object.DestroyImmediate(gameplay);
        }
    }
}
