using System.Collections.Generic;
using UnityEngine;

namespace PipeMuzzle.Data
{
    [CreateAssetMenu(
            fileName = "Level_",
            menuName = "PipeMuzzle/Level Definition"
    )]

    public class LevelDefinition : ScriptableObject
    {
        [Min(1)]
        [SerializeField] private int width = 3;

        [Min(1)]
        [SerializeField] private int height = 3;

        [SerializeField] private List<TileDefinition> tiles = new();

        public int Width => width;
        public int Height => height;
        public IReadOnlyList<TileDefinition> Tiles => tiles;
    }
}
