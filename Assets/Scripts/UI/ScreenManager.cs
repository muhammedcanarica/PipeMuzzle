using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using UnityEngine;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenManager : MonoBehaviour
    {
        [SerializeField] private GameObject worldMapPanel;
        [SerializeField] private GameObject comicViewerPanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject gameplayHud;
        [SerializeField] private GameObject settingsPanel;
        private JourneyCompleteUI journeyComplete;
        public bool IsJourneyCompleteOpen => journeyComplete != null && journeyComplete.gameObject.activeSelf;
        private bool settingsReturnToGameplay;
        public bool IsSettingsOpen => settingsPanel != null && settingsPanel.activeInHierarchy;

        public void Configure(GameObject worldMap, GameObject comicViewer, GameObject levelSelect, GameObject gameplay)
        {
            worldMapPanel = worldMap;
            comicViewerPanel = comicViewer;
            levelSelectPanel = levelSelect;
            gameplayHud = gameplay;
        }

        public void ConfigureWorldMap(GameObject worldMap)
        {
            worldMapPanel = worldMap;
        }

        public void ConfigureSettings(GameObject settings) => settingsPanel = settings;

        public bool TryShowJourneyComplete(GameController controller)
        {
            if (controller == null || !controller.IsCompleted || controller.CurrentWorld == null ||
                controller.CurrentWorld.WorldId != WorldId.MoonShrine || controller.CurrentLevelNumber != 12 ||
                gameplayHud == null) return false;
            if (journeyComplete == null)
            {
                var panel = new GameObject("JourneyComplete", typeof(RectTransform));
                panel.SetActive(false);
                panel.transform.SetParent(gameplayHud.transform.parent, false);
                journeyComplete = panel.AddComponent<JourneyCompleteUI>();
                journeyComplete.Configure(this);
            }
            SetScreen(false, false, false, false, journey: true);
            journeyComplete.ShowJourney();
            journeyComplete.transform.SetAsLastSibling();
            return true;
        }

        public void ShowSettings()
        {
            if (settingsPanel == null) return;
            settingsReturnToGameplay = false;
            settingsPanel.GetComponent<SettingsUI>()?.Refresh();
            SetScreen(false, false, false, false, true);
        }

        public void ShowGameplaySettings()
        {
            if (settingsPanel == null || gameplayHud == null || !gameplayHud.activeInHierarchy) return;
            settingsReturnToGameplay = true;
            settingsPanel.GetComponent<SettingsUI>()?.Refresh();
            // Keep gameplay suspended behind the full-screen settings paper.
            // Deactivating the HUD would release its pause and resume scaled effects.
            settingsPanel.transform.SetAsLastSibling();
            SetScreen(false, false, false, true, true);
        }

        public void ReturnFromSettings()
        {
            if (settingsReturnToGameplay) ShowGameplay();
            else ShowWorldMap();
        }

        public void ShowWorldMap()
        {
            SetScreen(true, false, false, false);
            if (worldMapPanel != null)
                worldMapPanel.GetComponent<WorldMapUI>()?.Refresh();
        }
        public void ShowComic() => SetScreen(false, true, false, false);
        public void ShowLevelSelect() => SetScreen(false, false, true, false);
        public void ShowGameplay() => SetScreen(false, false, false, true);

        private void SetScreen(bool worldMap, bool comicViewer, bool levelSelect, bool gameplay, bool settings = false, bool journey = false)
        {
            if (!settings) settingsReturnToGameplay = false;
            if (journeyComplete != null) journeyComplete.gameObject.SetActive(journey);
            if (worldMapPanel != null) worldMapPanel.SetActive(worldMap);
            if (comicViewerPanel != null) comicViewerPanel.SetActive(comicViewer);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(levelSelect);
            if (gameplayHud != null) gameplayHud.SetActive(gameplay);
            if (settingsPanel != null) settingsPanel.SetActive(settings);
            if (gameplayHud != null) gameplayHud.GetComponent<GameplayPauseUI>()?.RefreshSettingsInput();
        }
    }
}
