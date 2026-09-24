using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.Gameplay
{
    public enum WorldAccessState
    {
        Locked,
        ComingSoon,
        Playable
    }

    public sealed class WorldProgressService
    {
        public WorldProgressService()
        {
            string sakuraKey = Key(WorldId.SakuraGarden, "Unlocked");
            if (!PlayerPrefs.HasKey(sakuraKey))
            {
                PlayerPrefs.SetInt(sakuraKey, 1);
                PlayerPrefs.Save();
            }
        }

        public bool IsWorldUnlocked(WorldId worldId)
        {
            return PlayerPrefs.GetInt(Key(worldId, "Unlocked"), 0) == 1;
        }

        public bool IsWorldCompleted(WorldId worldId)
        {
            return PlayerPrefs.GetInt(Key(worldId, "Completed"), 0) == 1;
        }

        public WorldAccessState GetAccessState(WorldDefinition world)
        {
            if (world == null || !IsWorldUnlocked(world.WorldId))
                return WorldAccessState.Locked;

            return world.IsContentReady
                ? WorldAccessState.Playable
                : WorldAccessState.ComingSoon;
        }

        public void MarkWorldCompleted(WorldId worldId)
        {
            if (!IsWorldUnlocked(worldId))
                return;

            bool changed = SetOne(Key(worldId, "Completed"));

            WorldId? nextWorld = worldId switch
            {
                WorldId.SakuraGarden => WorldId.BambooWorkshop,
                WorldId.BambooWorkshop => WorldId.MoonShrine,
                WorldId.MoonShrine => null,
                _ => null
            };

            if (nextWorld.HasValue)
                changed |= SetOne(Key(nextWorld.Value, "Unlocked"));

            if (changed)
                PlayerPrefs.Save();
        }

        private static string Key(WorldId worldId, string suffix)
        {
            return $"PipeMuzzle.Progress.World.{WorldIdPersistence.Segment(worldId)}.{suffix}";
        }

        private static bool SetOne(string key)
        {
            if (PlayerPrefs.GetInt(key, 0) == 1)
                return false;

            PlayerPrefs.SetInt(key, 1);
            return true;
        }
    }
}
