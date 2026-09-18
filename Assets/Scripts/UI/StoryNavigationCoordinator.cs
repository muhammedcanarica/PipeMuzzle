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
        private LevelSelectUI levelSelectUi;
        private WorldDefinition selectedWorld;

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

            if (world == null)
            {
                Debug.LogError("StoryNavigationCoordinator requires a selected WorldDefinition.");
                return;
            }

            selectedWorld = world;
            screenManager.ShowComic();
            comicViewerUi.Play(selectedWorld.Story);
        }

        private void Awake()
        {
            levelSelectUi = GetComponent<LevelSelectUI>();
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
            if (screenManager == null || levelSelectUi == null || selectedWorld == null)
            {
                Debug.LogError("StoryNavigationCoordinator cannot open level select without its selected world.");
                return;
            }

            levelSelectUi.ConfigureForWorld(selectedWorld);
            screenManager.ShowLevelSelect();
        }

        private void ShowWorldMap()
        {
            if (screenManager != null) screenManager.ShowWorldMap();
        }
    }
}
