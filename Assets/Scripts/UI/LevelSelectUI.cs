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
        [SerializeField] private TMP_Text worldTitleText;
        [SerializeField] private Image levelSelectBackground;
        [SerializeField] private RectTransform pathArea;
        [SerializeField] private RectTransform routeLayer;
        [SerializeField] private Image routeSegmentTemplate;
        private WorldDefinition currentWorld;

        public WorldDefinition CurrentWorld => currentWorld;

        [Header("Level Buttons")]
        [SerializeField]
        private List<Button> levelButtons = new();

        private readonly List<UnityAction> buttonActions = new();
        private readonly List<Image> routeSegments = new();

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

                bool levelExists = currentWorld != null &&
                    i < currentWorld.LevelCount &&
                    i < gameController.LevelCount;

                button.interactable =
                    levelExists &&
                    gameController.IsLevelUnlocked(i);

                ApplyNodeTheme(button, button.interactable);
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
            if (currentWorld == null)
            {
                Debug.LogError("LevelSelectUI cannot open without a configured WorldDefinition.");
                return;
            }

            RefreshButtons();
            gameController.CancelTransientVisuals();

            screenManager.ShowLevelSelect();
        }

        public void ConfigureForWorld(WorldDefinition world)
        {
            if (world == null)
            {
                Debug.LogError("LevelSelectUI requires a non-null WorldDefinition.");
                return;
            }

            currentWorld = world;
            WorldLevelSelectTheme theme = currentWorld.LevelSelectTheme;
            if (theme == null)
            {
                Debug.LogError($"World '{currentWorld.DisplayName}' has no level select theme.");
                return;
            }

            TMP_Text title = worldTitleText != null ? worldTitleText : FindWorldTitle();
            if (title != null)
            {
                title.text = currentWorld.DisplayName;
                title.color = theme.TitleColor;
            }

            Image background = levelSelectBackground != null ? levelSelectBackground : levelSelectPanel.GetComponent<Image>();
            if (background != null && theme.BackgroundSprite != null)
            {
                background.sprite = theme.BackgroundSprite;
            }

            RefreshButtons();
            ApplyWorldLayout();
            RebuildRoute();
        }

        private TMP_Text FindWorldTitle()
        {
            foreach (TMP_Text text in levelSelectPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text.Contains("LEVEL")) return text;
            }

            return null;
        }

        private void ApplyNodeTheme(Button button, bool unlocked)
        {
            if (currentWorld?.LevelSelectTheme == null) return;
            Sprite sprite = unlocked
                ? currentWorld.LevelSelectTheme.NormalNodeSprite
                : currentWorld.LevelSelectTheme.LockedNodeSprite;
            Image image = button.GetComponent<Image>();
            if (image != null && sprite != null) image.sprite = sprite;
        }

        private void ApplyWorldLayout()
        {
            if (currentWorld == null || currentWorld.LevelPathLayout == null) return;
            if (!currentWorld.LevelPathLayout.HasValidNodeCount)
            {
                Debug.LogError($"World '{currentWorld.DisplayName}' requires exactly 12 level path nodes.");
                return;
            }

            if (pathArea == null)
            {
                Debug.LogWarning("LevelSelectUI has no Path Area; preserving existing node positions.");
                return;
            }

            if (levelButtons.Count < currentWorld.LevelPathLayout.NodeCount)
            {
                Debug.LogError("LevelSelectUI has fewer reusable nodes than the configured path layout.");
                return;
            }

            for (int index = 0; index < currentWorld.LevelPathLayout.NodeCount; index++)
            {
                Vector2 normalized = currentWorld.LevelPathLayout.GetNodePosition(index);
                if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f)
                {
                    Debug.LogError($"World '{currentWorld.DisplayName}' has an invalid normalized node position at {index}.");
                    return;
                }
            }

            for (int index = 0; index < currentWorld.LevelPathLayout.NodeCount; index++)
            {
                Button button = levelButtons[index];
                if (button == null) continue;

                RectTransform node = button.transform as RectTransform;
                RectTransform parent = node.parent as RectTransform;
                Vector2 areaPosition = ToAnchoredPosition(pathArea, currentWorld.LevelPathLayout.GetNodePosition(index));
                Vector3 worldPosition = pathArea.TransformPoint(areaPosition);
                node.anchoredPosition = parent.InverseTransformPoint(worldPosition);
            }
        }

        private static Vector2 ToAnchoredPosition(RectTransform area, Vector2 normalized)
        {
            Rect rect = area.rect;
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y)
            );
        }

        private void RebuildRoute()
        {
            foreach (Image segment in routeSegments)
            {
                if (segment != null) Destroy(segment.gameObject);
            }
            routeSegments.Clear();

            if (routeLayer == null || routeSegmentTemplate == null || currentWorld?.LevelPathLayout == null ||
                !currentWorld.LevelPathLayout.HasValidNodeCount || pathArea == null) return;

            for (int index = 0; index < currentWorld.LevelPathLayout.NodeCount - 1; index++)
            {
                Vector2 from = ToRoutePosition(currentWorld.LevelPathLayout.GetNodePosition(index));
                Vector2 to = ToRoutePosition(currentWorld.LevelPathLayout.GetNodePosition(index + 1));
                Vector2 delta = to - from;
                Image segment = Instantiate(routeSegmentTemplate, routeLayer);
                segment.gameObject.SetActive(true);
                segment.raycastTarget = false;
                segment.rectTransform.anchoredPosition = (from + to) * .5f;
                segment.rectTransform.sizeDelta = new Vector2(delta.magnitude, routeSegmentTemplate.rectTransform.sizeDelta.y);
                segment.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                if (currentWorld.LevelSelectTheme.RouteSegmentSprite != null)
                    segment.sprite = currentWorld.LevelSelectTheme.RouteSegmentSprite;
                segment.color = currentWorld.LevelSelectTheme.RouteColor;
                routeSegments.Add(segment);
            }
        }

        private Vector2 ToRoutePosition(Vector2 normalized)
        {
            Vector3 worldPosition = pathArea.TransformPoint(ToAnchoredPosition(pathArea, normalized));
            return routeLayer.InverseTransformPoint(worldPosition);
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
