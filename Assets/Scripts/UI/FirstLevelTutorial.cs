using System.Linq;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using PipeMuzzle.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class FirstLevelTutorial : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameController gameController;
        [SerializeField] private BoardView boardView;
        [SerializeField] private Camera gameplayCamera;

        [Header("Gentle guidance")]
        [SerializeField, Range(1f, 1.1f)] private float pulseScale = 1.05f;
        [SerializeField, Min(.1f)] private float pulsePeriod = 1.6f;
        [SerializeField, Min(.01f)] private float fadeDuration = .25f;

        private CanvasGroup visuals;
        private Image rotationRing, sourceRing, targetRing;
        private TMP_Text hint;
        private TileView highlightedTile, sourceTile, targetTile;
        private Vector3 originalScale;
        private Sprite ringSprite;
        private Texture2D ringTexture;
        private bool bound, guiding, fading;
        private float elapsed, fadeElapsed;

        public bool IsShowing => guiding || fading;
        public TileView HighlightedTile => highlightedTile;

        public void Configure(GameController controller, BoardView view, Camera camera)
        {
            Unbind();
            Clear();
            gameController = controller;
            boardView = view;
            gameplayCamera = camera;
            if (isActiveAndEnabled) { Bind(); BeginIfNeeded(); }
        }

        private void OnEnable() { Bind(); BeginIfNeeded(); }
        private void OnDisable() { Unbind(); Clear(); }
        private void Update()
        {
            if (gameController != null && gameController.IsPaused) return;
            Advance(Time.unscaledDeltaTime);
        }
        private void LateUpdate() { if (IsShowing) PositionVisuals(); }

        private void Bind()
        {
            if (bound || gameController == null) return;
            gameController.LevelLoaded += HandleLevelLoaded;
            gameController.MoveCountChanged += HandleMoveChanged;
            bound = true;
        }

        private void Unbind()
        {
            if (!bound || gameController == null) return;
            gameController.LevelLoaded -= HandleLevelLoaded;
            gameController.MoveCountChanged -= HandleMoveChanged;
            bound = false;
        }

        private bool IsFirstSakuraLevel() => gameController != null &&
            gameController.CurrentWorld != null && gameController.CurrentWorld.WorldId == WorldId.SakuraGarden &&
            gameController.CurrentLevelNumber == 1;

        private void HandleLevelLoaded(int number, int count) => BeginIfNeeded();

        private void BeginIfNeeded()
        {
            Clear();
            if (!IsFirstSakuraLevel() || BasicRotationTutorialProgress.HasSeen ||
                gameController.CurrentMoveCount != 0 || gameController.IsCompleted ||
                gameController.IsCompletionPending || boardView == null || gameplayCamera == null) return;

            TileView[] tiles = boardView.GetComponentsInChildren<TileView>();
            LevelDefinition level = gameController.CurrentLevelDefinition;
            highlightedTile = tiles.Where(t => t.State != null && t.State.Shape != TileShape.Empty &&
                    !t.State.IsLocked && t.State.Role == TileRole.Normal)
                .OrderBy(t => Mathf.Abs(t.State.X - (level.Width - 1) * .5f) +
                    Mathf.Abs(t.State.Y - (level.Height - 1) * .5f)).FirstOrDefault();
            if (highlightedTile == null) return;
            sourceTile = tiles.FirstOrDefault(t => t.State.Role == TileRole.Source);
            targetTile = tiles.FirstOrDefault(t => t.State.Role == TileRole.Target);
            originalScale = highlightedTile.transform.localScale;
            EnsureVisuals();
            visuals.alpha = 1f;
            visuals.gameObject.SetActive(true);
            guiding = true;
            PositionVisuals();
            Advance(0f);
        }

        private void HandleMoveChanged(int moves)
        {
            // The controller emits positive counts only after a successful rotation.
            if (!guiding || moves <= 0 || !IsFirstSakuraLevel()) return;
            BasicRotationTutorialProgress.MarkSeen();
            StopPulse();
            guiding = false;
            fading = true;
            fadeElapsed = 0f;
            sourceRing.gameObject.SetActive(false);
            targetRing.gameObject.SetActive(false);
        }

        private void Advance(float deltaTime)
        {
            if (!IsShowing) return;
            if (!IsFirstSakuraLevel() || highlightedTile == null) { Clear(); return; }
            if (fading)
            {
                fadeElapsed += Mathf.Max(0f, deltaTime);
                visuals.alpha = 1f - Mathf.SmoothStep(0f, 1f, fadeElapsed / fadeDuration);
                if (fadeElapsed >= fadeDuration) Clear();
                return;
            }

            elapsed += Mathf.Max(0f, deltaTime);
            float wave = (1f - Mathf.Cos(elapsed * Mathf.PI * 2f / pulsePeriod)) * .5f;
            float scale = Mathf.Lerp(1f, pulseScale, wave);
            // Rotation feedback owns this transform once a click has been accepted.
            if (!highlightedTile.HasPendingRotation) highlightedTile.transform.localScale = originalScale * scale;
            rotationRing.rectTransform.localScale = Vector3.one * scale;
            SetIntroPulse(sourceRing, elapsed, 0f);
            SetIntroPulse(targetRing, elapsed, .5f);
        }

        private static void SetIntroPulse(Image ring, float time, float start)
        {
            float phase = (time - start) / .5f;
            ring.gameObject.SetActive(phase >= 0f && phase < 1f);
            Color color = ring.color;
            color.a = Mathf.Sin(Mathf.Clamp01(phase) * Mathf.PI) * .24f;
            ring.color = color;
        }

        private void StopPulse()
        {
            // Do not overwrite the click squash/rotation coroutine on the selected pipe.
            if (highlightedTile != null && !highlightedTile.HasPendingRotation)
                highlightedTile.transform.localScale = originalScale;
        }

        private void Clear()
        {
            StopPulse();
            guiding = fading = false;
            elapsed = fadeElapsed = 0f;
            highlightedTile = sourceTile = targetTile = null;
            if (visuals != null) visuals.gameObject.SetActive(false);
        }

        private void EnsureVisuals()
        {
            if (visuals != null) return;
            GameObject root = new("RotationTutorial", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            visuals = root.GetComponent<CanvasGroup>();
            visuals.blocksRaycasts = false;
            visuals.interactable = false;
            BuildRingSprite();
            rotationRing = CreateRing("RotationRing");
            sourceRing = CreateRing("SourceIntroRing");
            targetRing = CreateRing("TargetIntroRing");
            GameObject label = new("RotationHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(root.transform, false);
            hint = label.GetComponent<TMP_Text>();
            hint.text = "CLICK TO ROTATE";
            hint.fontSize = 20f;
            UiTypography.Apply(hint, UiFontRole.Label);
            hint.characterSpacing = 1.2f;
            hint.alignment = TextAlignmentOptions.Center;
            hint.color = new Color(.53f, .32f, .40f, .82f);
            hint.raycastTarget = false;
            hint.rectTransform.sizeDelta = new Vector2(260f, 32f);
        }

        private Image CreateRing(string name)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(visuals.transform, false);
            Image ring = root.GetComponent<Image>();
            ring.sprite = ringSprite;
            ring.color = new Color(.91f, .46f, .60f, .25f);
            ring.raycastTarget = false;
            return ring;
        }

        private void BuildRingSprite()
        {
            const int size = 96;
            ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "TutorialSoftRing" };
            Color[] pixels = new Color[size * size];
            Vector2 center = Vector2.one * (size - 1) * .5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float edge = Mathf.Abs(Vector2.Distance(new Vector2(x, y), center) - 42f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(2.2f - edge));
            }
            ringTexture.SetPixels(pixels);
            ringTexture.Apply(false, true);
            ringSprite = Sprite.Create(ringTexture, new Rect(0, 0, size, size), Vector2.one * .5f);
        }

        private void PositionVisuals()
        {
            if (highlightedTile == null || gameplayCamera == null || visuals == null) return;
            RectTransform parent = (RectTransform)visuals.transform;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!boardView.TryGetGridBounds(out Bounds bounds)) return;
            float cellSize = bounds.size.y / gameController.CurrentLevelDefinition.Height;
            Vector2 center = Project(highlightedTile.transform.position, parent, uiCamera);
            float cellPixels = Vector2.Distance(center,
                Project(highlightedTile.transform.position + Vector3.up * cellSize, parent, uiCamera));
            rotationRing.rectTransform.anchoredPosition = center;
            rotationRing.rectTransform.sizeDelta = Vector2.one * cellPixels * 1.04f;
            hint.rectTransform.anchoredPosition = center + Vector2.down * (cellPixels * .32f + 4f);
            PositionIntro(sourceRing, sourceTile, parent, uiCamera, cellPixels);
            PositionIntro(targetRing, targetTile, parent, uiCamera, cellPixels);
        }

        private Vector2 Project(Vector3 position, RectTransform parent, Camera uiCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                gameplayCamera.WorldToScreenPoint(position), uiCamera, out Vector2 local);
            return local;
        }

        private void PositionIntro(Image ring, TileView tile, RectTransform parent, Camera camera, float cellPixels)
        {
            if (tile == null) return;
            ring.rectTransform.anchoredPosition = Project(tile.transform.position, parent, camera);
            ring.rectTransform.sizeDelta = Vector2.one * cellPixels * .70f;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying) { Destroy(ringSprite); Destroy(ringTexture); }
            else { DestroyImmediate(ringSprite); DestroyImmediate(ringTexture); }
        }
    }
}
