using System;
using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public class ProgressService
    {
        private const string LegacyHighestUnlockedLevelKey =
            "PipeMuzzle.HighestUnlockedLevel";
        private const string MigrationMarkerKey =
            "PipeMuzzle.Progress.Migration.LegacyHighestUnlockedLevelToSakura.V1";

        private readonly string progressKey;
        private readonly int levelCount;

        public int HighestUnlockedLevelIndex { get; private set; }

        public ProgressService(WorldId worldId, int levelCount)
        {
            if (levelCount < 0)
                throw new ArgumentOutOfRangeException(nameof(levelCount));

            progressKey = $"PipeMuzzle.Progress.World.{WorldIdPersistence.Segment(worldId)}.HighestUnlockedLevel";
            this.levelCount = levelCount;

            if (worldId == WorldId.SakuraGarden)
                MigrateLegacySakuraProgress();

            HighestUnlockedLevelIndex = levelCount == 0
                ? -1
                : Mathf.Clamp(PlayerPrefs.GetInt(progressKey, 0), 0, levelCount - 1);
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex >= 0 &&
                   levelIndex < levelCount &&
                   levelIndex <= HighestUnlockedLevelIndex;
        }

        public void UnlockLevel(int levelIndex)
        {
            if (levelIndex < 0 ||
                levelIndex >= levelCount ||
                levelIndex <= HighestUnlockedLevelIndex)
            {
                return;
            }

            HighestUnlockedLevelIndex = levelIndex;

            PlayerPrefs.SetInt(
                progressKey,
                HighestUnlockedLevelIndex
            );

            PlayerPrefs.Save();
        }

        private void MigrateLegacySakuraProgress()
        {
            if (PlayerPrefs.GetInt(MigrationMarkerKey, 0) == 1)
                return;

            if (levelCount > 0 && PlayerPrefs.HasKey(LegacyHighestUnlockedLevelKey))
            {
                int legacy = Mathf.Clamp(
                    PlayerPrefs.GetInt(LegacyHighestUnlockedLevelKey),
                    0, levelCount - 1);
                int existing = PlayerPrefs.HasKey(progressKey)
                    ? Mathf.Clamp(PlayerPrefs.GetInt(progressKey), 0, levelCount - 1)
                    : 0;
                int merged = Mathf.Max(legacy, existing);
                if (!PlayerPrefs.HasKey(progressKey) ||
                    PlayerPrefs.GetInt(progressKey) != merged)
                {
                    PlayerPrefs.SetInt(progressKey, merged);
                }
            }

            PlayerPrefs.SetInt(MigrationMarkerKey, 1);
            PlayerPrefs.Save();
        }
    }
}
