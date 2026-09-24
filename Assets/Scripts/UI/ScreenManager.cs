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

        public void ShowWorldMap()
        {
            SetScreen(true, false, false, false);
            if (worldMapPanel != null)
                worldMapPanel.GetComponent<WorldMapUI>()?.Refresh();
        }
        public void ShowComic() => SetScreen(false, true, false, false);
        public void ShowLevelSelect() => SetScreen(false, false, true, false);
        public void ShowGameplay() => SetScreen(false, false, false, true);

        private void SetScreen(bool worldMap, bool comicViewer, bool levelSelect, bool gameplay)
        {
            if (worldMapPanel != null) worldMapPanel.SetActive(worldMap);
            if (comicViewerPanel != null) comicViewerPanel.SetActive(comicViewer);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(levelSelect);
            if (gameplayHud != null) gameplayHud.SetActive(gameplay);
        }
    }
}
