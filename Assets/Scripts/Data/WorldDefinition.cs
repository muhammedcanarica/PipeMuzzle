using System.Collections.Generic;
using System.Linq;
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
        public const int ExpectedLevelCount = 12;

        [SerializeField] private WorldId worldId;
        [SerializeField] private string displayName;
        [SerializeField] private List<LevelDefinition> levels = new();
        [SerializeField] private ComicStoryDefinition story;
        [SerializeField] private WorldLevelSelectTheme levelSelectTheme;
        [SerializeField] private LevelPathLayoutDefinition levelPathLayout;
        [SerializeField] private WorldGameplayTheme gameplayTheme;

        public WorldId WorldId => worldId;
        public string DisplayName => displayName;
        public IReadOnlyList<LevelDefinition> Levels => levels;
        public int LevelCount => levels?.Count ?? 0;
        public bool IsContentReady => LevelCount == ExpectedLevelCount &&
            levels.All(level => level != null) &&
            levels.Distinct().Count() == ExpectedLevelCount;
        public ComicStoryDefinition Story => story;
        public WorldLevelSelectTheme LevelSelectTheme => levelSelectTheme;
        public LevelPathLayoutDefinition LevelPathLayout => levelPathLayout;
        public WorldGameplayTheme GameplayTheme => gameplayTheme;

        public void SetGameplayTheme(WorldGameplayTheme theme) => gameplayTheme = theme;
    }
}
