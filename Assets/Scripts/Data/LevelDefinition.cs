using System.Collections.Generic;
using UnityEngine;

namespace PipeMuzzle.Data
{
    [CreateAssetMenu(
            fileName = "Level_",
            menuName = "Ruilay/Level Definition"
    )]

    public class LevelDefinition : ScriptableObject
    {
        [Min(1)]
        [SerializeField] private int width = 3;

        [Min(1)]
        [SerializeField] private int height = 3;

        [SerializeField] private List<TileDefinition> tiles = new();

        // Source-to-target order, baked by the existing Editor validator.
        [SerializeField] private List<LevelSolutionStep> solutionPath = new();

        public IReadOnlyList<LevelSolutionStep> SolutionPath => solutionPath;
        public int Width => width;
        public int Height => height;
        public IReadOnlyList<TileDefinition> Tiles => tiles;
    }

    [System.Serializable]
    public sealed class LevelSolutionStep
    {
        [SerializeField] private Vector2Int position;
        [SerializeField, Range(0, 3)] private int rotation;
        public Vector2Int Position => position;
        public int Rotation => rotation;
    }
}
