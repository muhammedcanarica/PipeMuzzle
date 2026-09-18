using PipeMuzzle.Data;
using PipeMuzzle.View;
using UnityEngine;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class WorldPresentationController : MonoBehaviour
    {
        private const string BackgroundObjectName = "WorldGameplayBackground";

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
                backgroundRenderer = FindOrCreateBackgroundRenderer(camera);
            }

            backgroundRenderer.sprite = theme.BackgroundSprite;
        }

        private static SpriteRenderer FindOrCreateBackgroundRenderer(Camera camera)
        {
            SpriteRenderer existingRenderer = null;

            for (int index = camera.transform.childCount - 1; index >= 0; index--)
            {
                Transform child = camera.transform.GetChild(index);
                if (child.name != BackgroundObjectName) continue;

                SpriteRenderer candidate = child.GetComponent<SpriteRenderer>();
                if (existingRenderer == null && candidate != null)
                {
                    existingRenderer = candidate;
                    continue;
                }

                Destroy(child.gameObject);
            }

            if (existingRenderer != null) return existingRenderer;

            GameObject background = new(BackgroundObjectName);
            background.transform.SetParent(camera.transform, false);
            background.transform.localPosition = new Vector3(0f, 0f, 20f);
            SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = -1000;
            return renderer;
        }
    }
}
