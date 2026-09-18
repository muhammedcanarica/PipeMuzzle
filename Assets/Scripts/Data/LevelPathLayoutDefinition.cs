using UnityEngine;

namespace PipeMuzzle.Data
{
    [CreateAssetMenu(fileName = "LevelPathLayout", menuName = "PipeMuzzle/Level Path Layout")]
    public sealed class LevelPathLayoutDefinition : ScriptableObject
    {
        [SerializeField] private Vector2[] nodePositions = new Vector2[12];

        public int NodeCount => nodePositions?.Length ?? 0;
        public bool HasValidNodeCount => NodeCount == 12;
        public Vector2 GetNodePosition(int index) => nodePositions[index];
    }
}
