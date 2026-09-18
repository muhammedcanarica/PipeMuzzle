using PipeMuzzle.Data;
using PipeMuzzle.View;
using UnityEngine;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class WorldPresentationController : MonoBehaviour
    {
        private SpriteRenderer backgroundRenderer;
        private WorldGameplayTheme activeTheme;

        public WorldGameplayTheme ActiveTheme => activeTheme;

        public void Configure(WorldDefinition world)
        {
            activeTheme = world != null ? world.GameplayTheme : null;
            if (activeTheme == null) return;

            BoardView boardView = FindFirstObjectByType<BoardView>();
            if (boardView != null) boardView.SetGameplayTheme(activeTheme);

            GameUI gameUi = GetComponent<GameUI>();
            if (gameUi != null) gameUi.ApplyTheme(activeTheme);

            ApplyBackground(activeTheme);
        }

        private void ApplyBackground(WorldGameplayTheme theme)
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            camera.backgroundColor = theme.CameraBackgroundColor;

            if (backgroundRenderer == null)
            {
                GameObject background = new GameObject("WorldGameplayBackground");
                background.transform.SetParent(camera.transform, false);
                background.transform.localPosition = new Vector3(0f, 0f, 20f);
                backgroundRenderer = background.AddComponent<SpriteRenderer>();
                backgroundRenderer.sortingOrder = -1000;
            }

            backgroundRenderer.sprite = theme.BackgroundSprite;
        }
    }
}
