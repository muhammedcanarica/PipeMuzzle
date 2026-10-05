using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    public class GameUI : MonoBehaviour
    {
        private static readonly Color32 Charcoal = new(54, 50, 61, 255);
        private static readonly Color32 Ivory = new(255, 250, 244, 255);

        [Header("References")]
        [SerializeField] private GameController gameController;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text moveCountText;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text completionText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button nextButton;

        private TMP_Text completionMoveCountText;
        private Button completionRestartButton;
        private Button completionMapButton;
        private Button levelsButton;
        private Button hintButton;
        private GameplayPauseUI pauseUi;
        private Image levelSurface;
        private Image movesSurface;
        private Image completionCard;
        private WorldGameplayTheme activeTheme;
        private Sprite surfaceSprite;
        private Sprite secondaryButtonSprite;
        private Sprite primaryButtonSprite;
        private Sprite completionCardSprite;
        private Texture2D surfaceTexture;
        private Texture2D secondaryButtonTexture;
        private Texture2D primaryButtonTexture;
        private Texture2D completionCardTexture;
        private bool isBound;

        public void ApplyTheme(WorldGameplayTheme theme)
        {
            if (theme == null) return;

            activeTheme = theme;
            EnsureCompletionControls();
            EnsurePresentationObjects();
            if (HasRequiredReferences()) EnsurePauseControls();
            BuildThemeSprites(theme);
            ApplyLayout();

            StyleText(levelText, 34f, Charcoal, FontStyles.Bold);
            StyleText(moveCountText, 25f, Charcoal, FontStyles.Normal);
            StyleText(completionText, 42f, Charcoal, FontStyles.Bold);
            StyleText(completionMoveCountText, 27f, Charcoal, FontStyles.Normal);

            ConfigureSurface(levelSurface, surfaceSprite);
            ConfigureSurface(movesSurface, surfaceSprite);

            if (completionPanel != null)
            {
                Image overlay = completionPanel.GetComponent<Image>();
                if (overlay != null)
                {
                    overlay.sprite = null;
                    overlay.type = Image.Type.Simple;
                    overlay.color = new Color(0.08f, 0.08f, 0.12f, 0.38f);
                }
            }

            if (completionCard != null)
            {
                completionCard.sprite = completionCardSprite;
                completionCard.type = Image.Type.Sliced;
                completionCard.color = Color.white;
                completionCard.raycastTarget = false;
                Shadow shadow = completionCard.GetComponent<Shadow>();
                shadow.effectColor = new Color(0.12f, 0.1f, 0.15f, 0.16f);
                shadow.effectDistance = new Vector2(0f, -4f);
                shadow.useGraphicAlpha = true;
            }

            ApplyButtonStyle(levelsButton, false, theme);
            ApplyButtonStyle(restartButton, false, theme);
            ApplyButtonStyle(hintButton, false, theme);
            ApplyButtonStyle(completionRestartButton, false, theme);
            ApplyButtonStyle(completionMapButton, false, theme);
            ApplyButtonStyle(nextButton, true, theme);
            pauseUi?.ApplyTheme(theme, completionCardSprite, secondaryButtonSprite);
        }

        private void OnEnable()
        {
            EnsureCompletionControls();
            EnsurePresentationObjects();

            if (!HasRequiredReferences())
            {
                Debug.LogError("GameUI has missing Inspector references.", this);
                enabled = false;
                return;
            }

            EnsurePauseControls();
            Bind();
            ApplyLayout();
            RefreshFromCurrentState();
            if (activeTheme != null) ApplyTheme(activeTheme);
        }

        private void EnsurePauseControls()
        {
            // GameUI lives on Canvas; pause must follow the HUD's screen lifecycle.
            GameObject hud = restartButton.transform.parent.gameObject;
            if (pauseUi == null) pauseUi = hud.GetComponent<GameplayPauseUI>();
            if (pauseUi == null) pauseUi = hud.AddComponent<GameplayPauseUI>();
            ScreenManager screens = GetComponent<ScreenManager>() ?? GetComponentInParent<ScreenManager>(true);
            pauseUi.Initialize(gameController, restartButton, levelsButton, screens);
        }

        private void EnsureCompletionControls()
        {
            if (completionPanel == null) return;

            if (completionMoveCountText == null && moveCountText != null)
            {
                Transform existing = completionPanel.transform.Find(
                    "CompletionMoveCountText"
                );
                completionMoveCountText = existing != null
                    ? existing.GetComponent<TMP_Text>()
                    : Instantiate(moveCountText, completionPanel.transform);
                completionMoveCountText.name = "CompletionMoveCountText";
                completionMoveCountText.alignment = TextAlignmentOptions.Center;
            }

            if (completionRestartButton == null && restartButton != null)
            {
                Transform existing = completionPanel.transform.Find(
                    "CompletionRestartButton"
                );
                completionRestartButton = existing != null
                    ? existing.GetComponent<Button>()
                    : Instantiate(restartButton, completionPanel.transform);
                completionRestartButton.name = "CompletionRestartButton";
            }

            if (completionMapButton == null && restartButton != null)
            {
                Transform existing = completionPanel.transform.Find(
                    "CompletionMapButton"
                );
                completionMapButton = existing != null
                    ? existing.GetComponent<Button>()
                    : Instantiate(restartButton, completionPanel.transform);
                completionMapButton.name = "CompletionMapButton";
            }

            SetButtonLabel(completionRestartButton, "REPLAY");
            SetButtonLabel(completionMapButton, "BACK TO MAP");
        }

        private void EnsurePresentationObjects()
        {
            if (levelsButton == null)
            {
                Transform levels = FindDeepChild(transform, "LevelsButton");
                if (levels != null) levelsButton = levels.GetComponent<Button>();
            }

            levelSurface = EnsureBacking(levelText, "LevelSurface");
            movesSurface = EnsureBacking(moveCountText, "MovesSurface");

            if (hintButton == null && restartButton != null)
            {
                Transform parent = restartButton.transform.parent;
                Transform hint = parent.Find("HintButton");
                hintButton = hint != null ? hint.GetComponent<Button>() : Instantiate(restartButton, parent);
                hintButton.name = "HintButton";
                // Cloning a serialized button must not copy its gameplay action.
                hintButton.onClick = new Button.ButtonClickedEvent();
                SetButtonLabel(hintButton, "HINT 3/3");
            }
            if (hintButton != null)
            {
                GameplayHintUI action = hintButton.GetComponent<GameplayHintUI>();
                if (action == null) action = hintButton.gameObject.AddComponent<GameplayHintUI>();
                action.Configure(gameController);
            }

            if (completionPanel == null || completionCard != null) return;
            Transform existing = completionPanel.transform.Find("CompletionCard");
            if (existing == null)
            {
                GameObject card = new(
                    "CompletionCard",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Shadow)
                );
                card.transform.SetParent(completionPanel.transform, false);
                existing = card.transform;
            }

            completionCard = existing.GetComponent<Image>();
            existing.SetSiblingIndex(0);
        }

        private void ApplyLayout()
        {
            ConfigureRect(Rect(hintButton), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(32f, -92f), new Vector2(156f, 48f));
            ConfigureRect(
                Rect(levelsButton),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(32f, -32f),
                new Vector2(156f, 52f)
            );
            ConfigureRect(
                levelText != null ? levelText.rectTransform : null,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -26f),
                new Vector2(210f, 76f)
            );
            MatchBacking(levelSurface, levelText, new Vector2(24f, 8f));
            ConfigureRect(
                moveCountText != null ? moveCountText.rectTransform : null,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-32f, -30f),
                new Vector2(156f, 50f)
            );
            MatchBacking(movesSurface, moveCountText, new Vector2(16f, 6f));
            ConfigureRect(
                Rect(restartButton),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-32f, -92f),
                new Vector2(156f, 48f)
            );
            ConfigureRect(
                completionCard != null ? completionCard.rectTransform : null,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(560f, 360f)
            );
            ConfigureRect(
                completionText != null ? completionText.rectTransform : null,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 92f),
                new Vector2(480f, 100f)
            );
            ConfigureRect(
                completionMoveCountText != null
                    ? completionMoveCountText.rectTransform
                    : null,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 15f),
                new Vector2(420f, 84f)
            );
            ConfigureRect(
                Rect(completionRestartButton),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-118f, -82f),
                new Vector2(200f, 54f)
            );
            ConfigureRect(
                Rect(completionMapButton),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(118f, -82f),
                new Vector2(200f, 54f)
            );
            ConfigureRect(
                Rect(nextButton),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(118f, -82f),
                new Vector2(200f, 54f)
            );
        }

        private static RectTransform Rect(Button button) =>
            button != null ? button.GetComponent<RectTransform>() : null;

        private void BuildThemeSprites(WorldGameplayTheme theme)
        {
            DisposeThemeSprites();
            Color accent = theme.PrimaryButtonColor;
            Color border = new(accent.r, accent.g, accent.b, 0.48f);
            Color surface = new(Ivory.r / 255f, Ivory.g / 255f, Ivory.b / 255f, 0.91f);

            surfaceSprite = CreateRoundedSprite(
                "Gameplay Paper Surface",
                surface,
                border,
                9,
                1.5f,
                out surfaceTexture
            );
            secondaryButtonSprite = CreateRoundedSprite(
                "Gameplay Secondary Button",
                Color.white,
                border,
                8,
                1.5f,
                out secondaryButtonTexture
            );
            primaryButtonSprite = CreateRoundedSprite(
                "Gameplay Primary Button",
                Color.white,
                new Color(
                    accent.r * 0.72f,
                    accent.g * 0.72f,
                    accent.b * 0.72f,
                    1f
                ),
                8,
                1.5f,
                out primaryButtonTexture
            );
            completionCardSprite = CreateRoundedSprite(
                "Gameplay Completion Card",
                new Color(1f, 0.985f, 0.96f, 0.99f),
                border,
                10,
                1.5f,
                out completionCardTexture
            );
        }

        private void ApplyButtonStyle(Button button, bool primary, WorldGameplayTheme theme)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = primary ? primaryButtonSprite : secondaryButtonSprite;
                image.type = Image.Type.Sliced;
                image.color = primary
                    ? theme.PrimaryButtonColor
                    : QuietSecondary(theme.SecondaryButtonColor);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, theme.PrimaryButtonColor, 0.12f);
            colors.pressedColor = Color.Lerp(Color.white, theme.PrimaryButtonColor, 0.24f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.72f, 0.7f, 0.7f, 0.58f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = image;

            Shadow shadow = button.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = new Color(0f, 0f, 0f, 0.08f);
                shadow.effectDistance = new Vector2(0f, -2f);
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            StyleText(label, primary ? 25f : 23f, primary ? Color.white : (Color)Charcoal, FontStyles.Normal);
        }

        private static Color QuietSecondary(Color worldColor)
        {
            Color color = Color.Lerp((Color)Ivory, worldColor, 0.16f);
            color.a = 0.96f;
            return color;
        }

        private static void ConfigureSurface(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static Image EnsureBacking(TMP_Text target, string name)
        {
            if (target == null) return null;
            Transform existing = target.transform.parent.Find(name);
            if (existing == null)
            {
                GameObject backing = new(name, typeof(RectTransform), typeof(Image));
                backing.transform.SetParent(target.transform.parent, false);
                existing = backing.transform;
            }

            Image image = existing.GetComponent<Image>();
            image.raycastTarget = false;
            if (existing.GetSiblingIndex() + 1 !=
                target.transform.GetSiblingIndex())
            {
                existing.SetSiblingIndex(
                    target.transform.GetSiblingIndex()
                );
                target.transform.SetSiblingIndex(
                    existing.GetSiblingIndex() + 1
                );
            }
            return image;
        }

        private static void MatchBacking(Image backing, TMP_Text target, Vector2 padding)
        {
            if (backing == null || target == null) return;
            RectTransform targetRect = target.rectTransform;
            RectTransform backingRect = backing.rectTransform;
            backingRect.anchorMin = targetRect.anchorMin;
            backingRect.anchorMax = targetRect.anchorMax;
            backingRect.pivot = targetRect.pivot;
            backingRect.anchoredPosition = targetRect.anchoredPosition;
            backingRect.sizeDelta = targetRect.sizeDelta + padding;
            backingRect.localScale = Vector3.one;
        }

        private static void ConfigureRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void StyleText(TMP_Text text, float maxSize, Color color, FontStyles style)
        {
            if (text == null) return;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = maxSize;
            text.characterSpacing = 0.5f;
            text.raycastTarget = false;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            TMP_Text label = button != null
                ? button.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (label != null) label.text = value;
        }

        private bool HasRequiredReferences() =>
            gameController != null && levelText != null && moveCountText != null &&
            completionPanel != null && completionText != null &&
            completionMoveCountText != null && restartButton != null &&
            nextButton != null && completionRestartButton != null &&
            completionMapButton != null;

        private void Bind()
        {
            if (isBound) return;
            gameController.LevelLoaded += HandleLevelLoaded;
            gameController.LevelCompleted += HandleLevelCompleted;
            gameController.MoveCountChanged += HandleMoveCountChanged;
            restartButton.onClick.AddListener(gameController.RestartLevel);
            completionRestartButton.onClick.AddListener(gameController.RestartLevel);
            completionMapButton.onClick.AddListener(ReturnToWorldMap);
            isBound = true;
        }

        private void RefreshFromCurrentState()
        {
            int levelNumber = gameController.CurrentLevelNumber;
            if (levelNumber > 0) SetLevelText(levelNumber);
            HandleMoveCountChanged(gameController.CurrentMoveCount);
            if (gameController.IsCompleted)
                HandleLevelCompleted(gameController.HasNextLevel);
            else
                completionPanel.SetActive(false);
        }

        private void HandleMoveCountChanged(int moveCount)
        {
            moveCountText.text = $"Moves  {moveCount}";
            completionMoveCountText.text = $"Moves: {moveCount}";
        }

        private void HandleLevelLoaded(int levelNumber, int _)
        {
            SetLevelText(levelNumber);
            completionPanel.SetActive(false);
        }

        private void SetLevelText(int levelNumber)
        {
            levelText.text = $"<size=17><color=#77717C>LEVEL</color></size>\n<size=34>{levelNumber:00}</size>";
        }

        private void HandleLevelCompleted(bool hasNextLevel)
        {
            StoryNavigationCoordinator navigation = GetComponent<StoryNavigationCoordinator>() ??
                GetComponentInParent<StoryNavigationCoordinator>(true);
            if (navigation != null && navigation.TryPlayLevelCheckpoint(gameController.CurrentWorld,
                    gameController.CurrentLevelNumber, () => ShowCompletion(hasNextLevel))) return;
            ShowCompletion(hasNextLevel);
        }

        private void ShowCompletion(bool hasNextLevel)
        {
            int? best = gameController.CurrentWorld != null
                ? BestMovesProgress.GetBest(gameController.CurrentWorld.WorldId, gameController.CurrentLevelNumber) : null;
            string record = best.HasValue ? best.Value.ToString() : "--";
            string accent = activeTheme != null ? ColorUtility.ToHtmlStringRGB(activeTheme.PrimaryButtonColor) : "996677";
            completionMoveCountText.text = $"MOVES  {gameController.CurrentMoveCount}\n<size=21>BEST  {record}</size>" +
                (gameController.WasNewBest ? $"\n<size=15><color=#{accent}>NEW BEST</color></size>" : "");
            completionPanel.SetActive(true);
            bool worldComplete = !hasNextLevel && gameController.CurrentWorld != null;
            WorldDefinition nextWorld = worldComplete ? FindNextWorld() : null;
            completionText.text = worldComplete
                ? $"WORLD COMPLETE\n<size=24>{gameController.CurrentWorld.DisplayName} Complete</size>"
                : hasNextLevel ? "LEVEL COMPLETE" : "ALL LEVELS COMPLETE";
            nextButton.gameObject.SetActive(hasNextLevel || nextWorld != null);
            completionMapButton.gameObject.SetActive(worldComplete);
            nextButton.onClick.RemoveListener(gameController.LoadNextLevel);
            nextButton.onClick.RemoveListener(OpenNextWorld);
            if (hasNextLevel)
            {
                SetButtonLabel(nextButton, "NEXT");
                nextButton.onClick.AddListener(gameController.LoadNextLevel);
            }
            else if (nextWorld != null)
            {
                SetButtonLabel(nextButton, "NEXT WORLD");
                nextButton.onClick.AddListener(OpenNextWorld);
            }
        }

        private void ReturnToWorldMap()
        {
            gameController.CancelTransientVisuals();
            ScreenManager screens = GetComponent<ScreenManager>() ??
                GetComponentInParent<ScreenManager>(true);
            if (screens == null)
            {
                Debug.LogWarning("GameUI cannot return to the world map without a ScreenManager.", this);
                return;
            }
            screens.ShowWorldMap();
        }

        private void OpenNextWorld()
        {
            WorldDefinition nextWorld = FindNextWorld();
            StoryNavigationCoordinator navigation =
                GetComponent<StoryNavigationCoordinator>() ??
                GetComponentInParent<StoryNavigationCoordinator>(true);
            if (nextWorld == null || navigation == null)
            {
                ReturnToWorldMap();
                return;
            }
            navigation.OpenWorld(nextWorld);
        }

        private WorldDefinition FindNextWorld()
        {
            if (gameController.CurrentWorld == null) return null;
            WorldId? nextId = gameController.CurrentWorld.WorldId switch
            {
                WorldId.SakuraGarden => WorldId.BambooWorkshop,
                WorldId.BambooWorkshop => WorldId.MoonShrine,
                _ => null
            };
            if (!nextId.HasValue) return null;
            foreach (WorldDefinition world in Resources.LoadAll<WorldDefinition>("Worlds"))
            {
                if (world != null && world.WorldId == nextId.Value && world.IsContentReady)
                    return world;
            }
            return null;
        }

        private void OnDisable()
        {
            if (!isBound) return;
            gameController.LevelLoaded -= HandleLevelLoaded;
            gameController.LevelCompleted -= HandleLevelCompleted;
            gameController.MoveCountChanged -= HandleMoveCountChanged;
            restartButton.onClick.RemoveListener(gameController.RestartLevel);
            nextButton.onClick.RemoveListener(gameController.LoadNextLevel);
            nextButton.onClick.RemoveListener(OpenNextWorld);
            completionRestartButton.onClick.RemoveListener(gameController.RestartLevel);
            completionMapButton.onClick.RemoveListener(ReturnToWorldMap);
            isBound = false;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform result = FindDeepChild(parent.GetChild(index), name);
                if (result != null) return result;
            }
            return null;
        }

        private static Sprite CreateRoundedSprite(string name, Color fill, Color border, int radius, float borderWidth, out Texture2D texture)
        {
            const int size = 64;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = $"{name} Texture";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            Vector2 half = center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 offset = new(Mathf.Abs(x - center.x), Mathf.Abs(y - center.y));
                    Vector2 corner = new(
                        Mathf.Max(offset.x - (half.x - radius), 0f),
                        Mathf.Max(offset.y - (half.y - radius), 0f)
                    );
                    float signedDistance = corner.magnitude - radius;
                    float outerAlpha = Mathf.Clamp01(0.75f - signedDistance);
                    float innerAlpha = Mathf.Clamp01(0.75f - signedDistance - borderWidth);
                    Color pixel = Color.Lerp(border, fill, innerAlpha);
                    pixel.a *= outerAlpha;
                    pixels[y * size + x] = pixel;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.name = name;
            return sprite;
        }

        private void DisposeThemeSprites()
        {
            DestroyRuntimeObject(surfaceSprite);
            DestroyRuntimeObject(secondaryButtonSprite);
            DestroyRuntimeObject(primaryButtonSprite);
            DestroyRuntimeObject(completionCardSprite);
            DestroyRuntimeObject(surfaceTexture);
            DestroyRuntimeObject(secondaryButtonTexture);
            DestroyRuntimeObject(primaryButtonTexture);
            DestroyRuntimeObject(completionCardTexture);
            surfaceSprite = secondaryButtonSprite = primaryButtonSprite = completionCardSprite = null;
            surfaceTexture = secondaryButtonTexture = primaryButtonTexture = completionCardTexture = null;
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        private void OnDestroy()
        {
            DisposeThemeSprites();
        }
    }
}
