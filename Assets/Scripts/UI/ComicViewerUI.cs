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

        public event Action StoryCompleted;
        public event Action BackRequested;

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
            panelCanvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            panelCanvasGroup.alpha = to;
        }

        private void ShowPanel(Sprite panel)
        {
            if (panelImage != null) panelImage.sprite = panel;
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
            if (continueLabel != null) continueLabel.gameObject.SetActive(true);
        }

        private void CompleteStory()
        {
            CancelTransition();
            currentStory = null;
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
            GameObject frame = new("ComicFrame", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            frame.transform.SetParent(transform, false);
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(.5f, .5f);
            frameRect.sizeDelta = new Vector2(1120f, 800f);
            frame.GetComponent<Image>().color = new Color32(255, 248, 246, 255);

            panelImage = CreateImage("PanelImage", frame.transform, Color.white, true);
            panelImage.preserveAspect = true;
            panelCanvasGroup = frame.GetComponent<CanvasGroup>();

            advanceButton = CreateButton("AdvanceButton", transform, Color.clear, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
            continueLabel = CreateText("ContinueLabel", transform, "Tap to continue", new Vector2(0f, -440f), new Vector2(520f, 48f), 24f);
            continueLabel.raycastTarget = false;
            backButton = CreateButton("BackButton", transform, new Color32(238, 126, 164, 255), new Vector2(44f, -42f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(142f, 52f));
            skipButton = CreateButton("SkipButton", transform, new Color32(238, 126, 164, 255), new Vector2(-44f, -42f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(142f, 52f));
            CreateText("Label", backButton.transform, "BACK", Vector2.zero, new Vector2(142f, 52f), 22f).rectTransform.Stretch();
            CreateText("Label", skipButton.transform, "SKIP", Vector2.zero, new Vector2(142f, 52f), 22f).rectTransform.Stretch();
        }

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
            text.fontStyle = FontStyles.Bold;
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
