using UnityEngine;

namespace PipeMuzzle.Data
{
    public enum WorldId
    {
        SakuraGarden,
        BambooWorkshop,
        MoonShrine
    }

    [CreateAssetMenu(fileName = "World_", menuName = "PipeMuzzle/World Definition")]
    public class WorldDefinition : ScriptableObject
    {
        [SerializeField] private WorldId worldId;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int levelCount = 12;
        [SerializeField] private ComicStoryDefinition story;
        [SerializeField] private WorldLevelSelectTheme levelSelectTheme;
        [SerializeField] private LevelPathLayoutDefinition levelPathLayout;

        public WorldId WorldId => worldId;
        public string DisplayName => displayName;
        public int LevelCount => levelCount;
        public ComicStoryDefinition Story => story;
        public WorldLevelSelectTheme LevelSelectTheme => levelSelectTheme;
        public LevelPathLayoutDefinition LevelPathLayout => levelPathLayout;
    }
}
