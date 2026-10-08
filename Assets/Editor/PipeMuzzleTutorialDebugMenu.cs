#if UNITY_EDITOR
using PipeMuzzle.Gameplay;
using UnityEditor;

namespace PipeMuzzle.Editor
{
    public static class PipeMuzzleTutorialDebugMenu
    {
        [MenuItem("Ruilay/Debug/Reset Tutorial")]
        public static void ResetTutorial() => BasicRotationTutorialProgress.Reset();
    }
}
#endif
