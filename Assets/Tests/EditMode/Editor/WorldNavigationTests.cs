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
        public void MapRefreshShowsComingSoonWithoutRebuildingCards()
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
            Assert.That(bamboo.interactable, Is.False);
            Assert.That(Status(bamboo), Is.EqualTo("COMING SOON"));
            Assert.That(Status(moon), Is.EqualTo("LOCKED"));
        }

        [Test]
        public void DuplicateAndUnknownWorldAssetsAreSkipped()
        {
            WorldDefinition unknown = ScriptableObject.CreateInstance<WorldDefinition>();
            created.Add(unknown);
            typeof(WorldDefinition).GetField("worldId",
                BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(unknown, (WorldId)999);
            MethodInfo validate = typeof(WorldMapUI).GetMethod("ValidateWorlds",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(validate, Is.Not.Null);

            LogAssert.Expect(LogType.Error, "WorldMapUI skipped a duplicate WorldId: SakuraGarden.");
            LogAssert.Expect(LogType.Error, "WorldMapUI skipped an unknown WorldId: 999.");
            var result = (IEnumerable<WorldDefinition>)validate.Invoke(null,
                new object[] { new[] { Sakura(), Sakura(), unknown } });
            Assert.That(result.ToArray(), Is.EqualTo(new[] { Sakura() }));
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

            LogAssert.Expect(LogType.Error, "GameController cannot configure an unavailable world.");
            select.ConfigureForWorld(Bamboo());
            Assert.That(select.CurrentWorld, Is.Null);
            Assert.That(controller.CurrentWorld, Is.Null);
            Assert.That(buttons.All(button => !button.interactable), Is.True);
        }

        private static string Status(Button button) =>
            button.GetComponentsInChildren<TMP_Text>(true).Last().text;

        private static WorldDefinition Sakura() => AssetDatabase.LoadAssetAtPath<WorldDefinition>(
            "Assets/Resources/Worlds/SakuraGarden.asset");

        private static WorldDefinition Bamboo() => AssetDatabase.LoadAssetAtPath<WorldDefinition>(
            "Assets/Resources/Worlds/BambooWorkshop.asset");
    }
}
