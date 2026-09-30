using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public sealed class StoryCheckpointProgress
    {
        public bool HasViewed(WorldId world, int completedLevelNumber) =>
            PlayerPrefs.GetInt(Key(world, completedLevelNumber), 0) == 1;

        public bool TryBegin(WorldDefinition world, int completedLevelNumber, out StoryCheckpoint checkpoint)
        {
            checkpoint = null;
            if (world == null || HasViewed(world.WorldId, completedLevelNumber)) return false;
            checkpoint = world.GetStoryCheckpoint(completedLevelNumber);
            if (checkpoint?.Story == null || checkpoint.Story.PanelCount == 0)
            {
                checkpoint = null;
                return false;
            }

            // Consume on presentation, including skip/back, so replay can never repeat it.
            PlayerPrefs.SetInt(Key(world.WorldId, completedLevelNumber), 1);
            PlayerPrefs.Save();
            return true;
        }

        private static string Key(WorldId world, int completedLevelNumber) =>
            $"PipeMuzzle.Story.World.{WorldIdPersistence.Segment(world)}.Checkpoint.{completedLevelNumber}.Viewed";
    }
}
