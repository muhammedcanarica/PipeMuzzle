#if UNITY_EDITOR
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using UnityEditor;

namespace PipeMuzzle.Editor
{
    public static class PipeMuzzleStoryDebugMenu
    {
        private const string Menu = "PipeMuzzle/Debug/Reset Story Checkpoints/";

        [MenuItem(Menu + "Reset Sakura Story Checkpoints")]
        public static void ResetSakura() => StoryCheckpointProgress.ResetViewedCheckpointsForWorld(WorldId.SakuraGarden);

        [MenuItem(Menu + "Reset Bamboo Story Checkpoints")]
        public static void ResetBamboo() => StoryCheckpointProgress.ResetViewedCheckpointsForWorld(WorldId.BambooWorkshop);

        [MenuItem(Menu + "Reset Moon Story Checkpoints")]
        public static void ResetMoon() => StoryCheckpointProgress.ResetViewedCheckpointsForWorld(WorldId.MoonShrine);

        [MenuItem(Menu + "Reset All Story Checkpoints")]
        public static void ResetAll()
        {
            ResetSakura();
            ResetBamboo();
            ResetMoon();
        }
    }
}
#endif
