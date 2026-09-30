using System;
using UnityEngine;

namespace PipeMuzzle.Data
{
    [Serializable]
    public sealed class StoryCheckpoint
    {
        [SerializeField] private int completedLevelNumber;
        [SerializeField] private ComicStoryDefinition story;

        // Zero is world entry; positive values are completed level numbers.
        public int CompletedLevelNumber => completedLevelNumber;
        public ComicStoryDefinition Story => story;
    }
}
