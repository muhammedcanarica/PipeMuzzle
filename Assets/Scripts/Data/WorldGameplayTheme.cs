using UnityEngine;

namespace PipeMuzzle.Data
{
    [CreateAssetMenu(fileName = "WorldGameplayTheme", menuName = "PipeMuzzle/World Gameplay Theme")]
    public sealed class WorldGameplayTheme : ScriptableObject
    {
        [Header("Gameplay")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color cameraBackgroundColor = Color.black;

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
        [SerializeField] private Color normalTint = Color.white;
        [SerializeField] private Color lockedTint = Color.gray;
        [SerializeField] private Color sourceGlowColor = Color.cyan;
        [SerializeField] private Color targetGlowColor = Color.cyan;
        [SerializeField] private Color poweredGlowColor = Color.cyan;

        public Sprite BackgroundSprite => backgroundSprite;
        public Color CameraBackgroundColor => cameraBackgroundColor;
        public Color TextColor => textColor;
        public Color PanelColor => panelColor;
        public Color PrimaryButtonColor => primaryButtonColor;
        public Color SecondaryButtonColor => secondaryButtonColor;
        public Sprite StraightSprite => straightSprite;
        public Sprite CornerSprite => cornerSprite;
        public Sprite ThreeWaySprite => threeWaySprite;
        public Sprite CrossSprite => crossSprite;
        public Color NormalTint => normalTint;
        public Color LockedTint => lockedTint;
        public Color SourceGlowColor => sourceGlowColor;
        public Color TargetGlowColor => targetGlowColor;
        public Color PoweredGlowColor => poweredGlowColor;
    }
}
