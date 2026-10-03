using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public sealed class StoryCheckpointProgress
    {
        public bool HasViewed(WorldId world, int completedLevelNumber) =>
            completedLevelNumber > 0 && PlayerPrefs.GetInt(Key(world, completedLevelNumber), 0) == 1;

        public bool TryBegin(WorldDefinition world, int completedLevelNumber, out StoryCheckpoint checkpoint)
        {
            checkpoint = null;
            if (world == null || completedLevelNumber <= 0) return false;
            if (HasViewed(world.WorldId, completedLevelNumber))
            {
                Log($"Story checkpoint already viewed: {world.WorldId} / Level {completedLevelNumber}");
                return false;
            }
            checkpoint = world.GetStoryCheckpoint(completedLevelNumber);
            if (checkpoint?.Story == null || checkpoint.Story.PanelCount == 0)
            {
                checkpoint = null;
                return false;
            }

            return true;
        }

        public void MarkViewed(WorldId world, int completedLevelNumber)
        {
            // Intro is replayable and never participates in checkpoint persistence.
            if (completedLevelNumber <= 0 || HasViewed(world, completedLevelNumber)) return;
            PlayerPrefs.SetInt(Key(world, completedLevelNumber), 1);
            PlayerPrefs.Save();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        internal static void Log(string message) => Debug.Log(message);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Re-arm story checkpoints for testing without changing world or level progress.
        public static void ResetViewedCheckpointsForWorld(WorldId world)
        {
            foreach (int completedLevelNumber in new[] { 3, 6, 9, 12 })
                PlayerPrefs.DeleteKey(Key(world, completedLevelNumber));
            PlayerPrefs.Save();
        }
#endif

        private static string Key(WorldId world, int completedLevelNumber) =>
            $"PipeMuzzle.Story.World.{WorldIdPersistence.Segment(world)}.Checkpoint.{completedLevelNumber}.Viewed";
    }
}
