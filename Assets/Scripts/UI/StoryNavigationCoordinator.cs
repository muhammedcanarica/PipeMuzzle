using PipeMuzzle.Data;
using UnityEngine;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class StoryNavigationCoordinator : MonoBehaviour
    {
        [SerializeField] private ScreenManager screenManager;
        [SerializeField] private ComicViewerUI comicViewerUi;

        private WorldMapUI worldMapUi;

        public void Configure(ScreenManager screens, ComicViewerUI viewer)
        {
            UnsubscribeComicEvents();
            screenManager = screens;
            comicViewerUi = viewer;
            SubscribeComicEvents();
        }

        public void BindWorldMap(WorldMapUI worldMap)
        {
            if (worldMapUi != null) worldMapUi.WorldSelected -= OpenWorld;
            worldMapUi = worldMap;
            if (worldMapUi != null) worldMapUi.WorldSelected += OpenWorld;
        }

        public void OpenWorld(WorldDefinition world)
        {
            if (screenManager == null || comicViewerUi == null)
            {
                Debug.LogError("StoryNavigationCoordinator requires ScreenManager and ComicViewerUI references.");
                return;
            }

            screenManager.ShowComic();
            comicViewerUi.Play(world != null ? world.Story : null);
        }

        private void Awake()
        {
            SubscribeComicEvents();
        }

        private void OnDestroy()
        {
            BindWorldMap(null);
            UnsubscribeComicEvents();
        }

        private void SubscribeComicEvents()
        {
            if (comicViewerUi == null) return;
            comicViewerUi.StoryCompleted -= ShowLevelSelect;
            comicViewerUi.BackRequested -= ShowWorldMap;
            comicViewerUi.StoryCompleted += ShowLevelSelect;
            comicViewerUi.BackRequested += ShowWorldMap;
        }

        private void UnsubscribeComicEvents()
        {
            if (comicViewerUi == null) return;
            comicViewerUi.StoryCompleted -= ShowLevelSelect;
            comicViewerUi.BackRequested -= ShowWorldMap;
        }

        private void ShowLevelSelect()
        {
            if (screenManager != null) screenManager.ShowLevelSelect();
        }

        private void ShowWorldMap()
        {
            if (screenManager != null) screenManager.ShowWorldMap();
        }
    }
}
