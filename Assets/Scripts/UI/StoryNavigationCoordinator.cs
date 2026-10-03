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
        private WorldDefinition activeCheckpointWorld;
        private int activeCheckpointLevel;
        private Action finalCompletion;

        public void Configure(ScreenManager screens, ComicViewerUI viewer)
        {
            UnsubscribeComicEvents();
            ClearActiveCheckpoint();
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
            ClearActiveCheckpoint();
            StoryCheckpoint intro = world.GetStoryCheckpoint(0);
            ComicStoryDefinition entryStory = intro?.Story != null && intro.Story.PanelCount > 0
                ? intro.Story : world.Story;
            screenManager.ShowComic();
            comicViewerUi.Play(entryStory);
        }

        public bool TryPlayLevelCheckpoint(WorldDefinition world, int completedLevelNumber,
            Action onFinalCompleted)
        {
            // A screen change can abandon a comic without a terminal viewer event.
            if (playingLevelCheckpoint && (comicViewerUi == null || !comicViewerUi.gameObject.activeSelf))
                ClearActiveCheckpoint();
            if (world == null || screenManager == null || comicViewerUi == null ||
                completedLevelNumber <= 0 || playingLevelCheckpoint) return false;
            levelSelectUi ??= GetComponent<LevelSelectUI>();
            if (levelSelectUi == null || !checkpointProgress.TryBegin(world, completedLevelNumber, out StoryCheckpoint checkpoint))
                return false;

            if (!comicViewerUi.CanPresent(checkpoint.Story))
            {
                StoryCheckpointProgress.Log($"Story checkpoint presentation unavailable: {world.WorldId} / Level {completedLevelNumber}");
                return false;
            }

            selectedWorld = world;
            playingLevelCheckpoint = true;
            activeCheckpointWorld = world;
            activeCheckpointLevel = completedLevelNumber;
            finalCompletion = completedLevelNumber == world.LevelCount ? onFinalCompleted : null;
            screenManager.ShowComic();
            comicViewerUi.Play(checkpoint.Story);
            StoryCheckpointProgress.Log($"Story checkpoint triggered: {world.WorldId} / Level {completedLevelNumber} / {checkpoint.Story.StoryId}");
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
            ClearActiveCheckpoint();
        }

        private void OnDisable() => ClearActiveCheckpoint();

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
            if (playingLevelCheckpoint && !comicViewerUi.gameObject.activeSelf)
            {
                ClearActiveCheckpoint();
                return;
            }
            Action completion = finalCompletion;
            if (playingLevelCheckpoint && activeCheckpointWorld != null)
                checkpointProgress.MarkViewed(activeCheckpointWorld.WorldId, activeCheckpointLevel);
            // Persist and clear before ShowGameplay re-enables GameUI and reads completion state.
            ClearActiveCheckpoint();
            if (completion != null)
            {
                screenManager.ShowGameplay();
                completion();
            }
            else ShowLevelSelect();
        }

        private void ClearActiveCheckpoint()
        {
            activeCheckpointWorld = null;
            activeCheckpointLevel = 0;
            finalCompletion = null;
            playingLevelCheckpoint = false;
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
