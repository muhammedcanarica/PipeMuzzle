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
using UnityEngine.UI;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class LevelSelectPresentationTests
    {
        private readonly Dictionary<string, (bool exists, int value)> saved = new();
        private readonly List<Object> owned = new();
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            Save("PipeMuzzle.BestMoves.World.SakuraGarden.Level.1");
            foreach (WorldId id in new[] { WorldId.SakuraGarden, WorldId.BambooWorkshop, WorldId.MoonShrine })
            foreach (string suffix in new[] { "Unlocked", "Completed", "HighestUnlockedLevel" })
                Save($"PipeMuzzle.Progress.World.{id}.{suffix}");
            Save("PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1");
            PlayerPrefs.SetInt("PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1", 1);
            foreach (WorldId id in new[] { WorldId.SakuraGarden, WorldId.BambooWorkshop, WorldId.MoonShrine })
                PlayerPrefs.SetInt($"PipeMuzzle.Progress.World.{id}.Unlocked", 1);
        }

        private void Save(string key)
        {
            saved[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
            PlayerPrefs.DeleteKey(key);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
            foreach (var pair in saved)
                if (pair.Value.exists) PlayerPrefs.SetInt(pair.Key, pair.Value.value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
            saved.Clear();
        }

        private (LevelSelectUI ui, GameController controller, RectTransform panel, List<Button> buttons) Build()
        {
            GameObject root = new("SelectFixture", typeof(RectTransform));
            root.SetActive(false);
            owned.Add(root);
            GameController controller = root.AddComponent<GameController>();
            LevelSelectUI ui = root.AddComponent<LevelSelectUI>();
            GameObject panel = new("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.sizeDelta = new Vector2(1920, 1080);
            GameObject heading = new("WorldTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            heading.transform.SetParent(panel.transform, false);
            GameObject path = new("Path", typeof(RectTransform));
            path.transform.SetParent(panel.transform, false);
            ((RectTransform)path.transform).sizeDelta = new Vector2(1200, 700);
            List<Button> buttons = new();
            for (int i = 0; i < 12; i++)
            {
                GameObject node = new("Level" + (i + 1), typeof(RectTransform), typeof(Image), typeof(Button));
                node.transform.SetParent(path.transform, false);
                ((RectTransform)node.transform).sizeDelta = new Vector2(120, 64);
                GameObject label = new("Number", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(node.transform, false);
                label.GetComponent<TMP_Text>().text = (i + 1).ToString();
                buttons.Add(node.GetComponent<Button>());
            }
            SerializedObject data = new(ui);
            data.FindProperty("gameController").objectReferenceValue = controller;
            data.FindProperty("levelSelectPanel").objectReferenceValue = panel;
            data.FindProperty("pathArea").objectReferenceValue = path.transform;
            data.FindProperty("worldTitleText").objectReferenceValue = heading.GetComponent<TMP_Text>();
            var list = data.FindProperty("levelButtons");
            list.arraySize = 12;
            for (int i = 0; i < 12; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            return (ui, controller, rect, buttons);
        }

        [TestCase(WorldId.SakuraGarden)]
        [TestCase(WorldId.BambooWorkshop)]
        [TestCase(WorldId.MoonShrine)]
        public void JourneyShowsProgressAndOnlyFourCheckpointOrnaments(WorldId id)
        {
            PlayerPrefs.SetInt($"PipeMuzzle.Progress.World.{id}.HighestUnlockedLevel", 4);
            var fixture = Build();
            fixture.ui.ConfigureForWorld(Resources.Load<WorldDefinition>("Worlds/" + id));
            var view = fixture.panel.GetComponent("LevelSelectJourneyView");
            Assert.That(view, Is.Not.Null, "Level Select needs its journey presentation.");
            var state = view.GetType().GetMethod("GetNodeState");
            Assert.That(state.Invoke(view, new object[] { 0 }).ToString(), Is.EqualTo("Completed"));
            Assert.That(state.Invoke(view, new object[] { 4 }).ToString(), Is.EqualTo("Current"));
            Assert.That(state.Invoke(view, new object[] { 5 }).ToString(), Is.EqualTo("Locked"));
            for (int i = 0; i < 12; i++)
            {
                Assert.That(fixture.buttons[i].interactable, Is.EqualTo(i <= 4));
                Assert.That(fixture.buttons[i].transform.Find("CheckpointOrnament").gameObject.activeSelf, Is.EqualTo((i + 1) % 3 == 0));
            }
            Assert.That(fixture.panel.Find("LevelJourneyMarker").GetComponent<Image>().sprite,
                Is.EqualTo(Resources.Load<Sprite>("WorldMap/Journey/ChibiTraveler")));
        }

        [Test]
        public void HoverAndKeyboardSelectionShowOneSharedBestLabel()
        {
            var fixture = Build();
            PlayerPrefs.SetInt("PipeMuzzle.Progress.World.SakuraGarden.HighestUnlockedLevel", 1);
            BestMovesProgress.TrySetBest(WorldId.SakuraGarden, 1, 18);
            fixture.ui.ConfigureForWorld(Resources.Load<WorldDefinition>("Worlds/SakuraGarden"));
            Transform info = fixture.panel.Find("LevelJourneyBest");
            Assert.That(info, Is.Not.Null, "Journey needs one shared record preview.");
            TMP_Text label = info.GetComponent<TMP_Text>();
            fixture.buttons[0].GetComponent<LevelSelectNodeFeedback>().OnPointerEnter(null);
            Assert.That(label.text, Is.EqualTo("LEVEL 01  ·  BEST 18"));
            fixture.buttons[0].GetComponent<LevelSelectNodeFeedback>().OnPointerExit(null);
            Assert.That(info.gameObject.activeSelf, Is.False);
            fixture.buttons[1].GetComponent<LevelSelectNodeFeedback>().OnSelect(null);
            Assert.That(label.text, Is.EqualTo("LEVEL 02  ·  BEST --"));
            fixture.buttons[1].GetComponent<LevelSelectNodeFeedback>().OnDeselect(null);
            Assert.That(info.gameObject.activeSelf, Is.False);
            Assert.That(fixture.panel.GetComponentsInChildren<TMP_Text>(true).Count(t => t.name == "LevelJourneyBest"), Is.EqualTo(1));
        }

        [TestCase(1920, 1080)]
        [TestCase(2560, 1080)]
        [TestCase(1440, 1080)]
        [TestCase(800, 600)]
        [TestCase(600, 1000)]
        public void TwelveNodesStaySeparateAndInsidePanel(int width, int height)
        {
            var fixture = Build();
            fixture.panel.sizeDelta = new Vector2(width, height);
            PlayerPrefs.SetInt("PipeMuzzle.Progress.World.SakuraGarden.HighestUnlockedLevel", 1);
            fixture.ui.ConfigureForWorld(Resources.Load<WorldDefinition>("Worlds/SakuraGarden"));
            List<Rect> bounds = new();
            foreach (Button button in fixture.buttons)
            {
                RectTransform rect = (RectTransform)button.transform;
                Assert.That(rect.sizeDelta.x, Is.EqualTo(rect.sizeDelta.y).Within(.1f), "Stops should have a seal/stone shape.");
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Vector2 min = fixture.panel.InverseTransformPoint(corners[0]);
                Vector2 max = fixture.panel.InverseTransformPoint(corners[2]);
                Rect bound = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                Assert.That(fixture.panel.rect.Contains(min) && fixture.panel.rect.Contains(max), Is.True);
                Assert.That(bounds.All(other => !other.Overlaps(bound)), Is.True, "Stops must not overlap.");
                bounds.Add(bound);
            }
            Rect marker = BoundsInPanel((RectTransform)fixture.panel.Find("LevelJourneyMarker"), fixture.panel);
            Assert.That(marker.Overlaps(BoundsInPanel((RectTransform)fixture.panel.Find("WorldTitle"), fixture.panel)), Is.False);
            Assert.That(marker.Overlaps(BoundsInPanel((RectTransform)fixture.panel.Find("LevelJourneySubtitle"), fixture.panel)), Is.False);
        }

        private static Rect BoundsInPanel(RectTransform element, RectTransform panel)
        {
            Vector3[] corners = new Vector3[4];
            element.GetWorldCorners(corners);
            Vector2 min = panel.InverseTransformPoint(corners[0]);
            Vector2 max = panel.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        [Test]
        public void SwitchingWorldReusesVisualsAndCompletedFinalIsDistinct()
        {
            var fixture = Build();
            fixture.ui.ConfigureForWorld(Resources.Load<WorldDefinition>("Worlds/SakuraGarden"));
            var view = fixture.panel.GetComponent("LevelSelectJourneyView");
            Assert.That(view, Is.Not.Null);
            int count = fixture.panel.GetComponentsInChildren<Image>(true).Length;
            Color sakura = fixture.buttons[0].GetComponent<Image>().color;
            PlayerPrefs.SetInt("PipeMuzzle.Progress.World.MoonShrine.HighestUnlockedLevel", 11);
            PlayerPrefs.SetInt("PipeMuzzle.Progress.World.MoonShrine.Completed", 1);
            fixture.ui.ConfigureForWorld(Resources.Load<WorldDefinition>("Worlds/MoonShrine"));
            Assert.That(fixture.panel.GetComponentsInChildren<Image>(true).Length, Is.EqualTo(count));
            Assert.That(fixture.buttons[0].GetComponent<Image>().color, Is.Not.EqualTo(sakura));
            Assert.That(view.GetType().GetMethod("GetNodeState").Invoke(view, new object[] { 11 }).ToString(), Is.EqualTo("Completed"));
            Assert.That(((RectTransform)fixture.buttons[11].transform).sizeDelta.x,
                Is.GreaterThan(((RectTransform)fixture.buttons[0].transform).sizeDelta.x));
        }

        [TestCase(-1)]
        [TestCase(5)]
        [TestCase(12)]
        public void LockedAndOutOfRangeSelectionCannotLoadALevel(int index)
        {
            var fixture = Build();
            fixture.ui.ConfigureForWorld(Resources.Load<WorldDefinition>("Worlds/SakuraGarden"));
            typeof(LevelSelectUI).GetMethod("SelectLevel", Private).Invoke(fixture.ui, new object[] { index });
            Assert.That(fixture.controller.CurrentLevelDefinition, Is.Null);
        }
    }
}
