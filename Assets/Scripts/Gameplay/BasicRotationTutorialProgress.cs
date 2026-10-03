using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public static class BasicRotationTutorialProgress
    {
        public const string SeenKey = "PipeMuzzle.Tutorial.BasicRotationSeen";
        public static bool HasSeen => PlayerPrefs.GetInt(SeenKey, 0) == 1;

        public static void MarkSeen()
        {
            if (HasSeen) return;
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
        }
    }
}
