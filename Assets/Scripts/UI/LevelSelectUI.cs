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
        [SerializeField] private StoryNavigationCoordinator storyNavigation;

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
            screenManager.ConfigureWorldMap(worldMap);
            storyNavigation.BindWorldMap(worldMapUi);
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
            if (storyNavigation != null) storyNavigation.BindWorldMap(null);

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
