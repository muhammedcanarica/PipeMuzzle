using System.Collections.Generic;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class GameplayPauseUI : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(83, 65, 76, 255);
        private GameController controller;
        private ScreenManager screens;
        private Button levelsButton, pauseButton, resumeButton;
        private RectTransform overlay, surface;
        private TMP_Text title;
        private Image rule;
        private CanvasGroup overlayInput;
        private bool bound;
        private readonly Dictionary<Button, bool> suspendedButtons = new();

        public void Initialize(GameController source, Button template, Button levels, ScreenManager manager)
        {
            Unbind();
            controller = source;
            levelsButton = levels;
            screens = manager;
            if (overlay == null && template != null) Build(template);
            Bind();
            Refresh();
        }

        public void ApplyTheme(WorldGameplayTheme theme, Sprite paper, Sprite buttonSurface)
        {
            if (surface == null || theme == null) return;
            Image image = surface.GetComponent<Image>();
            image.sprite = paper;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            title.color = rule.color = theme.PrimaryButtonColor;
            foreach (Button button in overlay.GetComponentsInChildren<Button>(true))
            {
                ColorBlock menuColors = button.colors;
                menuColors.highlightedColor = Color.Lerp(Color.white, theme.PrimaryButtonColor, .24f);
                menuColors.pressedColor = Color.Lerp(Color.white, theme.PrimaryButtonColor, .44f);
                menuColors.selectedColor = menuColors.highlightedColor;
                button.colors = menuColors;
            }
            Image trigger = pauseButton.GetComponent<Image>();
            trigger.sprite = buttonSurface;
            trigger.type = Image.Type.Sliced;
            trigger.color = Color.Lerp(new Color32(255, 250, 244, 255), theme.SecondaryButtonColor, .16f);
            ColorBlock colors = pauseButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, theme.PrimaryButtonColor, .12f);
            colors.pressedColor = Color.Lerp(Color.white, theme.PrimaryButtonColor, .24f);
            colors.selectedColor = colors.highlightedColor;
            pauseButton.colors = colors;
        }

        private void Build(Button template)
        {
            pauseButton = Instantiate(template, template.transform.parent);
            pauseButton.name = "PauseButton";
            pauseButton.onClick = new Button.ButtonClickedEvent();
            pauseButton.onClick.AddListener(TogglePause);
            RectTransform trigger = (RectTransform)pauseButton.transform;
            trigger.anchorMin = trigger.anchorMax = trigger.pivot = new Vector2(0f, 1f);
            trigger.anchoredPosition = new Vector2(32f, -152f);
            trigger.sizeDelta = new Vector2(100f, 42f);
            TMP_Text label = pauseButton.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                label = Text("Label", "PAUSE", pauseButton.transform, Vector2.zero, 19f, TMP_Settings.defaultFontAsset);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            }
            label.text = "PAUSE";
            label.fontSize = label.fontSizeMax = 19f;
            label.fontSizeMin = 16f;
            label.color = Ink;
            label.raycastTarget = false;

            overlay = Rect("PauseOverlay", transform, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlayInput = overlay.gameObject.AddComponent<CanvasGroup>();
            Image veil = overlay.gameObject.AddComponent<Image>();
            veil.color = new Color(.12f, .10f, .15f, .24f);
            veil.raycastTarget = true;
            surface = Rect("Paper", overlay, Vector2.zero, new Vector2(380f, 380f));
            surface.gameObject.AddComponent<Image>().color = new Color32(255, 250, 244, 255);
            title = Text("Title", "PAUSED", surface, new Vector2(0f, 134f), 28f, label.font);
            title.characterSpacing = 3f;
            RectTransform divider = Rect("AccentRule", surface, new Vector2(0f, 96f), new Vector2(170f, 1f));
            rule = divider.gameObject.AddComponent<Image>();
            rule.raycastTarget = false;
            resumeButton = MenuButton("ResumeButton", "RESUME", 58f, label.font, Resume);
            MenuButton("RestartButton", "RESTART", 0f, label.font, Restart);
            MenuButton("LevelsButton", "LEVELS", -58f, label.font, Levels);
            MenuButton("SettingsButton", "SETTINGS", -116f, label.font, Settings);
            overlay.gameObject.SetActive(false);
        }

        private Button MenuButton(string name, string label, float y, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
        {
            TMP_Text text = Text(name, label, surface, new Vector2(0f, y), 22f, font);
            text.raycastTarget = true;
            text.rectTransform.sizeDelta = new Vector2(260f, 56f);
            Button button = text.gameObject.AddComponent<Button>();
            button.targetGraphic = text;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .8f, .85f);
            colors.pressedColor = new Color(.75f, .7f, .75f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = .08f;
            button.colors = colors;
            button.onClick.AddListener(action);
            return button;
        }

        public void HandleEscape() => TogglePause();

        private void TogglePause()
        {
            if (!isActiveAndEnabled || controller == null || screens != null && screens.IsSettingsOpen) return;
            if (controller.IsPaused) Resume();
            else controller.TryPause();
            Refresh();
        }

        private bool CanUseMenu => isActiveAndEnabled && controller != null && controller.IsPaused &&
            (screens == null || !screens.IsSettingsOpen);

        private void Resume() { if (CanUseMenu) controller.Resume(); }

        private void Restart()
        {
            if (!CanUseMenu) return;
            controller.Resume();
            controller.RestartLevel();
        }

        private void Levels()
        {
            if (!CanUseMenu || levelsButton == null) return;
            controller.Resume();
            // This is the existing LevelSelectUI.ShowLevelSelect button binding.
            levelsButton.onClick.Invoke();
        }

        private void Settings()
        {
            if (CanUseMenu) screens?.ShowGameplaySettings();
        }

        private void OnEnable() { Bind(); Refresh(); }
        private void Bind()
        {
            if (bound || controller == null || !isActiveAndEnabled) return;
            controller.PauseChanged += HandlePauseChanged;
            bound = true;
        }
        private void Unbind()
        {
            if (bound && controller != null) controller.PauseChanged -= HandlePauseChanged;
            bound = false;
        }
        private void HandlePauseChanged(bool paused) => Refresh();

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) HandleEscape();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape();
#endif
            Refresh();
        }

        private void Refresh()
        {
            if (overlay == null) return;
            bool paused = controller != null && controller.IsPaused;
            if (overlay.gameObject.activeSelf != paused) SetOverlay(paused);
            pauseButton.interactable = controller != null && controller.CanPause;
            RefreshSettingsInput();
            Layout();
        }

        public void RefreshSettingsInput()
        {
            if (overlayInput == null) return;
            bool allow = screens == null || !screens.IsSettingsOpen;
            bool changed = overlayInput.interactable != allow;
            overlayInput.interactable = overlayInput.blocksRaycasts = allow;
            if (changed && allow && controller != null && controller.IsPaused)
                EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
        }

        private void SetOverlay(bool paused)
        {
            overlay.gameObject.SetActive(paused);
            if (paused)
            {
                overlay.SetAsLastSibling();
                // Block keyboard/controller selection as well as pointer raycasts.
                foreach (Button button in GetComponentsInChildren<Button>(true))
                {
                    if (button.transform.IsChildOf(overlay)) continue;
                    suspendedButtons[button] = button.interactable;
                    button.interactable = false;
                }
                EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
            }
            else
            {
                foreach (var item in suspendedButtons)
                    if (item.Key != null) item.Key.interactable = item.Value;
                suspendedButtons.Clear();
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                    EventSystem.current.currentSelectedGameObject.transform.IsChildOf(overlay))
                    EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void Layout()
        {
            Vector2 size = ((RectTransform)transform).rect.size;
            float scale = Mathf.Max(.1f, Mathf.Min(1f, size.x / 432f, size.y / 432f));
            surface.localScale = Vector3.one * scale;
        }

        private void OnDisable()
        {
            Unbind();
            controller?.Resume();
            if (overlay != null) SetOverlay(false);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject obj = new(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text Text(string name, string value, Transform parent, Vector2 position, float size, TMP_FontAsset font)
        {
            RectTransform rect = Rect(name, parent, position, new Vector2(300f, 48f));
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Ink;
            text.raycastTarget = false;
            return text;
        }
    }
}
