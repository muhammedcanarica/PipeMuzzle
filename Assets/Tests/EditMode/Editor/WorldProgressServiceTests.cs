using System;
using System.Collections.Generic;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class WorldProgressServiceTests
    {
        private static readonly string[] Keys =
        {
            "PipeMuzzle.Progress.World.SakuraGarden.Unlocked",
            "PipeMuzzle.Progress.World.BambooWorkshop.Unlocked",
            "PipeMuzzle.Progress.World.MoonShrine.Unlocked",
            "PipeMuzzle.Progress.World.SakuraGarden.Completed",
            "PipeMuzzle.Progress.World.BambooWorkshop.Completed",
            "PipeMuzzle.Progress.World.MoonShrine.Completed"
        };

        private readonly Dictionary<string, (bool exists, int value)> original = new();

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
            foreach (string key in Keys)
            {
                if (original[key].exists) PlayerPrefs.SetInt(key, original[key].value);
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            original.Clear();
        }

        [Test]
        public void FirstLaunchUnlocksOnlySakuraAndPersistsIt()
        {
            WorldProgressService progress = new();

            Assert.That(progress.IsWorldUnlocked(WorldId.SakuraGarden), Is.True);
            Assert.That(progress.IsWorldUnlocked(WorldId.BambooWorkshop), Is.False);
            Assert.That(progress.IsWorldUnlocked(WorldId.MoonShrine), Is.False);
            Assert.That(PlayerPrefs.GetInt(Keys[0]), Is.EqualTo(1));
            Assert.That(PlayerPrefs.HasKey(Keys[1]), Is.False);
            Assert.That(PlayerPrefs.HasKey(Keys[2]), Is.False);
        }

        [Test]
        public void FinalWorldCompletionUnlocksOnlyExplicitNextWorld()
        {
            WorldProgressService progress = new();
            progress.MarkWorldCompleted(WorldId.SakuraGarden);
            progress.MarkWorldCompleted(WorldId.SakuraGarden);

            Assert.That(progress.IsWorldCompleted(WorldId.SakuraGarden), Is.True);
            Assert.That(new WorldProgressService().IsWorldUnlocked(
                WorldId.BambooWorkshop), Is.True);
            Assert.That(progress.IsWorldUnlocked(WorldId.MoonShrine), Is.False);

            progress.MarkWorldCompleted(WorldId.BambooWorkshop);
            Assert.That(new WorldProgressService().IsWorldUnlocked(
                WorldId.MoonShrine), Is.True);

            progress.MarkWorldCompleted(WorldId.MoonShrine);
            Assert.That(new WorldProgressService().IsWorldCompleted(
                WorldId.MoonShrine), Is.True);
        }

        [Test]
        public void LockedWorldCannotCompleteOutOfOrder()
        {
            WorldProgressService progress = new();

            progress.MarkWorldCompleted(WorldId.BambooWorkshop);

            Assert.That(progress.IsWorldCompleted(WorldId.BambooWorkshop), Is.False);
            Assert.That(progress.IsWorldUnlocked(WorldId.MoonShrine), Is.False);
        }

        [Test]
        public void UnknownWorldIdentityNeverUsesAnotherWorldKey()
        {
            WorldProgressService progress = new();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                progress.IsWorldUnlocked((WorldId)999));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                progress.MarkWorldCompleted((WorldId)999));
            Assert.That(progress.IsWorldUnlocked(WorldId.BambooWorkshop), Is.False);
        }

        [Test]
        public void AccessStateSeparatesUnlockFromContentReadiness()
        {
            WorldProgressService progress = new();
            WorldDefinition sakura = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/SakuraGarden.asset");
            WorldDefinition bamboo = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/BambooWorkshop.asset");
            WorldDefinition moon = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/MoonShrine.asset");
            WorldDefinition completeBamboo = UnityEngine.Object.Instantiate(sakura);

            try
            {
                SerializedObject serialized = new(completeBamboo);
                serialized.FindProperty("worldId").enumValueIndex =
                    (int)WorldId.BambooWorkshop;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(progress.GetAccessState(null),
                    Is.EqualTo(WorldAccessState.Locked));
                Assert.That(progress.GetAccessState(completeBamboo),
                    Is.EqualTo(WorldAccessState.Locked));
                Assert.That(progress.GetAccessState(sakura),
                    Is.EqualTo(WorldAccessState.Playable));

                progress.MarkWorldCompleted(WorldId.SakuraGarden);

                Assert.That(progress.GetAccessState(bamboo),
                    Is.EqualTo(WorldAccessState.Playable));
                Assert.That(progress.GetAccessState(completeBamboo),
                    Is.EqualTo(WorldAccessState.Playable));
                Assert.That(progress.GetAccessState(sakura),
                    Is.EqualTo(WorldAccessState.Playable));

                progress.MarkWorldCompleted(WorldId.BambooWorkshop);

                Assert.That(progress.GetAccessState(moon),
                    Is.EqualTo(WorldAccessState.Playable));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(completeBamboo);
            }
        }
    }
}
