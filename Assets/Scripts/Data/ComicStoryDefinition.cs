using System.Collections.Generic;
using UnityEngine;

namespace PipeMuzzle.Data
{
    [CreateAssetMenu(fileName = "ComicStory_", menuName = "PipeMuzzle/Comic Story")]
    public class ComicStoryDefinition : ScriptableObject
    {
        [SerializeField] private string storyId;
        [SerializeField] private Sprite[] panels;

        public string StoryId => storyId;
        public IReadOnlyList<Sprite> Panels => panels;
        public int PanelCount => panels?.Length ?? 0;
    }
}
