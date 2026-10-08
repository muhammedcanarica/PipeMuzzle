using System;
using System.Collections.Generic;
using PipeMuzzle.Data;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Editor
{
    internal static class LevelHintBaker
    {
        [MenuItem("Ruilay/Bake Hint Solutions")]
        internal static void BakeAll()
        {
            // Validate everything before touching any asset. Tile layouts are never edited.
            var solutions = new List<(LevelDefinition level, LevelValidationResult result)>();
            foreach (WorldDefinition world in Resources.LoadAll<WorldDefinition>("Worlds"))
            foreach (LevelDefinition level in world.Levels)
            {
                LevelValidationResult result = LevelValidationUtility.Analyze(level);
                if (result.Errors.Count != 0 || !result.SearchComplete || result.SolutionPath.Count < 2)
                    throw new InvalidOperationException($"Cannot bake {level.name}: {string.Join("; ", result.Errors)}");
                solutions.Add((level, result));
            }

            foreach (var solution in solutions)
            {
                SerializedObject asset = new(solution.level);
                SerializedProperty path = asset.FindProperty("solutionPath");
                path.arraySize = solution.result.SolutionPath.Count;
                for (int i = 0; i < path.arraySize; i++)
                {
                    Vector2Int pos = solution.result.SolutionPath[i];
                    SerializedProperty step = path.GetArrayElementAtIndex(i);
                    step.FindPropertyRelative("position").vector2IntValue = pos;
                    step.FindPropertyRelative("rotation").intValue = solution.result.SolutionRotations[pos];
                }
                asset.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Baked hint solutions for {solutions.Count} levels.");
        }
    }
}
