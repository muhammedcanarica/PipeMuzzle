using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using PipeMuzzle.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class GameControllerWorldTests
    {
        private static readonly string[] Keys =
        {
            "PipeMuzzle.HighestUnlockedLevel",
            "PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1",
            "PipeMuzzle.Progress.World.SakuraGarden.HighestUnlockedLevel",
            "PipeMuzzle.Progress.World.BambooWorkshop.HighestUnlockedLevel",
            "PipeMuzzle.Progress.World.MoonShrine.HighestUnlockedLevel",
            "PipeMuzzle.Progress.World.SakuraGarden.Unlocked",
            "PipeMuzzle.Progress.World.BambooWorkshop.Unlocked",
            "PipeMuzzle.Progress.World.MoonShrine.Unlocked",
            "PipeMuzzle.Progress.World.SakuraGarden.Completed",
            "PipeMuzzle.Progress.World.BambooWorkshop.Completed",
            "PipeMuzzle.Progress.World.MoonShrine.Completed"
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
        public void SelectedWorldDeterminesLoadedDefinitionAndProgressNamespace()
        {
            GameController controller = CreateController();
            WorldDefinition sakura = Sakura();
            WorldDefinition bamboo = CreateCompleteWorld(WorldId.BambooWorkshop);
            WorldProgressService worlds = new();
            int loadCount = 0;
            controller.LevelLoaded += (_, _) => loadCount++;

            Assert.That(controller.LevelCount, Is.Zero);
            Assert.That(controller.ConfigureWorld(sakura), Is.True);
            Assert.That(controller.CurrentWorld, Is.SameAs(sakura));
            Assert.That(controller.LevelCount, Is.EqualTo(12));
            controller.LoadLevelByIndex(0);
            Assert.That(controller.CurrentLevelDefinition, Is.SameAs(sakura.Levels[0]));
            Assert.That(loadCount, Is.EqualTo(1));

            new ProgressService(WorldId.SakuraGarden, 12).UnlockLevel(1);
            worlds.MarkWorldCompleted(WorldId.SakuraGarden);
            Assert.That(controller.ConfigureWorld(bamboo), Is.True);
            Assert.That(controller.CurrentLevelNumber, Is.Zero);
            Assert.That(controller.IsLevelUnlocked(1), Is.False);
            controller.LoadLevelByIndex(0);
            Assert.That(controller.CurrentLevelDefinition, Is.SameAs(bamboo.Levels[0]));
            Assert.That(controller.CurrentLevelDefinition, Is.Not.SameAs(sakura.Levels[0]));
            Assert.That(loadCount, Is.EqualTo(2));
        }

        [Test]
        public void InvalidOrUnavailableWorldClearsPreviousGameplayContext()
        {
            GameController controller = CreateController();
            Assert.That(controller.ConfigureWorld(Sakura()), Is.True);
            controller.LoadLevelByIndex(0);

            LogAssert.Expect(LogType.Error,
                "GameController cannot configure an unavailable world.");
            Assert.That(controller.ConfigureWorld(null), Is.False);
            Assert.That(controller.CurrentWorld, Is.Null);
            Assert.That(controller.CurrentLevelDefinition, Is.Null);
            Assert.That(controller.CurrentLevelNumber, Is.Zero);
            Assert.That(controller.LevelCount, Is.Zero);

            WorldDefinition incomplete = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/BambooWorkshop.asset");
            LogAssert.Expect(LogType.Error,
                "GameController cannot configure an unavailable world.");
            Assert.That(controller.ConfigureWorld(incomplete), Is.False);

            WorldDefinition lockedCompleteMoon = CreateCompleteWorld(WorldId.MoonShrine);
            LogAssert.Expect(LogType.Error,
                "GameController cannot configure an unavailable world.");
            Assert.That(controller.ConfigureWorld(lockedCompleteMoon), Is.False);
        }

        [Test]
        public void OutOfRangeLevelCannotLoad()
        {
            GameController controller = CreateController();
            Assert.That(controller.ConfigureWorld(Sakura()), Is.True);
            int loaded = 0;
            controller.LevelLoaded += (_, _) => loaded++;

            controller.LoadLevelByIndex(-1);
            controller.LoadLevelByIndex(12);

            Assert.That(loaded, Is.Zero);
            Assert.That(controller.CurrentLevelDefinition, Is.Null);
        }

        [Test]
        public void OrdinaryCompletionUnlocksOnlyNextLevel()
        {
            GameController controller = CreateController();
            Assert.That(controller.ConfigureWorld(Sakura()), Is.True);
            controller.LoadLevelByIndex(0);

            CompleteLoadedLevel(controller);

            Assert.That(new ProgressService(WorldId.SakuraGarden, 12)
                .IsLevelUnlocked(1), Is.True);
            Assert.That(new WorldProgressService()
                .IsWorldUnlocked(WorldId.BambooWorkshop), Is.False);
        }

        [Test]
        public void FinalCompletionAdvancesWorldChainWithoutLevelThirteen()
        {
            GameController controller = CreateController();
            WorldProgressService worlds = new();
            bool? lastHasNext = null;
            int loaded = 0;
            controller.LevelCompleted += hasNext => lastHasNext = hasNext;
            controller.LevelLoaded += (_, _) => loaded++;

            CompleteFinalLevel(controller, Sakura(), WorldId.SakuraGarden);
            Assert.That(worlds.IsWorldUnlocked(WorldId.BambooWorkshop), Is.True);
            Assert.That(worlds.IsWorldUnlocked(WorldId.MoonShrine), Is.False);

            CompleteFinalLevel(controller,
                CreateCompleteWorld(WorldId.BambooWorkshop), WorldId.BambooWorkshop);
            Assert.That(worlds.IsWorldUnlocked(WorldId.MoonShrine), Is.True);

            WorldDefinition moon = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/MoonShrine.asset");
            CompleteFinalLevel(controller, moon, WorldId.MoonShrine);
            Assert.That(worlds.IsWorldCompleted(WorldId.MoonShrine), Is.True);
            Assert.That(controller.HasNextLevel, Is.False);
            Assert.That(lastHasNext, Is.False);
            Assert.That(controller.LevelCount, Is.EqualTo(12));
            Assert.That(controller.CurrentLevelDefinition, Is.SameAs(moon.Levels[11]));
            int loadedAfterFinale = loaded;
            controller.LoadLevelByIndex(12);
            Assert.That(loaded, Is.EqualTo(loadedAfterFinale));
            Assert.That(controller.CurrentLevelDefinition, Is.SameAs(moon.Levels[11]));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                worlds.IsWorldUnlocked((WorldId)3));
        }

        private static WorldDefinition Sakura() =>
            AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/SakuraGarden.asset");

        private WorldDefinition CreateCompleteWorld(WorldId id)
        {
            WorldDefinition world = ScriptableObject.CreateInstance<WorldDefinition>();
            created.Add(world);
            SerializedObject serialized = new(world);
            serialized.FindProperty("worldId").enumValueIndex = (int)id;
            SerializedProperty list = serialized.FindProperty("levels");
            list.arraySize = 12;
            LevelDefinition[] levels = Sakura().Levels.Reverse().ToArray();
            for (int i = 0; i < levels.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return world;
        }

        private GameController CreateController()
        {
            GameObject boardObject = new("TestBoardView");
            created.Add(boardObject);
            BoardView boardView = boardObject.AddComponent<BoardView>();
            GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/TilePrefab.prefab");
            Assert.That(tilePrefab, Is.Not.Null);
            SerializedObject boardSerialized = new(boardView);
            boardSerialized.FindProperty("tilePrefab").objectReferenceValue =
                tilePrefab.GetComponent<TileView>();
            boardSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject cameraObject = new("TestBoardCamera");
            created.Add(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            BoardCameraFitter fitter = cameraObject.AddComponent<BoardCameraFitter>();
            SerializedObject fitterSerialized = new(fitter);
            fitterSerialized.FindProperty("targetCamera").objectReferenceValue = camera;
            fitterSerialized.FindProperty("boardView").objectReferenceValue = boardView;
            fitterSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject controllerObject = new("TestGameController");
            created.Add(controllerObject);
            GameController controller = controllerObject.AddComponent<GameController>();
            SerializedObject controllerSerialized = new(controller);
            controllerSerialized.FindProperty("boardView").objectReferenceValue = boardView;
            controllerSerialized.FindProperty("boardCameraFitter").objectReferenceValue = fitter;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static void CompleteLoadedLevel(GameController controller)
        {
            MethodInfo complete = typeof(GameController).GetMethod("CompleteLevel",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(complete, Is.Not.Null);
            complete.Invoke(controller, null);
        }

        private static void CompleteFinalLevel(
            GameController controller, WorldDefinition world, WorldId id)
        {
            new ProgressService(id, 12).UnlockLevel(11);
            Assert.That(controller.ConfigureWorld(world), Is.True);
            controller.LoadLevelByIndex(11);
            Assert.That(controller.CurrentLevelNumber, Is.EqualTo(12));
            CompleteLoadedLevel(controller);
            Assert.That(new WorldProgressService().IsWorldCompleted(id), Is.True);
        }
    }
}
