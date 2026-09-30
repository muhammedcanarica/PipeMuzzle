using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using System;
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
        private WorldPresentationController presentationController;
        private WorldDefinition selectedWorld;
        private readonly StoryCheckpointProgress checkpointProgress = new();
        private bool playingLevelCheckpoint;
        private Action finalCompletion;

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

            if (world.Story == null || world.Story.PanelCount == 0 ||
                world.LevelSelectTheme == null ||
                world.LevelPathLayout == null || !world.LevelPathLayout.HasValidNodeCount ||
                world.GameplayTheme == null)
            {
                Debug.LogError("StoryNavigationCoordinator cannot open a world with missing presentation assets.");
                return;
            }

            selectedWorld = world;
            if (presentationController == null)
                presentationController = GetComponent<WorldPresentationController>();
            if (presentationController == null)
                presentationController = gameObject.AddComponent<WorldPresentationController>();
            presentationController.Configure(selectedWorld);
            playingLevelCheckpoint = false;
            finalCompletion = null;
            if (checkpointProgress.TryBegin(world, 0, out StoryCheckpoint intro))
            {
                screenManager.ShowComic();
                comicViewerUi.Play(intro.Story);
            }
            else if (world.GetStoryCheckpoint(0) == null)
            {
                // Preserve legacy/custom world definitions until checkpoint data is assigned.
                screenManager.ShowComic();
                comicViewerUi.Play(world.Story);
            }
            else ShowLevelSelect();
        }

        public bool TryPlayLevelCheckpoint(WorldDefinition world, int completedLevelNumber,
            bool firstCompletion, Action onFinalCompleted)
        {
            if (!firstCompletion || world == null || screenManager == null || comicViewerUi == null ||
                completedLevelNumber <= 0 || playingLevelCheckpoint) return false;
            levelSelectUi ??= GetComponent<LevelSelectUI>();
            if (levelSelectUi == null || !checkpointProgress.TryBegin(world, completedLevelNumber, out StoryCheckpoint checkpoint))
                return false;

            selectedWorld = world;
            playingLevelCheckpoint = true;
            finalCompletion = completedLevelNumber == world.LevelCount ? onFinalCompleted : null;
            screenManager.ShowComic();
            comicViewerUi.Play(checkpoint.Story);
            return true;
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
            comicViewerUi.StoryCompleted -= FinishStory;
            comicViewerUi.BackRequested -= HandleBack;
            comicViewerUi.StoryCompleted += FinishStory;
            comicViewerUi.BackRequested += HandleBack;
        }

        private void UnsubscribeComicEvents()
        {
            if (comicViewerUi == null) return;
            comicViewerUi.StoryCompleted -= FinishStory;
            comicViewerUi.BackRequested -= HandleBack;
        }

        private void FinishStory()
        {
            Action completion = finalCompletion;
            finalCompletion = null;
            playingLevelCheckpoint = false;
            if (completion != null)
            {
                screenManager.ShowGameplay();
                completion();
            }
            else ShowLevelSelect();
        }

        private void HandleBack()
        {
            if (playingLevelCheckpoint) FinishStory();
            else ShowWorldMap();
        }

        private void ShowLevelSelect()
        {
            levelSelectUi ??= GetComponent<LevelSelectUI>();
            if (screenManager == null || levelSelectUi == null || selectedWorld == null)
            {
                Debug.LogError("StoryNavigationCoordinator cannot open level select without its selected world.");
                return;
            }

            levelSelectUi.ConfigureForWorld(selectedWorld);
            if (levelSelectUi.CurrentWorld == selectedWorld)
                screenManager.ShowLevelSelect();
        }

        private void ShowWorldMap()
        {
            if (screenManager != null) screenManager.ShowWorldMap();
        }
    }
}
