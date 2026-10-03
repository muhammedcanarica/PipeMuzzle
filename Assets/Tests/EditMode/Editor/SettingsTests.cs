using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.Feedback;
using PipeMuzzle.Gameplay;
using PipeMuzzle.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class SettingsTests
    {
        private const string SoundKey = "PipeMuzzle.Feedback.SoundEnabled";
        private const string HapticsKey = "PipeMuzzle.Feedback.HapticsEnabled";
        private const string OtherKey = "SettingsTests.OtherSystem";
        private readonly Dictionary<string, (bool exists, int value)> saved = new();
        private GameObject root, map, settingsPanel, comic, levels, hud;
        private ScreenManager screens;
        private SettingsUI settings;
        private GameController controller;

        [SetUp]
        public void SetUp()
        {
            foreach (string key in Keys())
            {
                saved[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
                PlayerPrefs.DeleteKey(key);
            }
            ResetFeedbackCache();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            foreach (var item in saved)
            {
                if (item.Value.exists) PlayerPrefs.SetInt(item.Key, item.Value.value);
                else PlayerPrefs.DeleteKey(item.Key);
            }
            PlayerPrefs.Save();
            saved.Clear();
            ResetFeedbackCache();
        }

        [Test]
        public void MapSettingsEntryOpensSettingsAndBackRestoresOnlyMap()
        {
            BuildScreens();
            Button entry = map.transform.Find("SettingsEntry/SettingsButton").GetComponent<Button>();
            entry.onClick.Invoke();
            AssertOnlySettings();
            Button("BackButton").onClick.Invoke();
            Assert.That(map.activeSelf, Is.True);
            Assert.That(settingsPanel.activeSelf, Is.False);
            Assert.That(comic.activeSelf || levels.activeSelf || hud.activeSelf, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SoundButtonChangesAndReloadsExistingFeedbackPreference(bool initial)
        {
            GameFeedback.SetSoundEnabled(initial);
            BuildScreens();
            OpenSettings();
            Assert.That(Label("SoundButton").text, Is.EqualTo(initial ? "ON" : "OFF"));
            Button("SoundButton").onClick.Invoke();
            Assert.That(GameFeedback.SoundEnabled, Is.EqualTo(!initial));
            Assert.That(new FeedbackSettings().SoundEnabled, Is.EqualTo(!initial));
            Assert.That(Label("SoundButton").text, Is.EqualTo(initial ? "OFF" : "ON"));
            ResetFeedbackCache();
            Assert.That(GameFeedback.SoundEnabled, Is.EqualTo(!initial));
        }

        [Test]
        public void DefaultSoundIsOnWithoutCreatingPreference()
        {
            BuildScreens();
            OpenSettings();
            Assert.That(Label("SoundButton").text, Is.EqualTo("ON"));
            Assert.That(PlayerPrefs.HasKey(SoundKey), Is.False);
        }

        [Test]
        public void CancelAndClosingConfirmationPreserveAllSavedProgress()
        {
            SeedProgress();
            BuildScreens();
            OpenSettings();
            Button("ResetProgressButton").onClick.Invoke();
            Assert.That(Confirmation().activeSelf, Is.True);
            Confirmation().transform.Find("CancelButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(Confirmation().activeSelf, Is.False);
            AssertSeededProgress();
            Button("ResetProgressButton").onClick.Invoke();
            screens.ShowWorldMap();
            OpenSettings();
            Assert.That(Confirmation().activeSelf, Is.False);
            AssertSeededProgress();
        }

        [Test]
        public void ResetWithoutConfirmationDoesNotDeleteProgress()
        {
            SeedProgress();
            BuildScreens();
            OpenSettings();
            settings.ConfirmReset();
            AssertSeededProgress();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConfirmedResetClearsProgressPreservesPreferencesAndRefreshesMap(bool sound)
        {
            SeedProgress();
            GameFeedback.SetSoundEnabled(sound);
            GameFeedback.SetHapticsEnabled(false);
            PlayerPrefs.SetInt(OtherKey, 73);
            BuildScreens();
            controller.ConfigureWorld(Resources.Load<WorldDefinition>("Worlds/MoonShrine"));
            OpenSettings();
            Button("ResetProgressButton").onClick.Invoke();
            Confirmation().transform.Find("ConfirmResetButton").GetComponent<Button>().onClick.Invoke();

            WorldProgressService progress = new();
            Assert.That(progress.IsWorldUnlocked(WorldId.SakuraGarden), Is.True);
            Assert.That(progress.IsWorldUnlocked(WorldId.BambooWorkshop), Is.False);
            Assert.That(progress.IsWorldUnlocked(WorldId.MoonShrine), Is.False);
            foreach (WorldId world in Worlds())
            {
                Assert.That(progress.IsWorldCompleted(world), Is.False);
                ProgressService levelProgress = new(world, 12);
                Assert.That(levelProgress.HighestUnlockedLevelIndex, Is.Zero);
                Assert.That(levelProgress.IsLevelUnlocked(1), Is.False);
                foreach (int checkpoint in new[] { 3, 6, 9, 12 })
                    Assert.That(new StoryCheckpointProgress().HasViewed(world, checkpoint), Is.False);
            }
            Assert.That(PlayerPrefs.HasKey("PipeMuzzle.HighestUnlockedLevel"), Is.False);
            Assert.That(BasicRotationTutorialProgress.HasSeen, Is.False);
            Assert.That(GameFeedback.SoundEnabled, Is.EqualTo(sound));
            Assert.That(new FeedbackSettings().SoundEnabled, Is.EqualTo(sound));
            Assert.That(GameFeedback.HapticsEnabled, Is.False);
            Assert.That(PlayerPrefs.GetInt(OtherKey), Is.EqualTo(73));
            Assert.That(controller.CurrentWorld.WorldId, Is.EqualTo(WorldId.SakuraGarden));
            Assert.That(controller.IsLevelUnlocked(0), Is.True);
            Assert.That(controller.IsLevelUnlocked(1), Is.False);
            Assert.That(controller.CurrentLevelDefinition, Is.Null);
            Assert.That(map.activeSelf, Is.True);
            Assert.That(settingsPanel.activeSelf, Is.False);
            Assert.That(MapDestination("Sakura Garden").interactable, Is.True);
            Assert.That(MapDestination("Bamboo Workshop").interactable, Is.False);
            Assert.That(MapDestination("Moon Shrine").interactable, Is.False);
            Assert.That(map.transform.Find("Sakura Garden/Status").GetComponent<TMP_Text>().text, Is.EqualTo("12 LEVELS"));
        }

        [Test]
        public void OtherNavigationClosesSettingsAndItsConfirmation()
        {
            BuildScreens();
            foreach (Action navigate in new Action[] { screens.ShowComic, screens.ShowLevelSelect, screens.ShowGameplay })
            {
                OpenSettings();
                Button("ResetProgressButton").onClick.Invoke();
                navigate();
                Assert.That(settingsPanel.activeSelf, Is.False);
                OpenSettings();
                Assert.That(Confirmation().activeSelf, Is.False);
            }
        }

        [Test]
        public void PortraitSettingsHasReadableTextAndTouchTargets()
        {
            BuildScreens();
            ((RectTransform)settingsPanel.transform).sizeDelta = new Vector2(1080f, 1918f);
            OpenSettings();
            Canvas.ForceUpdateCanvases();
            settings.Refresh();
            Transform surface = settingsPanel.transform.Find("Content/Surface");
            const float portraitCanvasScale = .296f; // Scene scaler at 320 x 568.
            Assert.That(Label("SoundButton").fontSize * surface.localScale.x * portraitCanvasScale, Is.GreaterThanOrEqualTo(11.5f), ((RectTransform)surface.parent).rect.ToString());
            Assert.That(((RectTransform)Button("SoundButton").transform).rect.height * surface.localScale.x * portraitCanvasScale, Is.GreaterThanOrEqualTo(44f));
        }

        [Test]
        public void PortraitMapEntryHasTouchTargetInsideSafeArea()
        {
            BuildScreens();
            ((RectTransform)map.transform).sizeDelta = new Vector2(1080f, 1918f);
            map.GetComponent<WorldMapUI>().Refresh();
            Button entry = map.transform.Find("SettingsEntry/SettingsButton").GetComponent<Button>();
            RectTransform rect = (RectTransform)entry.transform;
            Assert.That(rect.rect.height * rect.localScale.y * .296f, Is.GreaterThanOrEqualTo(44f));
            Assert.That(entry.GetComponentInParent<SafeAreaPanel>(), Is.Not.Null);
        }

        private void BuildScreens()
        {
            root = new GameObject("SettingsTest", typeof(RectTransform));
            root.SetActive(false);
            screens = root.AddComponent<ScreenManager>();
            controller = root.AddComponent<GameController>();
            map = Panel("Map");
            WorldMapUI mapUi = map.AddComponent<WorldMapUI>();
            mapUi.Initialize();
            comic = Panel("Comic"); levels = Panel("Levels"); hud = Panel("Hud");
            screens.Configure(map, comic, levels, hud);
            settingsPanel = Panel("Settings");
            settings = settingsPanel.AddComponent<SettingsUI>();
            settings.Initialize(screens, () => controller.ConfigureWorld(Resources.Load<WorldDefinition>("Worlds/SakuraGarden")));
            screens.ConfigureSettings(settingsPanel);
            mapUi.ConfigureSettings(screens);
            screens.ShowWorldMap();
            root.SetActive(true);
        }

        private GameObject Panel(string name)
        {
            GameObject panel = new(name, typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            ((RectTransform)panel.transform).sizeDelta = new Vector2(700, 800);
            return panel;
        }

        private void OpenSettings() => screens.ShowSettings();
        private Button Button(string name) => settingsPanel.transform.Find($"Content/Surface/{name}").GetComponent<Button>();
        private TMP_Text Label(string name) => Button(name).GetComponentInChildren<TMP_Text>();
        private GameObject Confirmation() => settingsPanel.transform.Find("Content/Confirmation").gameObject;
        private Button MapDestination(string world) => map.transform.Find($"{world}/WorldNode").GetComponent<Button>();
        private void AssertOnlySettings()
        {
            Assert.That(settingsPanel.activeSelf, Is.True);
            Assert.That(map.activeSelf || comic.activeSelf || levels.activeSelf || hud.activeSelf, Is.False);
        }

        private static WorldId[] Worlds() => new[] { WorldId.SakuraGarden, WorldId.BambooWorkshop, WorldId.MoonShrine };
        private static IEnumerable<string> Keys()
        {
            yield return SoundKey; yield return HapticsKey; yield return OtherKey;
            yield return BasicRotationTutorialProgress.SeenKey;
            yield return "PipeMuzzle.HighestUnlockedLevel";
            yield return "PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1";
            foreach (WorldId world in Worlds())
            {
                foreach (string suffix in new[] { "Unlocked", "Completed", "HighestUnlockedLevel" })
                    yield return $"PipeMuzzle.Progress.World.{world}.{suffix}";
                foreach (int checkpoint in new[] { 3, 6, 9, 12 })
                    yield return $"PipeMuzzle.Story.World.{world}.Checkpoint.{checkpoint}.Viewed";
            }
        }

        private static void SeedProgress()
        {
            foreach (string key in Keys())
                if (key.StartsWith("PipeMuzzle.Progress.World.") || key.StartsWith("PipeMuzzle.Story."))
                    PlayerPrefs.SetInt(key, key.EndsWith("HighestUnlockedLevel") ? 11 : 1);
            PlayerPrefs.SetInt("PipeMuzzle.HighestUnlockedLevel", 11);
            BasicRotationTutorialProgress.MarkSeen();
        }

        private static void AssertSeededProgress()
        {
            foreach (WorldId world in Worlds())
            {
                Assert.That(new WorldProgressService().IsWorldCompleted(world), Is.True);
                Assert.That(new ProgressService(world, 12).HighestUnlockedLevelIndex, Is.EqualTo(11));
                foreach (int checkpoint in new[] { 3, 6, 9, 12 })
                    Assert.That(new StoryCheckpointProgress().HasViewed(world, checkpoint), Is.True);
            }
            Assert.That(BasicRotationTutorialProgress.HasSeen, Is.True);
        }

        private static void ResetFeedbackCache() => typeof(GameFeedback)
            .GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
}
