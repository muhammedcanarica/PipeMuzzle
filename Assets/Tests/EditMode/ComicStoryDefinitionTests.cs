using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.UI;
using UnityEngine;

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
    }
}
