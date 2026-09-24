using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PipeMuzzle.Tests.EditMode
{
    public class ComicStoryDefinitionTests
    {
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
            int expectedActualLevels = world.WorldId == WorldId.SakuraGarden ? 12 : 0;
            Assert.That(world.LevelCount, Is.EqualTo(expectedActualLevels));
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
