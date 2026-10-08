using System;
using PipeMuzzle.Feedback;
using PipeMuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private TMP_Text volumeText;
        private Slider volumeSlider;
        private Slider musicSlider;
        private TMP_Text musicVolumeText;
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

            surface = Rect("Surface", content, Vector2.zero, new Vector2(430f, 610f));
            surfaceInput = surface.gameObject.AddComponent<CanvasGroup>();
            Text("Title", "SETTINGS", surface, new Vector2(0f, 222f), new Vector2(340f, 48f), 30f, Ink).characterSpacing = 4f;
            Rule(surface, 184f);
            TMP_Text soundLabel = Text("SoundLabel", "SFX SOUND", surface, new Vector2(-88f, 128f), new Vector2(190f, 50f), 19f, Ink);
            soundLabel.alignment = TextAlignmentOptions.Left;
            Button sound = TextButton("SoundButton", "ON", surface, new Vector2(125f, 128f), new Vector2(80f, 76f), ToggleSound);
            soundText = sound.GetComponent<TMP_Text>();
            TMP_Text volumeLabel = Text("VolumeLabel", "SFX VOLUME", surface, new Vector2(-65f, 60f), new Vector2(240f, 40f), 17f, Ink);
            volumeLabel.alignment = TextAlignmentOptions.Left;
            volumeText = Text("VolumeValue", "75%", surface, new Vector2(125f, 60f), new Vector2(80f, 40f), 17f, Muted);
            volumeSlider = CreateVolumeSlider("SfxVolumeSlider", 14f, volumeText, GameFeedback.SetSfxVolume);
            TMP_Text musicLabel = Text("MusicVolumeLabel", "MUSIC VOLUME", surface, new Vector2(-65f, -46f), new Vector2(240f, 40f), 17f, Ink);
            musicLabel.alignment = TextAlignmentOptions.Left;
            musicVolumeText = Text("MusicVolumeValue", "40%", surface, new Vector2(125f, -46f), new Vector2(80f, 40f), 17f, Muted);
            musicSlider = CreateVolumeSlider("MusicVolumeSlider", -92f, musicVolumeText, GameFeedback.SetMusicVolume);
            TextButton("ResetProgressButton", "RESET PROGRESS", surface, new Vector2(0f, -166f), new Vector2(280f, 76f), ShowResetConfirmation);
            TextButton("BackButton", "BACK", surface, new Vector2(0f, -244f), new Vector2(140f, 76f), () => screens.ReturnFromSettings());

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
            musicSlider.SetValueWithoutNotify(GameFeedback.MusicVolume);
            musicVolumeText.text = Mathf.RoundToInt(GameFeedback.MusicVolume * 100f) + "%";
            volumeSlider.SetValueWithoutNotify(GameFeedback.SfxVolume);
            volumeText.text = Mathf.RoundToInt(GameFeedback.SfxVolume * 100f) + "%";
            Layout();
        }

        private void ToggleSound()
        {
            GameFeedback.SetSoundEnabled(!GameFeedback.SoundEnabled);
            Refresh();
        }

        private Slider CreateVolumeSlider(string name, float y, TMP_Text valueText, Action<float> setVolume)
        {
            RectTransform root = Rect(name, surface, new Vector2(0f, y), new Vector2(320f, 44f));
            Image hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            Slider slider = root.gameObject.AddComponent<Slider>();
            RectTransform track = Rect("Track", root, Vector2.zero, new Vector2(300f, 6f));
            Image background = track.gameObject.AddComponent<Image>();
            background.color = new Color32(224, 204, 205, 255);
            RectTransform fillArea = Rect("FillArea", root, Vector2.zero, new Vector2(300f, 6f));
            RectTransform fill = Rect("Fill", fillArea, Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>(); fillImage.color = Rose; fillImage.raycastTarget = false;
            RectTransform handleArea = Rect("HandleArea", root, Vector2.zero, new Vector2(300f, 18f));
            RectTransform handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(18f, 0f));
            Image handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Rose;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.onValueChanged.AddListener(value =>
            {
                setVolume(value);
                valueText.text = Mathf.RoundToInt(value * 100f) + "%";
            });
            return slider;
        }

        private void ShowResetConfirmation()
        {
            surface.gameObject.SetActive(false);
            surfaceInput.interactable = false;
            confirmation.gameObject.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(confirmation.Find("CancelButton").gameObject);
        }

        private void CancelReset()
        {
            confirmation.gameObject.SetActive(false);
            surface.gameObject.SetActive(true);
            surfaceInput.interactable = true;
            if (gameObject.activeInHierarchy && soundText != null)
                EventSystem.current?.SetSelectedGameObject(soundText.gameObject);
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
            float scale = Mathf.Max(.1f, Mathf.Min(preferred, size.x / 524f, size.y / 674f));
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
            UiTypography.Apply(text, name == "Title" && value == "SETTINGS" ? UiFontRole.Heading :
                name == "Title" ? UiFontRole.Emphasis : name == "Description" ? UiFontRole.Body : UiFontRole.Label);
            text.alignment = TextAlignmentOptions.Center;
            text.color = UiColor(color);
            text.raycastTarget = false;
            return text;
        }

        private static Button TextButton(string name, string label, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            TMP_Text text = Text(name, label, parent, position, size, 20f, Rose);
            UiTypography.Apply(text, UiFontRole.Emphasis);
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
