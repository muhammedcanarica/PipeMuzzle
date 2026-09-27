using PipeMuzzle.Data;
using PipeMuzzle.View;
using UnityEngine;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class WorldPresentationController : MonoBehaviour
    {
        private const string BackgroundObjectName = "WorldGameplayBackground";
        private const float BackgroundOpacity = 0.74f;

        private SpriteRenderer backgroundRenderer;
        private WorldGameplayTheme activeTheme;

        public WorldGameplayTheme ActiveTheme => activeTheme;

        private void LateUpdate()
        {
            if (backgroundRenderer == null) return;
            FitBackgroundToCamera(Camera.main, backgroundRenderer);
        }

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

            backgroundRenderer = FindOrCreateBackgroundRenderer(camera);

            backgroundRenderer.sprite = theme.BackgroundSprite;
            backgroundRenderer.color = new Color(1f, 1f, 1f, BackgroundOpacity);
            FitBackgroundToCamera(camera, backgroundRenderer);
        }

        private static void FitBackgroundToCamera(
            Camera camera,
            SpriteRenderer renderer
        )
        {
            if (camera == null ||
                renderer == null ||
                renderer.sprite == null ||
                !camera.orthographic)
            {
                return;
            }

            Vector2 spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

            float worldHeight = camera.orthographicSize * 2f;
            float worldWidth = worldHeight * camera.aspect;
            float scale = Mathf.Max(
                worldWidth / spriteSize.x,
                worldHeight / spriteSize.y
            );

            renderer.transform.localScale = new Vector3(scale, scale, 1f);
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

                DestroyBackground(child.gameObject);
            }

            if (existingRenderer == null)
            {
                GameObject background = new(BackgroundObjectName);
                existingRenderer = background.AddComponent<SpriteRenderer>();
            }

            Transform backgroundTransform = existingRenderer.transform;
            backgroundTransform.SetParent(camera.transform, false);
            backgroundTransform.localPosition = new Vector3(0f, 0f, 20f);
            backgroundTransform.localRotation = Quaternion.identity;
            existingRenderer.sortingOrder = -1000;
            return existingRenderer;
        }

        private static void DestroyBackground(GameObject background)
        {
            if (Application.isPlaying)
            {
                Destroy(background);
            }
            else
            {
                DestroyImmediate(background);
            }
        }
    }
}
