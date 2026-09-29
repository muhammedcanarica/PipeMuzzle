using PipeMuzzle.Data;
using PipeMuzzle.View;
using UnityEngine;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class WorldPresentationController : MonoBehaviour
    {
        private const string BackgroundObjectName = "WorldGameplayBackground";
        private const string BoardPanelObjectName = "WorldGameplayBoardPanel";
        private const string BoardPanelBorderObjectName =
            "WorldGameplayBoardPanelBorder";
        private const float BackgroundOpacity = 1f;
        private const float BoardPanelPadding = 0.38f;
        private const float BoardPanelBorderPadding = 0.035f;
        private const int BoardPanelSortingOrder = -10;
        private const int BoardPanelBorderSortingOrder = -11;

        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer boardPanelRenderer;
        private SpriteRenderer boardPanelBorderRenderer;
        private Texture2D boardPanelTexture;
        private Sprite boardPanelSprite;
        private WorldGameplayTheme activeTheme;

        public WorldGameplayTheme ActiveTheme => activeTheme;

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (backgroundRenderer != null)
                FitBackgroundToCamera(camera, backgroundRenderer);

            UpdateBoardPanel(camera);
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
            UpdateBoardPanel(Camera.main);
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

        private void UpdateBoardPanel(Camera camera)
        {
            if (activeTheme == null || camera == null) return;

            BoardView boardView = FindFirstObjectByType<BoardView>();
            if (boardView == null || !boardView.TryGetGridBounds(out Bounds bounds))
            {
                if (boardPanelRenderer != null) boardPanelRenderer.enabled = false;
                if (boardPanelBorderRenderer != null)
                    boardPanelBorderRenderer.enabled = false;
                return;
            }

            boardPanelRenderer = FindOrCreateBoardPanelRenderer(
                camera,
                BoardPanelObjectName,
                BoardPanelSortingOrder
            );
            boardPanelBorderRenderer = FindOrCreateBoardPanelRenderer(
                camera,
                BoardPanelBorderObjectName,
                BoardPanelBorderSortingOrder
            );
            boardPanelRenderer.enabled = true;
            boardPanelRenderer.color = activeTheme.BoardPanelColor;
            Vector3 panelPosition = new(
                bounds.center.x - camera.transform.position.x,
                bounds.center.y - camera.transform.position.y,
                bounds.center.z - camera.transform.position.z
            );
            Vector3 panelScale = new(
                bounds.size.x + (BoardPanelPadding * 2f),
                bounds.size.y + (BoardPanelPadding * 2f),
                1f
            );
            boardPanelRenderer.transform.localPosition = panelPosition;
            boardPanelRenderer.transform.localScale = panelScale;
            boardPanelBorderRenderer.enabled = true;
            boardPanelBorderRenderer.color = GetBoardPanelBorderColor();
            boardPanelBorderRenderer.transform.localPosition = panelPosition;
            boardPanelBorderRenderer.transform.localScale = panelScale +
                new Vector3(
                    BoardPanelBorderPadding * 2f,
                    BoardPanelBorderPadding * 2f,
                    0f
                );
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

        private SpriteRenderer FindOrCreateBoardPanelRenderer(
            Camera camera,
            string objectName,
            int sortingOrder)
        {
            Transform existing = camera.transform.Find(objectName);
            SpriteRenderer renderer = existing != null
                ? existing.GetComponent<SpriteRenderer>()
                : null;

            if (renderer == null)
            {
                GameObject panel = new(objectName);
                renderer = panel.AddComponent<SpriteRenderer>();
            }

            Transform panelTransform = renderer.transform;
            panelTransform.SetParent(camera.transform, false);
            panelTransform.localRotation = Quaternion.identity;
            renderer.sprite = GetBoardPanelSprite();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private Color GetBoardPanelBorderColor()
        {
            Color color = Color.Lerp(
                activeTheme.BoardPanelColor,
                activeTheme.TextColor,
                .16f
            );
            color.a = Mathf.Min(1f, activeTheme.BoardPanelColor.a + .06f);
            return color;
        }

        private Sprite GetBoardPanelSprite()
        {
            if (boardPanelSprite != null) return boardPanelSprite;

            const int textureSize = 128;
            const float radius = .16f;
            const float softness = 1f / textureSize;
            boardPanelTexture = new Texture2D(textureSize, textureSize)
            {
                name = "Rounded Board Panel Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 point = new(
                        ((x + .5f) / textureSize) - .5f,
                        ((y + .5f) / textureSize) - .5f
                    );
                    Vector2 corner = new(
                        Mathf.Max(Mathf.Abs(point.x) - (.5f - radius), 0f),
                        Mathf.Max(Mathf.Abs(point.y) - (.5f - radius), 0f)
                    );
                    float distance = corner.magnitude - radius;
                    float alpha = Mathf.Clamp01(.5f - (distance / softness));
                    pixels[(y * textureSize) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            boardPanelTexture.SetPixels(pixels);
            boardPanelTexture.Apply(false);
            boardPanelSprite = Sprite.Create(
                boardPanelTexture,
                new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(.5f, .5f),
                textureSize
            );
            boardPanelSprite.name = "Rounded Board Panel Sprite";
            boardPanelSprite.hideFlags = HideFlags.HideAndDontSave;
            return boardPanelSprite;
        }

        private void OnDestroy()
        {
            if (boardPanelSprite != null) Destroy(boardPanelSprite);
            if (boardPanelTexture != null) Destroy(boardPanelTexture);
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
