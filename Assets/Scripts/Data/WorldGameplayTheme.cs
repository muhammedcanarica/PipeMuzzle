using UnityEngine;

namespace PipeMuzzle.Data
{
    [CreateAssetMenu(fileName = "WorldGameplayTheme", menuName = "Ruilay/World Gameplay Theme")]
    public sealed class WorldGameplayTheme : ScriptableObject
    {
        [Header("Gameplay")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color cameraBackgroundColor = Color.black;
        [SerializeField] private Color boardPanelColor = new(1f, 1f, 1f, .75f);

        [Header("Optional gameplay overrides")]
        [Tooltip("-1 inherits GameController's hint budget. Zero disables hints in this world.")]
        [SerializeField, Min(-1)] private int maxHintsPerLevel = -1;
        [SerializeField] private AudioClip pipeRotateClip;
        [SerializeField] private AudioClip levelCompleteClip;

        public int HintLimitOverride => maxHintsPerLevel;
        public AudioClip PipeRotateClip => pipeRotateClip;
        public AudioClip LevelCompleteClip => levelCompleteClip;

        [Header("HUD")]
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color panelColor = new(0f, 0f, 0f, .5f);
        [SerializeField] private Color primaryButtonColor = Color.white;
        [SerializeField] private Color secondaryButtonColor = Color.white;

        [Header("Tiles")]
        [SerializeField] private Sprite straightSprite;
        [SerializeField] private Sprite cornerSprite;
        [SerializeField] private Sprite threeWaySprite;
        [SerializeField] private Sprite crossSprite;
        [SerializeField] private Sprite sourceMarker;
        [SerializeField] private Sprite targetMarker;
        [Tooltip("Water inside the target basin, shown only when flow arrives.")]
        [SerializeField] private Sprite targetWater;
        [SerializeField] private Color flowColor = new(.2f, .85f, .95f, 1f);
        [SerializeField] private Color normalTint = Color.white;
        [SerializeField] private Color lockedTint = Color.gray;
        [SerializeField] private Color sourceGlowColor = Color.cyan;
        [SerializeField] private Color targetGlowColor = Color.cyan;
        [SerializeField] private Color poweredGlowColor = Color.cyan;

        public Sprite BackgroundSprite => backgroundSprite;
        public Color CameraBackgroundColor => cameraBackgroundColor;
        public Color BoardPanelColor => boardPanelColor;
        public Color TextColor => textColor;
        public Color PanelColor => panelColor;
        public Color PrimaryButtonColor => primaryButtonColor;
        public Color SecondaryButtonColor => secondaryButtonColor;
        public Sprite StraightSprite => straightSprite;
        public Sprite CornerSprite => cornerSprite;
        public Sprite ThreeWaySprite => threeWaySprite;
        public Sprite CrossSprite => crossSprite;
        public Sprite SourceMarker => sourceMarker;
        public Sprite TargetMarker => targetMarker;
        public Sprite TargetWater => targetWater;
        public Color FlowColor => flowColor;
        public Color NormalTint => normalTint;
        public Color LockedTint => lockedTint;
        public Color SourceGlowColor => sourceGlowColor;
        public Color TargetGlowColor => targetGlowColor;
        public Color PoweredGlowColor => poweredGlowColor;
    }
}
