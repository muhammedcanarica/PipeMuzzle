using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class StoryCheckpointTests
    {
        private readonly Dictionary<string, (bool exists, int value)> saves = new();
        private static readonly int[] Triggers = { 0, 3, 6, 9, 12 };

        [SetUp]
        public void SetUp()
        {
            foreach (WorldId world in new[] { WorldId.SakuraGarden, WorldId.BambooWorkshop, WorldId.MoonShrine })
            foreach (int trigger in Triggers)
            {
                string key = $"PipeMuzzle.Story.World.{world}.Checkpoint.{trigger}.Viewed";
                saves[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
                PlayerPrefs.DeleteKey(key);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var entry in saves)
            {
                if (entry.Value.exists) PlayerPrefs.SetInt(entry.Key, entry.Value.value);
                else PlayerPrefs.DeleteKey(entry.Key);
            }
            PlayerPrefs.Save();
            saves.Clear();
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void EveryWorldHasFiveNonemptyStoriesWithUniquePanelsInReadingOrder(string name)
        {
            WorldDefinition world = World(name);
            Assert.That(world.StoryCheckpoints.Select(c => c.CompletedLevelNumber), Is.EqualTo(Triggers));
            Sprite[] panels = world.StoryCheckpoints.SelectMany(c => c.Story.Panels).ToArray();
            Assert.That(panels, Is.Not.Empty);
            Assert.That(panels.All(p => p != null), Is.True);
            Assert.That(panels.Distinct().Count(), Is.EqualTo(panels.Length));
            foreach (StoryCheckpoint checkpoint in world.StoryCheckpoints)
                Assert.That(checkpoint.Story.PanelCount, Is.GreaterThan(0));
        }

        [TestCase("SakuraGarden", 3)]
        [TestCase("SakuraGarden", 6)]
        [TestCase("SakuraGarden", 9)]
        [TestCase("SakuraGarden", 12)]
        [TestCase("BambooWorkshop", 3)]
        [TestCase("BambooWorkshop", 6)]
        [TestCase("BambooWorkshop", 9)]
        [TestCase("BambooWorkshop", 12)]
        [TestCase("MoonShrine", 3)]
        [TestCase("MoonShrine", 6)]
        [TestCase("MoonShrine", 9)]
        [TestCase("MoonShrine", 12)]
        public void FirstReachPlaysOnceAndPersistsAcrossServiceInstances(string name, int trigger)
        {
            WorldDefinition world = World(name);
            StoryCheckpointProgress progress = new();
            Assert.That(progress.TryBegin(world, trigger, out StoryCheckpoint checkpoint), Is.True);
            Assert.That(checkpoint, Is.SameAs(world.GetStoryCheckpoint(trigger)));
            Assert.That(new StoryCheckpointProgress().HasViewed(world.WorldId, trigger), Is.True);
            Assert.That(new StoryCheckpointProgress().TryBegin(world, trigger, out _), Is.False);
        }

        [TestCase("SakuraGarden")]
        [TestCase("BambooWorkshop")]
        [TestCase("MoonShrine")]
        public void IntroIsNotManagedAsPersistentCheckpoint(string name)
        {
            WorldDefinition world = World(name);
            string key = $"PipeMuzzle.Story.World.{name}.Checkpoint.0.Viewed";
            PlayerPrefs.SetInt(key, 1); // An older save may already contain this key.
            StoryCheckpointProgress progress = new();

            Assert.That(progress.HasViewed(world.WorldId, 0), Is.False);
            Assert.That(progress.TryBegin(world, 0, out _), Is.False);
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(1));
        }

        [Test]
        public void SakuraCheckpointDoesNotAffectOtherWorldsOrOtherCheckpoints()
        {
            StoryCheckpointProgress progress = new();
            Assert.That(progress.TryBegin(World("SakuraGarden"), 3, out _), Is.True);
            Assert.That(progress.HasViewed(WorldId.BambooWorkshop, 3), Is.False);
            Assert.That(progress.HasViewed(WorldId.MoonShrine, 3), Is.False);
            Assert.That(progress.HasViewed(WorldId.SakuraGarden, 6), Is.False);
            Assert.That(progress.TryBegin(World("BambooWorkshop"), 3, out _), Is.True);
            Assert.That(progress.TryBegin(World("MoonShrine"), 3, out _), Is.True);
        }

        [Test]
        public void NonCheckpointLevelsAndMissingWorldDoNotConsumeStoryState()
        {
            StoryCheckpointProgress progress = new();
            Assert.That(progress.TryBegin(null, 3, out _), Is.False);
            Assert.That(progress.TryBegin(World("SakuraGarden"), 4, out _), Is.False);
            Assert.That(progress.HasViewed(WorldId.SakuraGarden, 3), Is.False);
        }

        private static WorldDefinition World(string name) =>
            AssetDatabase.LoadAssetAtPath<WorldDefinition>($"Assets/Resources/Worlds/{name}.asset");
    }
}
