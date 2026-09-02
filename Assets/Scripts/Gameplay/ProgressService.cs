using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public class ProgressService
    {
        private const string HighestUnlockedLevelKey =
            "PipeMuzzle.HighestUnlockedLevel";

        public int HighestUnlockedLevelIndex { get; private set; }

        public ProgressService()
        {
            HighestUnlockedLevelIndex = Mathf.Max(
                0,
                PlayerPrefs.GetInt(
                    HighestUnlockedLevelKey,
                    0
                )
            );
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex >= 0 &&
                   levelIndex <= HighestUnlockedLevelIndex;
        }

        public void UnlockLevel(int levelIndex)
        {
            if (levelIndex <= HighestUnlockedLevelIndex)
            {
                return;
            }

            HighestUnlockedLevelIndex = levelIndex;

            PlayerPrefs.SetInt(
                HighestUnlockedLevelKey,
                HighestUnlockedLevelIndex
            );

            PlayerPrefs.Save();
        }
    }
}
