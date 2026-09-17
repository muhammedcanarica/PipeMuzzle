using System.Collections.Generic;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameController gameController;

        [SerializeField]
        private GameObject levelSelectPanel;

        [SerializeField]
        private GameObject gameplayHUD;

        [SerializeField]
        private Button levelsButton;

        private ScreenManager screenManager;
        private WorldMapUI worldMapUi;
        private ComicViewerUI comicViewerUi;

        [Header("Level Buttons")]
        [SerializeField]
        private List<Button> levelButtons = new();

        private readonly List<UnityAction> buttonActions = new();

        private void Awake()
        {
            if (gameController == null ||
                levelSelectPanel == null ||
                gameplayHUD == null ||
                levelsButton == null)
            {
                Debug.LogError(
                    "LevelSelectUI requires a GameController, both panels, and a Levels button."
                );

                enabled = false;
                return;
            }

            EnsureSafeArea(gameplayHUD);
            EnsureSafeArea(levelSelectPanel);
            screenManager = GetComponent<ScreenManager>();

            if (screenManager == null)
            {
                screenManager = gameObject.AddComponent<ScreenManager>();
            }

            GameObject worldMap = CreateFullScreenPanel("WorldMapPanel");
            worldMapUi = worldMap.AddComponent<WorldMapUI>();
            GameObject comicViewerPanel = CreateComicViewerPanel();
            screenManager.Configure(worldMap, comicViewerPanel, levelSelectPanel, gameplayHUD);
            worldMapUi.WorldSelected += OpenWorld;
            worldMapUi.Initialize();
            CreateWorldMapBackButton();
            screenManager.ShowWorldMap();
        }

        private void Start()
        {
            SetupButtons();

            levelsButton.onClick.AddListener(
                ShowLevelSelect
            );

            screenManager.ShowWorldMap();
        }

        private void SetupButtons()
        {
            buttonActions.Clear();

            for (int i = 0; i < levelButtons.Count; i++)
            {
                Button button = levelButtons[i];

                if (button == null)
                {
                    continue;
                }

                int levelIndex = i;

                UnityAction action =
                    () => SelectLevel(levelIndex);

                buttonActions.Add(action);

                button.onClick.AddListener(action);
            }
        }

        private static void EnsureSafeArea(GameObject panel)
        {
            if (panel != null &&
                panel.GetComponent<SafeAreaPanel>() == null)
            {
                panel.AddComponent<SafeAreaPanel>();
            }
        }

        private GameObject CreateFullScreenPanel(string name)
        {
            GameObject panel = new(name, typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private GameObject CreateComicViewerPanel()
        {
            GameObject panel = CreateFullScreenPanel("ComicViewerPanel");
            CreateImage("Background", panel.transform, new Color32(35, 29, 44, 255), true);

            GameObject frame = new("ComicFrame", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            frame.transform.SetParent(panel.transform, false);
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(.5f, .5f);
            frameRect.sizeDelta = new Vector2(1120f, 800f);
            frame.GetComponent<Image>().color = new Color32(255, 248, 246, 255);

            Image panelImage = CreateImage("PanelImage", frame.transform, Color.white, true);
            panelImage.preserveAspect = true;

            GameObject advanceObject = new("AdvanceButton", typeof(RectTransform), typeof(Image), typeof(Button));
            advanceObject.transform.SetParent(panel.transform, false);
            Stretch(advanceObject.GetComponent<RectTransform>());
            advanceObject.GetComponent<Image>().color = Color.clear;
            Button advanceButton = advanceObject.GetComponent<Button>();

            TextMeshProUGUI continueLabel = CreateText(
                "ContinueLabel",
                panel.transform,
                "Tap to continue",
                new Vector2(0f, -440f),
                new Vector2(520f, 48f),
                24f
            );
            continueLabel.raycastTarget = false;

            Button backButton = CreateTextButton(
                "BackButton",
                panel.transform,
                "BACK",
                new Vector2(44f, -42f),
                new Vector2(142f, 52f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f)
            );
            Button skipButton = CreateTextButton(
                "SkipButton",
                panel.transform,
                "SKIP",
                new Vector2(-44f, -42f),
                new Vector2(142f, 52f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f)
            );

            comicViewerUi = panel.AddComponent<ComicViewerUI>();
            comicViewerUi.Configure(
                panelImage,
                frame.GetComponent<CanvasGroup>(),
                advanceButton,
                skipButton,
                backButton,
                continueLabel
            );
            comicViewerUi.StoryCompleted += ShowLevelSelect;
            comicViewerUi.BackRequested += ShowWorldMap;
            return panel;
        }

        private void OpenWorld(WorldDefinition world)
        {
            screenManager.ShowComic();
            comicViewerUi.Play(world != null ? world.Story : null);
        }

        private void ShowWorldMap()
        {
            screenManager.ShowWorldMap();
        }

        private static Image CreateImage(string name, Transform parent, Color color, bool stretch)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            if (stretch) Stretch(rect);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Button CreateTextButton(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            buttonObject.GetComponent<Image>().color = new Color32(238, 126, 164, 255);

            TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, Vector2.zero, size, 22f);
            Stretch(text.rectTransform);
            return buttonObject.GetComponent<Button>();
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string value,
            Vector2 position,
            Vector2 size,
            float fontSize)
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void CreateWorldMapBackButton()
        {
            if (levelSelectPanel.transform.Find("WorldMapBackButton") != null)
            {
                return;
            }

            GameObject buttonObject = new(
                "WorldMapBackButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );
            buttonObject.transform.SetParent(levelSelectPanel.transform, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(36f, -36f);
            rect.sizeDelta = new Vector2(150f, 54f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color32(250, 133, 174, 255);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(screenManager.ShowWorldMap);

            GameObject labelObject = new(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "MAP";
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 24f;
            label.color = Color.white;
        }

        private void RefreshButtons()
        {
            for (int i = 0; i < levelButtons.Count; i++)
            {
                Button button = levelButtons[i];

                if (button == null)
                {
                    continue;
                }

                bool levelExists =
                    i < gameController.LevelCount;

                button.interactable =
                    levelExists &&
                    gameController.IsLevelUnlocked(i);
            }
        }

        private void SelectLevel(int levelIndex)
        {
            if (levelIndex < 0 ||
                levelIndex >= gameController.LevelCount ||
                !gameController.IsLevelUnlocked(levelIndex))
            {
                return;
            }

            screenManager.ShowGameplay();

            gameController.LoadLevelByIndex(levelIndex);
        }

        public void ShowLevelSelect()
        {
            RefreshButtons();
            gameController.CancelTransientVisuals();

            screenManager.ShowLevelSelect();
        }

        private void OnDestroy()
        {
            if (worldMapUi != null) worldMapUi.WorldSelected -= OpenWorld;
            if (comicViewerUi != null)
            {
                comicViewerUi.StoryCompleted -= ShowLevelSelect;
                comicViewerUi.BackRequested -= ShowWorldMap;
            }

            if (levelsButton != null)
            {
                levelsButton.onClick.RemoveListener(
                    ShowLevelSelect
                );
            }

            int actionIndex = 0;

            for (int i = 0; i < levelButtons.Count; i++)
            {
                Button button = levelButtons[i];

                if (button == null)
                {
                    continue;
                }

                if (actionIndex >= buttonActions.Count)
                {
                    break;
                }

                button.onClick.RemoveListener(
                    buttonActions[actionIndex]
                );

                actionIndex++;
            }
        }
    }
}
