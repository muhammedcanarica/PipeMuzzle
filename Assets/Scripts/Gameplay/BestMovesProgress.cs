using System;
using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public static class BestMovesProgress
    {
        // Public level numbers are one-based, matching the HUD.
        public static int? GetBest(WorldId world, int level)
        {
            if (!Valid(world, level)) return null;
            int value = PlayerPrefs.GetInt(Key(world, level), 0);
            return value > 0 ? value : null;
        }

        public static bool TrySetBest(WorldId world, int level, int moves)
        {
            if (moves <= 0 || !Valid(world, level)) return false;
            int? previous = GetBest(world, level);
            if (previous.HasValue && moves >= previous.Value) return false;
            PlayerPrefs.SetInt(Key(world, level), moves);
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetAll()
        {
            foreach (WorldId world in Enum.GetValues(typeof(WorldId)))
            for (int level = 1; level <= WorldDefinition.ExpectedLevelCount; level++)
                PlayerPrefs.DeleteKey(Key(world, level));
            PlayerPrefs.Save();
        }

        private static bool Valid(WorldId world, int level) =>
            Enum.IsDefined(typeof(WorldId), world) && level >= 1 && level <= WorldDefinition.ExpectedLevelCount;
        private static string Key(WorldId world, int level) =>
            $"PipeMuzzle.BestMoves.World.{WorldIdPersistence.Segment(world)}.Level.{level}";
    }
}
