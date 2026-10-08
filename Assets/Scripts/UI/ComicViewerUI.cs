using System;
using System.Collections;
using PipeMuzzle.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class ComicViewerUI : MonoBehaviour
    {
        private const float TransitionDuration = .3f;

        [SerializeField] private Image panelImage;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text continueLabel;

        private ComicStoryDefinition currentStory;
        private int currentPanelIndex;
        private bool isTransitioning;
        private Coroutine transitionRoutine;
        private bool buttonsBound;
        private TMP_Text panelCounter;
        private Image continueWash;
        private ComicBackdropGraphic atmosphere;
        private Vector2 lastPresentationSize;

        public event Action StoryCompleted;
        public event Action BackRequested;

        public bool CanPresent(ComicStoryDefinition story)
        {
            if (panelImage == null || story == null || story.PanelCount == 0) return false;
            foreach (Sprite panel in story.Panels)
                if (panel == null) return false;
            return true;
        }

        public void Configure(
            Image image,
            CanvasGroup canvasGroup,
            Button advance,
            Button skip,
            Button back,
            TMP_Text continueText)
        {
            UnbindButtons();

            panelImage = image;
            panelCanvasGroup = canvasGroup;
            advanceButton = advance;
            skipButton = skip;
            backButton = back;
            continueLabel = continueText;

            BindButtons();
            EnsurePresentation();
        }

        public void Play(ComicStoryDefinition story)
        {
            CancelTransition();
            currentStory = story;
            currentPanelIndex = 0;

            if (currentStory == null || currentStory.PanelCount == 0)
            {
                CompleteStory();
                return;
            }

            ShowPanel(currentStory.Panels[currentPanelIndex]);
        }

        public void Advance()
        {
            if (isTransitioning || currentStory == null) return;

            if (currentPanelIndex + 1 >= currentStory.PanelCount)
            {
                CompleteStory();
                return;
            }

            currentPanelIndex++;
            transitionRoutine = StartCoroutine(TransitionToPanel(currentStory.Panels[currentPanelIndex]));
        }

        public void Skip()
        {
            if (currentStory == null) return;

            CompleteStory();
        }

        public void Back()
        {
            CancelTransition();
            currentStory = null;
            UpdatePanelLabels();
            BackRequested?.Invoke();
        }

        private IEnumerator TransitionToPanel(Sprite nextPanel)
        {
            isTransitioning = true;
            yield return Fade(1f, 0f, TransitionDuration * .5f);
            ShowPanel(nextPanel);
            yield return Fade(0f, 1f, TransitionDuration * .5f);
            isTransitioning = false;
            transitionRoutine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (panelCanvasGroup == null) yield break;

            float elapsed = 0f;
            double started = Time.realtimeSinceStartupAsDouble;
            panelCanvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed = (float)(Time.realtimeSinceStartupAsDouble - started);
                panelCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            panelCanvasGroup.alpha = to;
        }

        private void ShowPanel(Sprite panel)
        {
            EnsurePresentation();
            if (panelImage != null)
            {
                // Reuse one Image for every step, including during the fade transition.
                panelImage.overrideSprite = null;
                panelImage.sprite = panel;
                panelImage.type = Image.Type.Simple;
                panelImage.preserveAspect = true;
                panelImage.color = Color.white;
                panelImage.enabled = panel != null;
                panelImage.raycastTarget = false;
            }
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
            if (continueLabel != null) continueLabel.gameObject.SetActive(true);
            ApplyPresentationTheme();
            UpdatePanelLabels();
            LayoutPresentation();
        }

        private void CompleteStory()
        {
            CancelTransition();
            currentStory = null;
            UpdatePanelLabels();
            StoryCompleted?.Invoke();
        }

        private void CancelTransition()
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            isTransitioning = false;
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Awake()
        {
            EnsureContent();
            BindButtons();
        }

        private void EnsureContent()
        {
            if (panelImage != null || transform.Find("ComicFrame") != null) return;

            CreateImage("Background", transform, new Color32(35, 29, 44, 255), true);
            GameObject frame = new("ComicFrame", typeof(RectTransform), typeof(CanvasGroup));
            frame.transform.SetParent(transform, false);
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.Stretch();
            // Fit inside the screen with space for navigation and the continue hint.
            // The unused space reveals the backdrop instead of an opaque page frame.
            frameRect.offsetMin = new Vector2(32f, 92f);
            frameRect.offsetMax = new Vector2(-32f, -104f);

            panelImage = CreateImage("PanelImage", frame.transform, Color.white, true);
            panelImage.preserveAspect = true;
            panelCanvasGroup = frame.GetComponent<CanvasGroup>();

            advanceButton = CreateButton("AdvanceButton", transform, Color.clear, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
            continueLabel = CreateText("ContinueLabel", transform, "Tap to continue", new Vector2(0f, 30f), new Vector2(520f, 48f), 24f);
            continueLabel.rectTransform.anchorMin = continueLabel.rectTransform.anchorMax = new Vector2(.5f, 0f);
            continueLabel.rectTransform.pivot = new Vector2(.5f, 0f);
            continueLabel.raycastTarget = false;
            backButton = CreateButton("BackButton", transform, new Color32(238, 126, 164, 255), new Vector2(44f, -42f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(142f, 52f));
            skipButton = CreateButton("SkipButton", transform, new Color32(238, 126, 164, 255), new Vector2(-44f, -42f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(142f, 52f));
            CreateText("Label", backButton.transform, "BACK", Vector2.zero, new Vector2(142f, 52f), 22f).rectTransform.Stretch();
            CreateText("Label", skipButton.transform, "SKIP", Vector2.zero, new Vector2(142f, 52f), 22f).rectTransform.Stretch();
            EnsurePresentation();
        }

        private void EnsurePresentation()
        {
            if (panelCounter != null || transform is not RectTransform) return;
            GameObject backdrop = new("ComicAtmosphere", typeof(RectTransform), typeof(ComicBackdropGraphic));
            backdrop.transform.SetParent(transform, false);
            ((RectTransform)backdrop.transform).Stretch();
            backdrop.transform.SetSiblingIndex(transform.Find("Background") != null ? 1 : 0);
            atmosphere = backdrop.GetComponent<ComicBackdropGraphic>();
            atmosphere.raycastTarget = false;
            panelCounter = CreateText("PanelCounter", transform, "", Vector2.zero, new Vector2(96, 30), 16);
            panelCounter.fontStyle = FontStyles.Normal;
            panelCounter.alignment = TextAlignmentOptions.Left;
            panelCounter.raycastTarget = false;
            continueWash = CreateImage("ContinueWash", transform, new Color(1, 1, 1, .035f), false);
            continueWash.raycastTarget = false;
            if (continueLabel != null)
            {
                continueLabel.fontStyle = FontStyles.Normal;
                continueLabel.characterSpacing = 1.5f;
                continueLabel.raycastTarget = false;
            }
            // The existing full-screen Advance button remains below the independent Back/Skip controls.
            if (continueWash != null) continueWash.transform.SetAsLastSibling();
            if (continueLabel != null) continueLabel.transform.SetAsLastSibling();
            panelCounter.transform.SetAsLastSibling();
            if (backButton != null) backButton.transform.SetAsLastSibling();
            if (skipButton != null) skipButton.transform.SetAsLastSibling();
            ApplyPresentationTheme();
            LayoutPresentation();
        }

        private void UpdatePanelLabels()
        {
            if (panelCounter != null)
                panelCounter.text = currentStory != null ? $"{currentPanelIndex + 1} / {currentStory.PanelCount}" : "";
            if (continueLabel != null)
                continueLabel.text = currentStory != null && currentPanelIndex + 1 < currentStory.PanelCount ? "NEXT  ›" : "CONTINUE  ›";
        }

        private void ApplyPresentationTheme()
        {
            string id = currentStory?.StoryId ?? "";
            bool bamboo = id.StartsWith("bamboo", StringComparison.OrdinalIgnoreCase);
            bool moon = id.StartsWith("moon", StringComparison.OrdinalIgnoreCase);
            Color accent = bamboo ? new Color32(196, 207, 174, 255) : moon ? new Color32(190, 192, 230, 255) : new Color32(231, 185, 204, 255);
            Color backdrop = bamboo ? new Color32(43, 48, 36, 255) : moon ? new Color32(34, 38, 64, 255) : new Color32(57, 34, 48, 255);
            atmosphere?.SetTheme(backdrop);
            if (panelCounter != null) panelCounter.color = UiColor(new Color(accent.r, accent.g, accent.b, .65f));
            if (continueLabel != null) continueLabel.color = UiColor(accent);
            StyleAction(backButton, "‹  BACK", accent, .86f);
            StyleAction(skipButton, "SKIP", accent, .68f);
            if (advanceButton != null && continueLabel != null)
            {
                advanceButton.transition = Selectable.Transition.ColorTint;
                advanceButton.targetGraphic = continueLabel;
                ColorBlock colors = ColorBlock.defaultColorBlock;
                colors.normalColor = new Color(1, 1, 1, .88f);
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(1, 1, 1, .7f);
                advanceButton.colors = colors;
            }
        }

        private static void StyleAction(Button button, string label, Color accent, float alpha)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            if (image != null) image.color = Color.clear;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null) return;
            text.text = label;
            text.color = Color.white;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 1.2f;
            text.raycastTarget = false;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = text;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = new Color(1f, 1f, 1f, alpha);
            colors.highlightedColor = UiColor(accent);
            colors.selectedColor = UiColor(accent);
            colors.pressedColor = UiColor(new Color(accent.r, accent.g, accent.b, .72f));
            colors.fadeDuration = .12f;
            button.colors = colors;
        }

        private void LateUpdate()
        {
            if (transform is RectTransform rect && rect.rect.size != lastPresentationSize) LayoutPresentation();
        }

        private void LayoutPresentation()
        {
            if (transform is not RectTransform root || panelCounter == null) return;
            Vector2 size = root.rect.size;
            if (size.x <= 0f || size.y <= 0f) return;
            lastPresentationSize = size;
            float scale = Mathf.Min(1f, size.x / 850f, size.y / 700f);
            float margin = Mathf.Max(16f, 32f * scale);
            RectTransform frame = panelImage != null && panelImage.transform.parent != transform
                ? panelImage.transform.parent as RectTransform : panelImage?.rectTransform;
            if (frame != null)
            {
                frame.Stretch();
                frame.offsetMin = new Vector2(margin, 84f * scale);
                frame.offsetMax = new Vector2(-margin, -80f * scale);
            }
            if (backButton != null)
                Place((RectTransform)backButton.transform, new Vector2(-size.x * .5f + margin + 50f * scale, size.y * .5f - 35f * scale), new Vector2(100, 36) * scale);
            if (skipButton != null)
                Place((RectTransform)skipButton.transform, new Vector2(size.x * .5f - margin - 40f * scale, size.y * .5f - 35f * scale), new Vector2(80, 36) * scale);
            foreach (Button button in new[] { backButton, skipButton })
            {
                TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                if (text != null) text.fontSize = 17f * scale;
            }
            Vector2 next = new(size.x * .5f - margin - 78f * scale, -size.y * .5f + 34f * scale);
            if (continueLabel != null)
            {
                Place(continueLabel.rectTransform, next, new Vector2(156, 34) * scale);
                continueLabel.fontSize = 17f * scale;
            }
            Place(continueWash.rectTransform, next, new Vector2(176, 38) * scale);
            Place(panelCounter.rectTransform, new Vector2(-size.x * .5f + margin + 48f * scale, -size.y * .5f + 34f * scale), new Vector2(96, 30) * scale);
            panelCounter.fontSize = 15f * scale;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Color UiColor(Color color) => QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;

        private static Image CreateImage(string name, Transform parent, Color color, bool stretch)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            if (stretch) rect.Stretch();
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Button CreateButton(string name, Transform parent, Color color, Vector2 position, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            buttonObject.GetComponent<Image>().color = color;
            return buttonObject.GetComponent<Button>();
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            UiTypography.Apply(text, name == "Label" || name == "ContinueLabel" ? UiFontRole.Emphasis :
                name == "PanelCounter" ? UiFontRole.Label : UiFontRole.Body);
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }

        private void BindButtons()
        {
            if (buttonsBound) return;
            if (advanceButton != null) advanceButton.onClick.RemoveListener(Advance);
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
            if (backButton != null) backButton.onClick.RemoveListener(Back);
            if (advanceButton != null) advanceButton.onClick.AddListener(Advance);
            if (skipButton != null) skipButton.onClick.AddListener(Skip);
            if (backButton != null) backButton.onClick.AddListener(Back);
            buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (!buttonsBound) return;
            if (advanceButton != null) advanceButton.onClick.RemoveListener(Advance);
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
            if (backButton != null) backButton.onClick.RemoveListener(Back);
            buttonsBound = false;
        }
    }

    internal static class RectTransformExtensions
    {
        public static void Stretch(this RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
