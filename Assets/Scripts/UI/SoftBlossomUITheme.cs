using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class SoftBlossomUITheme : MonoBehaviour
    {
        private const string BackgroundResource =
            "UI/soft-blossom-background";

        private static readonly Color32 Charcoal =
            new(54, 50, 61, 255);

        private static readonly Color32 Rose =
            new(235, 94, 145, 255);

        private static readonly Color32 DeepRose =
            new(201, 67, 116, 255);

        private static readonly Color32 Blush =
            new(255, 218, 230, 255);

        private static readonly Color32 Ivory =
            new(255, 249, 246, 255);

        private static readonly Color32 BorderRose =
            new(234, 151, 177, 255);

        private Sprite cardSprite;
        private Sprite ivoryButtonSprite;
        private Sprite pinkButtonSprite;
        private Texture2D cardTexture;
        private Texture2D ivoryButtonTexture;
        private Texture2D pinkButtonTexture;
        private Sprite backgroundSprite;
        private GameObject backgroundObject;
        private SpriteRenderer backgroundRenderer;
        private Camera gameplayCamera;
        private GameObject levelSelectPanel;
        private GameObject boardRoot;
        private Renderer[] boardRenderers;
        private bool[] boardRendererStates;
        private bool lastMenuVisibility;
        private bool hasMenuVisibility;
        private bool initialized;

        private void Awake()
        {
            BuildRuntimeSprites();
            EnsureWorldBackground();
            ApplyTheme();
            boardRoot = GameObject.Find("Board");
            initialized = true;
        }

        private IEnumerator Start()
        {
            // GameUI creates completion controls during OnEnable.
            yield return null;
            ApplyTheme();
            SyncBoardVisibility(true);
        }

        private void OnEnable()
        {
            if (initialized)
            {
                ApplyTheme();
            }
        }

        private void LateUpdate()
        {
            FitBackgroundToCamera();
            SyncBoardVisibility(false);
        }

        private void BuildRuntimeSprites()
        {
            cardSprite = CreateRoundedSprite(
                "Soft Blossom Card",
                Ivory,
                BorderRose,
                22,
                out cardTexture
            );

            ivoryButtonSprite = CreateRoundedSprite(
                "Soft Blossom Ivory Button",
                new Color32(255, 242, 244, 255),
                BorderRose,
                22,
                out ivoryButtonTexture
            );

            pinkButtonSprite = CreateRoundedSprite(
                "Soft Blossom Pink Button",
                new Color32(250, 133, 174, 255),
                DeepRose,
                22,
                out pinkButtonTexture
            );
        }

        private void EnsureWorldBackground()
        {
            gameplayCamera = Camera.main;

            if (gameplayCamera == null)
            {
                return;
            }

            Texture2D texture =
                Resources.Load<Texture2D>(BackgroundResource);

            if (texture == null)
            {
                Debug.LogWarning(
                    $"SoftBlossomUITheme could not load {BackgroundResource}.",
                    this
                );
                return;
            }

            backgroundSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect
            );
            backgroundSprite.name = "Soft Blossom Background";

            backgroundObject = new GameObject(
                "SoftBlossomBackground"
            );
            backgroundObject.transform.SetParent(
                gameplayCamera.transform,
                false
            );
            backgroundObject.transform.localPosition =
                new Vector3(0f, 0f, 20f);

            backgroundRenderer =
                backgroundObject.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = backgroundSprite;
            backgroundRenderer.sortingOrder = -1000;

            gameplayCamera.backgroundColor =
                new Color32(246, 216, 221, 255);

            FitBackgroundToCamera();
        }

        private void FitBackgroundToCamera()
        {
            if (gameplayCamera == null ||
                backgroundRenderer == null ||
                backgroundSprite == null ||
                !gameplayCamera.orthographic)
            {
                return;
            }

            float worldHeight = gameplayCamera.orthographicSize * 2f;
            float worldWidth = worldHeight * gameplayCamera.aspect;
            Vector2 spriteSize = backgroundSprite.bounds.size;

            float scale = Mathf.Max(
                worldWidth / spriteSize.x,
                worldHeight / spriteSize.y
            );

            backgroundObject.transform.localScale =
                new Vector3(scale, scale, 1f);
        }

        private void ApplyTheme()
        {
            Transform levelSelect = FindDeepChild(
                transform,
                "LevelSelectPanel"
            );
            Transform gameplayHud = FindDeepChild(
                transform,
                "GameplayHUD"
            );

            if (levelSelect != null)
            {
                StyleLevelSelect(levelSelect);
            }

            if (gameplayHud != null)
            {
                StyleGameplayHud(gameplayHud);
            }
        }

        private void StyleLevelSelect(Transform panel)
        {
            Image panelImage = panel.GetComponent<Image>();

            if (panelImage != null)
            {
                panelImage.color = new Color(1f, 1f, 1f, 0f);
            }

            RectTransform card = EnsurePanel(
                panel,
                "BlossomLevelCard",
                new Vector2(740f, 620f),
                new Vector2(0f, 20f),
                cardSprite,
                new Color32(255, 250, 248, 244)
            );
            card.SetSiblingIndex(0);
            EnsureBlossom(card, "TopBlossom", new Vector2(-310f, 258f));
            EnsureBlossom(card, "BottomBlossom", new Vector2(310f, -258f));

            TMP_Text title = FindText(panel, "TitleText");
            StyleText(title, 64f, Charcoal);

            if (title != null)
            {
                ConfigureRect(
                    title.rectTransform,
                    new Vector2(0f, 250f),
                    new Vector2(680f, 120f)
                );
            }

            for (int i = 1; i <= 12; i++)
            {
                Transform button = FindDeepChild(
                    panel,
                    $"LevelButton{i}"
                );

                StyleButton(button, i == 1);

                if (button != null)
                {
                    RectTransform rect =
                        button.GetComponent<RectTransform>();
                    int column = (i - 1) % 3;
                    int row = (i - 1) / 3;
                    rect.anchoredPosition = new Vector2(
                        (column - 1) * 190f,
                        112f - row * 105f
                    );
                    rect.sizeDelta = new Vector2(156f, 94f);
                }
            }

            levelSelectPanel = panel.gameObject;
        }

        private void SyncBoardVisibility(bool force)
        {
            if (levelSelectPanel == null)
            {
                return;
            }

            bool menuVisible = levelSelectPanel.activeInHierarchy;

            if (!force &&
                hasMenuVisibility &&
                menuVisible == lastMenuVisibility)
            {
                return;
            }

            if (menuVisible)
            {
                CacheBoardRenderers();

                for (int i = 0; i < boardRenderers.Length; i++)
                {
                    Renderer renderer = boardRenderers[i];

                    if (renderer == null)
                    {
                        continue;
                    }

                    boardRendererStates[i] = renderer.enabled;
                    renderer.enabled = false;
                }
            }
            else if (boardRenderers != null)
            {
                for (int i = 0; i < boardRenderers.Length; i++)
                {
                    Renderer renderer = boardRenderers[i];

                    if (renderer != null)
                    {
                        renderer.enabled = boardRendererStates[i];
                    }
                }
            }

            lastMenuVisibility = menuVisible;
            hasMenuVisibility = true;
        }

        private void CacheBoardRenderers()
        {
            boardRenderers = boardRoot != null
                ? boardRoot.GetComponentsInChildren<Renderer>(true)
                : System.Array.Empty<Renderer>();
            boardRendererStates = new bool[boardRenderers.Length];
        }

        private void StyleGameplayHud(Transform hud)
        {
            TMP_Text levelText = FindText(hud, "LevelText");
            TMP_Text moveText = FindText(hud, "MoveCountText");

            StyleText(levelText, 38f, Charcoal);
            StyleText(moveText, 28f, Charcoal);

            if (levelText != null)
            {
                levelText.rectTransform.anchoredPosition =
                    new Vector2(0f, -58f);
                levelText.rectTransform.sizeDelta =
                    new Vector2(330f, 68f);
            }

            if (moveText != null)
            {
                moveText.rectTransform.anchoredPosition =
                    new Vector2(-116f, -62f);
                moveText.rectTransform.sizeDelta =
                    new Vector2(170f, 92f);
            }

            EnsureTextChip(
                levelText,
                "LevelChip",
                new Vector2(42f, 18f),
                Ivory
            );
            EnsureTextChip(
                moveText,
                "MoveChip",
                new Vector2(28f, 14f),
                Blush
            );

            Transform restartButton = FindDeepChild(
                hud,
                "RestartButton"
            );
            Transform levelsButton = FindDeepChild(
                hud,
                "LevelsButton"
            );

            StyleButton(restartButton, false);
            StyleButton(levelsButton, false);

            if (restartButton != null)
            {
                RectTransform rect =
                    restartButton.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(138f, -58f);
                rect.sizeDelta = new Vector2(224f, 64f);
            }

            if (levelsButton != null)
            {
                RectTransform rect =
                    levelsButton.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(118f, -130f);
                rect.sizeDelta = new Vector2(184f, 54f);
            }

            Transform completion = FindDeepChild(
                hud,
                "CompletionPanel"
            );

            if (completion != null)
            {
                StyleCompletion(completion);
            }
        }

        private void StyleCompletion(Transform panel)
        {
            Image overlay = panel.GetComponent<Image>();

            if (overlay != null)
            {
                overlay.color = new Color32(69, 42, 61, 112);
                overlay.sprite = null;
                overlay.type = Image.Type.Simple;
            }

            RectTransform card = EnsurePanel(
                panel,
                "BlossomCompletionCard",
                new Vector2(720f, 450f),
                Vector2.zero,
                cardSprite,
                new Color32(255, 250, 248, 255)
            );
            card.SetSiblingIndex(0);
            EnsureBlossom(card, "TopBlossom", new Vector2(-304f, 172f));
            EnsureBlossom(card, "BottomBlossom", new Vector2(302f, -172f));

            TMP_Text completionText = FindText(
                panel,
                "CompletionText"
            );
            TMP_Text movesText = FindText(
                panel,
                "CompletionMoveCountText"
            );

            StyleText(completionText, 46f, Charcoal);
            StyleText(movesText, 28f, DeepRose);

            StyleButton(
                FindDeepChild(panel, "CompletionRestartButton"),
                false
            );
            StyleButton(
                FindDeepChild(panel, "NextButton"),
                true
            );
        }

        private void StyleButton(Transform buttonTransform, bool primary)
        {
            if (buttonTransform == null)
            {
                return;
            }

            Button button = buttonTransform.GetComponent<Button>();
            Image image = buttonTransform.GetComponent<Image>();

            if (button == null || image == null)
            {
                return;
            }

            image.sprite = primary
                ? pinkButtonSprite
                : ivoryButtonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(255, 240, 246, 255);
            colors.pressedColor = new Color32(231, 194, 207, 255);
            colors.selectedColor = new Color32(255, 230, 239, 255);
            colors.disabledColor = new Color32(190, 184, 188, 155);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = image;

            Shadow shadow = buttonTransform.GetComponent<Shadow>();

            if (shadow == null)
            {
                shadow = buttonTransform.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color32(137, 75, 101, 92);
            shadow.effectDistance = new Vector2(0f, -5f);
            shadow.useGraphicAlpha = true;

            SoftButtonFeedback feedback =
                buttonTransform.GetComponent<SoftButtonFeedback>();

            if (feedback == null)
            {
                buttonTransform.gameObject.AddComponent<SoftButtonFeedback>();
            }

            TMP_Text label =
                buttonTransform.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                StyleText(
                    label,
                    primary ? 30f : 27f,
                    primary ? Color.white : Charcoal
                );
            }
        }

        private void EnsureTextChip(
            TMP_Text target,
            string name,
            Vector2 padding,
            Color color)
        {
            if (target == null)
            {
                return;
            }

            RectTransform targetRect = target.rectTransform;
            Transform existing = targetRect.parent.Find(name);
            RectTransform chip;

            if (existing == null)
            {
                GameObject chipObject = new(name, typeof(RectTransform), typeof(Image));
                chipObject.transform.SetParent(targetRect.parent, false);
                chip = chipObject.GetComponent<RectTransform>();
            }
            else
            {
                chip = existing.GetComponent<RectTransform>();
            }

            chip.anchorMin = targetRect.anchorMin;
            chip.anchorMax = targetRect.anchorMax;
            chip.pivot = targetRect.pivot;
            chip.anchoredPosition = targetRect.anchoredPosition;
            chip.sizeDelta = targetRect.sizeDelta + padding;
            chip.localScale = Vector3.one;

            Image image = chip.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;

            int targetIndex = targetRect.GetSiblingIndex();
            chip.SetSiblingIndex(targetIndex);
            targetRect.SetSiblingIndex(targetIndex + 1);
        }

        private RectTransform EnsurePanel(
            Transform parent,
            string name,
            Vector2 size,
            Vector2 position,
            Sprite sprite,
            Color color)
        {
            Transform existing = parent.Find(name);
            RectTransform rect;

            if (existing == null)
            {
                GameObject panel = new(
                    name,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Shadow)
                );
                panel.transform.SetParent(parent, false);
                rect = panel.GetComponent<RectTransform>();
            }
            else
            {
                rect = existing.GetComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            Image image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;

            Shadow shadow = rect.GetComponent<Shadow>();
            shadow.effectColor = new Color32(116, 67, 88, 94);
            shadow.effectDistance = new Vector2(0f, -10f);
            shadow.useGraphicAlpha = true;

            return rect;
        }

        private void EnsureBlossom(
            RectTransform parent,
            string name,
            Vector2 position)
        {
            if (parent.Find(name) != null)
            {
                return;
            }

            GameObject blossom = new(name, typeof(RectTransform));
            blossom.transform.SetParent(parent, false);
            RectTransform root = blossom.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = position;
            root.sizeDelta = new Vector2(72f, 72f);

            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                GameObject petal = new(
                    $"Petal{i + 1}",
                    typeof(RectTransform),
                    typeof(Image)
                );
                petal.transform.SetParent(root, false);
                RectTransform petalRect = petal.GetComponent<RectTransform>();
                petalRect.anchorMin = new Vector2(0.5f, 0.5f);
                petalRect.anchorMax = new Vector2(0.5f, 0.5f);
                petalRect.anchoredPosition = new Vector2(
                    Mathf.Cos(angle) * 18f,
                    Mathf.Sin(angle) * 18f
                );
                petalRect.sizeDelta = new Vector2(30f, 20f);
                petalRect.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    angle * Mathf.Rad2Deg
                );

                Image petalImage = petal.GetComponent<Image>();
                petalImage.sprite = pinkButtonSprite;
                petalImage.type = Image.Type.Sliced;
                petalImage.color = new Color32(255, 151, 186, 210);
                petalImage.raycastTarget = false;
            }

            GameObject center = new(
                "Center",
                typeof(RectTransform),
                typeof(Image)
            );
            center.transform.SetParent(root, false);
            RectTransform centerRect = center.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.anchoredPosition = Vector2.zero;
            centerRect.sizeDelta = new Vector2(18f, 18f);

            Image centerImage = center.GetComponent<Image>();
            centerImage.sprite = pinkButtonSprite;
            centerImage.type = Image.Type.Sliced;
            centerImage.color = new Color32(255, 212, 100, 255);
            centerImage.raycastTarget = false;
        }

        private static TMP_Text FindText(Transform root, string name)
        {
            Transform target = FindDeepChild(root, name);
            return target != null ? target.GetComponent<TMP_Text>() : null;
        }

        private static void StyleText(
            TMP_Text text,
            float maxSize,
            Color color)
        {
            if (text == null)
            {
                return;
            }

            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = maxSize;
            text.characterSpacing = 1.5f;
        }

        private static void ConfigureRect(
            RectTransform rect,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
        }

        private static Transform FindDeepChild(
            Transform parent,
            string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindDeepChild(
                    parent.GetChild(i),
                    name
                );

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Sprite CreateRoundedSprite(
            string name,
            Color32 fill,
            Color32 border,
            int radius,
            out Texture2D texture)
        {
            const int size = 64;
            const float borderWidth = 3.5f;

            texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );
            texture.name = $"{name} Texture";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels = new Color32[size * size];
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            Vector2 half = new(center.x, center.y);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 offset = new(
                        Mathf.Abs(x - center.x),
                        Mathf.Abs(y - center.y)
                    );
                    Vector2 corner = new(
                        Mathf.Max(offset.x - (half.x - radius), 0f),
                        Mathf.Max(offset.y - (half.y - radius), 0f)
                    );
                    float signedDistance = corner.magnitude - radius;
                    float outerAlpha = Mathf.Clamp01(0.75f - signedDistance);
                    float innerDistance = signedDistance + borderWidth;
                    float innerAlpha = Mathf.Clamp01(0.75f - innerDistance);
                    Color pixel = Color.Lerp(border, fill, innerAlpha);
                    pixel.a *= outerAlpha;
                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius)
            );
            sprite.name = name;
            return sprite;
        }

        private void OnDestroy()
        {
            Destroy(backgroundObject);
            Destroy(backgroundSprite);
            Destroy(cardSprite);
            Destroy(ivoryButtonSprite);
            Destroy(pinkButtonSprite);
            Destroy(cardTexture);
            Destroy(ivoryButtonTexture);
            Destroy(pinkButtonTexture);
        }
    }

    internal sealed class SoftButtonFeedback :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        private Vector3 restingScale = Vector3.one;

        private void Awake()
        {
            restingScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = restingScale * 0.96f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = restingScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = restingScale;
        }

        private void OnDisable()
        {
            transform.localScale = restingScale;
        }
    }
}
