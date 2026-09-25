using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PipeMuzzle.Data;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class WorldDefinitionTests
    {
        [Test]
        public void SakuraOwnsTwelveExistingLevelsInNumberOrder()
        {
            WorldDefinition sakura = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/SakuraGarden.asset");

            Assert.That(sakura, Is.Not.Null);
            Assert.That(sakura.LevelCount, Is.EqualTo(12));
            Assert.That(sakura.IsContentReady, Is.True);
            Assert.That(sakura.Levels.Select(level => level.name),
                Is.EqualTo(Enumerable.Range(1, 12).Select(i => $"Level_{i:000}")));
            Assert.That(sakura.Levels.Distinct().Count(), Is.EqualTo(12));
        }

        [Test]
        public void MoonShrineHasTwelvePlayableLevels()
        {
            WorldDefinition world = AssetDatabase.LoadAssetAtPath<WorldDefinition>(
                "Assets/Resources/Worlds/MoonShrine.asset");

            Assert.That(world, Is.Not.Null);
            Assert.That(world.LevelCount, Is.EqualTo(12));
            Assert.That(world.IsContentReady, Is.True);
        }

        [Test]
        public void CountComesFromActualListAndIncompleteListsAreRejected()
        {
            WorldDefinition world = ScriptableObject.CreateInstance<WorldDefinition>();
            LevelDefinition[] levels = Enumerable.Range(1, 12)
                .Select(i => AssetDatabase.LoadAssetAtPath<LevelDefinition>(
                    $"Assets/Scripts/Data/Level_{i:000}.asset"))
                .ToArray();

            try
            {
                SetLevels(world, levels.Take(11).ToArray());
                Assert.That(world.LevelCount, Is.EqualTo(11));
                Assert.That(world.IsContentReady, Is.False);

                SetLevels(world, levels);
                Assert.That(world.LevelCount, Is.EqualTo(12));
                Assert.That(world.IsContentReady, Is.True);

                LevelDefinition[] withNull = (LevelDefinition[])levels.Clone();
                withNull[3] = null;
                SetLevels(world, withNull);
                Assert.That(world.IsContentReady, Is.False);

                LevelDefinition[] withDuplicate = (LevelDefinition[])levels.Clone();
                withDuplicate[3] = withDuplicate[2];
                SetLevels(world, withDuplicate);
                Assert.That(world.IsContentReady, Is.False);

                SetLevels(world, System.Array.Empty<LevelDefinition>());
                Assert.That(world.LevelCount, Is.Zero);
                Assert.That(world.IsContentReady, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(world);
            }
        }

        private static void SetLevels(
            WorldDefinition world,
            IReadOnlyList<LevelDefinition> levels)
        {
            SerializedObject serialized = new(world);
            SerializedProperty list = serialized.FindProperty("levels");
            Assert.That(list, Is.Not.Null);
            list.arraySize = levels.Count;
            for (int i = 0; i < levels.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
