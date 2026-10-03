using PipeMuzzle.Data;

namespace PipeMuzzle.Gameplay
{
    public static class ProgressResetService
    {
        public static void ResetAllProgress()
        {
            // Only these worlds and their known progress keys belong to this reset.
            foreach (WorldId world in new[] { WorldId.SakuraGarden, WorldId.BambooWorkshop, WorldId.MoonShrine })
            {
                ProgressService.ResetForWorld(world);
                WorldProgressService.ResetForWorld(world);
                StoryCheckpointProgress.ResetViewedCheckpointsForWorld(world);
            }
            BasicRotationTutorialProgress.Reset();
        }
    }
}
