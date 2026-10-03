using System;
using PipeMuzzle.Feedback;
using PipeMuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SettingsUI : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(83, 65, 76, 255);
        private static readonly Color Rose = new Color32(154, 78, 104, 255);
        private static readonly Color Muted = new Color32(128, 121, 125, 255);
        private ScreenManager screens;
        private Action prepareStartingWorld;
        private RectTransform surface, confirmation;
        private CanvasGroup fade, surfaceInput;
        private TMP_Text soundText;
        private float fadeElapsed;
        private Vector2 layoutSize;

        public void Initialize(ScreenManager manager, Action onProgressReset)
        {
            if (surface != null) return;
            screens = manager;
            prepareStartingWorld = onProgressReset;
            Image paper = gameObject.AddComponent<Image>();
            paper.color = new Color32(255, 247, 236, 255);
            paper.raycastTarget = true;
            fade = gameObject.AddComponent<CanvasGroup>();
            RectTransform content = Rect("Content", transform, Vector2.zero, Vector2.zero);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<SafeAreaPanel>();

            surface = Rect("Surface", content, Vector2.zero, new Vector2(430f, 410f));
            surfaceInput = surface.gameObject.AddComponent<CanvasGroup>();
            Text("Title", "SETTINGS", surface, new Vector2(0f, 132f), new Vector2(340f, 48f), 30f, Ink).characterSpacing = 4f;
            Rule(surface, 94f);
            TMP_Text soundLabel = Text("SoundLabel", "SOUND", surface, new Vector2(-88f, 35f), new Vector2(190f, 50f), 19f, Ink);
            soundLabel.alignment = TextAlignmentOptions.Left;
            Button sound = TextButton("SoundButton", "ON", surface, new Vector2(125f, 35f), new Vector2(80f, 76f), ToggleSound);
            soundText = sound.GetComponent<TMP_Text>();
            TextButton("ResetProgressButton", "RESET PROGRESS", surface, new Vector2(0f, -62f), new Vector2(280f, 76f), ShowResetConfirmation);
            TextButton("BackButton", "BACK", surface, new Vector2(0f, -150f), new Vector2(140f, 76f), () => screens.ShowWorldMap());

            confirmation = Rect("Confirmation", content, Vector2.zero, new Vector2(460f, 350f));
            Text("Title", "RESET ALL PROGRESS?", confirmation, new Vector2(0f, 112f), new Vector2(440f, 54f), 24f, Ink);
            Rule(confirmation, 70f);
            Text("Description", "Resets unlocked worlds, completed levels,\nstory checkpoints and tutorial progress.", confirmation,
                new Vector2(0f, 5f), new Vector2(430f, 110f), 20f, Muted);
            TextButton("CancelButton", "CANCEL", confirmation, new Vector2(-100f, -112f), new Vector2(150f, 76f), CancelReset);
            TextButton("ConfirmResetButton", "RESET", confirmation, new Vector2(100f, -112f), new Vector2(150f, 76f), ConfirmReset);
            Refresh();
            Layout();
        }

        public void Refresh()
        {
            if (surface == null || confirmation == null) return;
            CancelReset();
            soundText.text = GameFeedback.SoundEnabled ? "ON" : "OFF";
            soundText.color = UiColor(GameFeedback.SoundEnabled ? Rose : Muted);
            Layout();
        }

        private void ToggleSound()
        {
            GameFeedback.SetSoundEnabled(!GameFeedback.SoundEnabled);
            Refresh();
        }

        private void ShowResetConfirmation()
        {
            surface.gameObject.SetActive(false);
            surfaceInput.interactable = false;
            confirmation.gameObject.SetActive(true);
        }

        private void CancelReset()
        {
            confirmation.gameObject.SetActive(false);
            surface.gameObject.SetActive(true);
            surfaceInput.interactable = true;
        }

        public void ConfirmReset()
        {
            if (confirmation == null || !confirmation.gameObject.activeSelf || !gameObject.activeSelf) return;
            CancelReset();
            ProgressResetService.ResetAllProgress();
            prepareStartingWorld?.Invoke();
            screens.ShowWorldMap();
        }

        private void OnEnable()
        {
            Refresh();
            fadeElapsed = 0f;
            if (fade != null) fade.alpha = Application.isPlaying ? 0f : 1f;
            Layout();
        }

        private void OnDisable()
        {
            if (surface != null) CancelReset();
        }

        private void Update()
        {
            if (surface != null && layoutSize != ((RectTransform)surface.parent).rect.size) Layout();
            if (fade == null || fade.alpha >= 1f) return;
            fadeElapsed += Time.unscaledDeltaTime;
            fade.alpha = Mathf.SmoothStep(0f, 1f, fadeElapsed / .2f);
        }

        private void OnRectTransformDimensionsChange() => Layout();
        private void Layout()
        {
            if (surface == null || confirmation == null) return;
            Vector2 size = ((RectTransform)surface.parent).rect.size;
            layoutSize = size;
            float preferred = Mathf.Lerp(1f, 2.4f, Mathf.InverseLerp(1f, 1.65f, size.y / Mathf.Max(1f, size.x)));
            float scale = Mathf.Max(.1f, Mathf.Min(preferred, size.x / 524f, size.y / 474f));
            surface.localScale = confirmation.localScale = Vector3.one * scale;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject item = new(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text Text(string name, string value, Transform parent, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            RectTransform rect = Rect(name, parent, position, size);
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = UiColor(color);
            text.raycastTarget = false;
            return text;
        }

        private static Button TextButton(string name, string label, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            TMP_Text text = Text(name, label, parent, position, size, 20f, Rose);
            text.characterSpacing = 1.4f;
            text.raycastTarget = true;
            Button button = text.gameObject.AddComponent<Button>();
            button.targetGraphic = text;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, .90f, .94f);
            colors.pressedColor = new Color(.83f, .72f, .76f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = .08f;
            button.colors = colors;
            button.onClick.AddListener(action);
            return button;
        }

        private static void Rule(Transform parent, float y)
        {
            Image line = Rect("RoseRule", parent, new Vector2(0f, y), new Vector2(270f, 1f)).gameObject.AddComponent<Image>();
            line.color = new Color(Rose.r, Rose.g, Rose.b, .22f);
            line.raycastTarget = false;
        }

        private static Color UiColor(Color color) => QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
    }
}
