using System;
using System.Collections.Generic;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class ProgressServiceTests
    {
        private const string LegacyKey = "PipeMuzzle.HighestUnlockedLevel";
        private const string MarkerKey =
            "PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1";
        private const string SakuraKey =
            "PipeMuzzle.Progress.World.SakuraGarden.HighestUnlockedLevel";
        private const string BambooKey =
            "PipeMuzzle.Progress.World.BambooWorkshop.HighestUnlockedLevel";
        private const string MoonKey =
            "PipeMuzzle.Progress.World.MoonShrine.HighestUnlockedLevel";

        private static readonly string[] Keys =
            { LegacyKey, MarkerKey, SakuraKey, BambooKey, MoonKey };
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
        public void StableWorldSegmentsRejectUnknownIds()
        {
            Assert.That(WorldIdPersistence.Segment(WorldId.SakuraGarden),
                Is.EqualTo("SakuraGarden"));
            Assert.That(WorldIdPersistence.Segment(WorldId.BambooWorkshop),
                Is.EqualTo("BambooWorkshop"));
            Assert.That(WorldIdPersistence.Segment(WorldId.MoonShrine),
                Is.EqualTo("MoonShrine"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                WorldIdPersistence.Segment((WorldId)999));
        }

        [Test]
        public void UnlocksAreIndependentAndMonotonic()
        {
            ProgressService sakura = new(WorldId.SakuraGarden, 12);
            ProgressService bamboo = new(WorldId.BambooWorkshop, 12);
            ProgressService moon = new(WorldId.MoonShrine, 12);

            sakura.UnlockLevel(3);
            bamboo.UnlockLevel(4);
            bamboo.UnlockLevel(2);

            Assert.That(new ProgressService(WorldId.SakuraGarden, 12)
                .HighestUnlockedLevelIndex, Is.EqualTo(3));
            Assert.That(new ProgressService(WorldId.BambooWorkshop, 12)
                .HighestUnlockedLevelIndex, Is.EqualTo(4));
            Assert.That(moon.IsLevelUnlocked(1), Is.False);
            Assert.That(PlayerPrefs.HasKey(MoonKey), Is.False);
        }

        [Test]
        public void InvalidIndexesAndStoredValuesStayWithinActualContent()
        {
            PlayerPrefs.SetInt(BambooKey, 99);
            ProgressService bamboo = new(WorldId.BambooWorkshop, 12);

            Assert.That(bamboo.HighestUnlockedLevelIndex, Is.EqualTo(11));
            Assert.That(bamboo.IsLevelUnlocked(-1), Is.False);
            Assert.That(bamboo.IsLevelUnlocked(12), Is.False);
            bamboo.UnlockLevel(-1);
            bamboo.UnlockLevel(12);
            Assert.That(bamboo.HighestUnlockedLevelIndex, Is.EqualTo(11));

            ProgressService emptyMoon = new(WorldId.MoonShrine, 0);
            Assert.That(emptyMoon.HighestUnlockedLevelIndex, Is.EqualTo(-1));
            Assert.That(emptyMoon.IsLevelUnlocked(0), Is.False);
            emptyMoon.UnlockLevel(0);
            Assert.That(PlayerPrefs.HasKey(MoonKey), Is.False);
        }

        [Test]
        public void MissingLegacyKeyMarksMigrationWithoutCreatingProgress()
        {
            ProgressService sakura = new(WorldId.SakuraGarden, 12);

            Assert.That(sakura.HighestUnlockedLevelIndex, Is.Zero);
            Assert.That(PlayerPrefs.GetInt(MarkerKey), Is.EqualTo(1));
            Assert.That(PlayerPrefs.HasKey(SakuraKey), Is.False);
        }

        [Test]
        public void LegacyProgressMigratesOnlyToSakura()
        {
            PlayerPrefs.SetInt(LegacyKey, 7);
            Assert.That(new ProgressService(WorldId.BambooWorkshop, 12)
                .HighestUnlockedLevelIndex, Is.Zero);
            Assert.That(new ProgressService(WorldId.MoonShrine, 12)
                .HighestUnlockedLevelIndex, Is.Zero);
            Assert.That(PlayerPrefs.HasKey(MarkerKey), Is.False);

            Assert.That(new ProgressService(WorldId.SakuraGarden, 12)
                .HighestUnlockedLevelIndex, Is.EqualTo(7));
            Assert.That(PlayerPrefs.GetInt(SakuraKey), Is.EqualTo(7));
            Assert.That(PlayerPrefs.GetInt(MarkerKey), Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt(LegacyKey), Is.EqualTo(7));
            Assert.That(PlayerPrefs.HasKey(BambooKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(MoonKey), Is.False);
        }

        [TestCase(-9, 0)]
        [TestCase(99, 11)]
        public void LegacyProgressClampsToSakuraCount(int legacy, int expected)
        {
            PlayerPrefs.SetInt(LegacyKey, legacy);

            Assert.That(new ProgressService(WorldId.SakuraGarden, 12)
                .HighestUnlockedLevelIndex, Is.EqualTo(expected));
            Assert.That(PlayerPrefs.GetInt(SakuraKey), Is.EqualTo(expected));
        }

        [Test]
        public void ExistingSakuraProgressWinsOverOlderLegacyValue()
        {
            PlayerPrefs.SetInt(SakuraKey, 9);
            PlayerPrefs.SetInt(LegacyKey, 4);

            Assert.That(new ProgressService(WorldId.SakuraGarden, 12)
                .HighestUnlockedLevelIndex, Is.EqualTo(9));
            Assert.That(PlayerPrefs.GetInt(SakuraKey), Is.EqualTo(9));
        }

        [Test]
        public void MigrationIsIdempotent()
        {
            PlayerPrefs.SetInt(LegacyKey, 4);
            Assert.That(new ProgressService(WorldId.SakuraGarden, 12)
                .HighestUnlockedLevelIndex, Is.EqualTo(4));
            PlayerPrefs.SetInt(LegacyKey, 10);

            Assert.That(new ProgressService(WorldId.SakuraGarden, 12)
                .HighestUnlockedLevelIndex, Is.EqualTo(4));
            Assert.That(PlayerPrefs.GetInt(SakuraKey), Is.EqualTo(4));
        }
    }
}
