using System.Collections.Generic;
using UnityEngine;

namespace PipeMuzzle.Data
{
    [CreateAssetMenu(fileName = "WorldLevelSelectTheme", menuName = "PipeMuzzle/World Level Select Theme")]
    public sealed class WorldLevelSelectTheme : ScriptableObject
    {
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite normalNodeSprite;
        [SerializeField] private Sprite lockedNodeSprite;
        [SerializeField] private Sprite completedNodeSprite;
        [SerializeField] private Sprite routeSegmentSprite;
        [SerializeField] private Sprite[] decorativeSprites;
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color routeColor = Color.white;
        [SerializeField] private Color unlockedTextColor = Color.white;
        [SerializeField] private Color lockedTextColor = Color.gray;

        public Sprite BackgroundSprite => backgroundSprite;
        public Sprite NormalNodeSprite => normalNodeSprite;
        public Sprite LockedNodeSprite => lockedNodeSprite;
        public Sprite CompletedNodeSprite => completedNodeSprite;
        public Sprite RouteSegmentSprite => routeSegmentSprite;
        public IReadOnlyList<Sprite> DecorativeSprites => decorativeSprites;
        public Color TitleColor => titleColor;
        public Color RouteColor => routeColor;
        public Color UnlockedTextColor => unlockedTextColor;
        public Color LockedTextColor => lockedTextColor;
    }
}
