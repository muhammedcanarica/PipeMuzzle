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
            if (advanceButton != null) advanceButton.onClick.RemoveListener(Advance);
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
            if (backButton != null) backButton.onClick.RemoveListener(Back);

            panelImage = image;
            panelCanvasGroup = canvasGroup;
            advanceButton = advance;
            skipButton = skip;
            backButton = back;
            continueLabel = continueText;

            if (advanceButton != null) advanceButton.onClick.AddListener(Advance);
            if (skipButton != null) skipButton.onClick.AddListener(Skip);
            if (backButton != null) backButton.onClick.AddListener(Back);
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
            if (advanceButton != null) advanceButton.onClick.RemoveListener(Advance);
            if (skipButton != null) skipButton.onClick.RemoveListener(Skip);
            if (backButton != null) backButton.onClick.RemoveListener(Back);
        }
    }
}
