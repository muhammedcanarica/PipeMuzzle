using System;
using System.Collections;
using System.Collections.Generic;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.Feedback;
using PipeMuzzle.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PipeMuzzle.Gameplay
{
    public class GameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private BoardView boardView;

        [SerializeField]
        private BoardCameraFitter boardCameraFitter;

        private BoardState board;
        private ProgressService progressService;
        private WorldProgressService worldProgressService;
        private WorldDefinition currentWorld;
        private LevelDefinition currentLevelDefinition;
        private readonly List<TileState> solvedPath = new();

        private int currentLevelIndex;
        private bool isCompleted;
        private bool isCompleting;
        private Coroutine completionCoroutine;
        public const int HintsPerAttempt = 3;
        private const float HintCooldown = 2f;
        private float hintAvailableAt;
        public int RemainingHints { get; private set; } = HintsPerAttempt;
        public bool UsedHintThisAttempt { get; private set; }

        public event Action<int, int> LevelLoaded;
        public event Action<bool> LevelCompleted;
        public event Action<int> MoveCountChanged;
        public event Action<bool> PauseChanged;
        public WorldDefinition CurrentWorld => currentWorld;
        public LevelDefinition CurrentLevelDefinition => currentLevelDefinition;
        public int LevelCount => currentWorld?.LevelCount ?? 0;
        public int CurrentLevelNumber =>
            board == null ? 0 : currentLevelIndex + 1;
        public int CurrentMoveCount => board?.MoveCount ?? 0;
        public bool IsPaused { get; private set; }
        public bool CanPause => isActiveAndEnabled && board != null && !IsPaused &&
            !isCompleted && !isCompleting;
        public bool IsCompleted => isCompleted;
        public bool IsCompletionPending => isCompleting;
        public bool WasNewBest { get; private set; }
        public bool CanHint => isActiveAndEnabled && boardView != null && board != null &&
            !IsPaused && !isCompleted && !isCompleting && RemainingHints > 0 &&
            Time.unscaledTime >= hintAvailableAt && !boardView.IsHintPlaying && !boardView.HasPendingRotations &&
            HintSelector.Select(board, currentLevelDefinition) != null;

        public bool TryShowHint()
        {
            if (!CanHint) return false;
            LevelSolutionStep step = HintSelector.SelectStep(board, currentLevelDefinition);
            if (step == null) return false;
            TileState tile = board.GetTile(step.Position.x, step.Position.y);
            Color color = currentWorld?.GameplayTheme != null
                ? currentWorld.GameplayTheme.PrimaryButtonColor : new Color(.7f, .4f, .5f);
            if (!boardView.TryShowHint(tile, color)) return false;
            int turns = (step.Rotation - tile.Rotation + 4) % 4;
            if (!tile.TryApplyHint(step.Rotation)) { CancelHint(); return false; }
            RemainingHints--;
            UsedHintThisAttempt = true;
            hintAvailableAt = Time.unscaledTime + HintCooldown;
            boardView.PlayHintRotation(tile, turns);
            GameFeedback.PlayPipeRotate();
            bool solved = ConnectionChecker.Evaluate(board);
            boardView.RefreshPoweredTiles(Application.isPlaying);
            if (solved) BeginCompletion();
            return true;
        }

        private void ResetHintAttempt()
        {
            RemainingHints = HintsPerAttempt;
            UsedHintThisAttempt = false;
            hintAvailableAt = 0f;
        }

        public void CancelHint() => boardView?.StopHint();

        public bool TryPause()
        {
            if (!CanPause) return false;
            IsPaused = true;
            CancelHint();
            Time.timeScale = 0f;
            PauseChanged?.Invoke(true);
            return true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            PauseChanged?.Invoke(false);
        }
        public bool HasNextLevel =>
            board != null && currentLevelIndex < LevelCount - 1;

        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex >= 0 &&
                   levelIndex < LevelCount &&
                   progressService != null &&
                   progressService.IsLevelUnlocked(levelIndex);
        }

        private void Awake()
        {
            worldProgressService = new WorldProgressService();
        }

        public bool ConfigureWorld(WorldDefinition world)
        {
            Resume();
            CancelTransientVisuals();
            if (boardView != null)
                boardView.Clear();

            board = null;
            currentWorld = null;
            currentLevelDefinition = null;
            progressService = null;
            currentLevelIndex = 0;
            isCompleted = false;
            solvedPath.Clear();
            WasNewBest = false;
            ResetHintAttempt();

            worldProgressService ??= new WorldProgressService();
            if (worldProgressService.GetAccessState(world) != WorldAccessState.Playable)
            {
                Debug.LogError("GameController cannot configure an unavailable world.");
                return false;
            }

            currentWorld = world;
            progressService = new ProgressService(world.WorldId, world.LevelCount);
            return true;
        }

        private void Start()
        {
            if (boardView == null)
            {
                Debug.LogError(
                    "GameController requires a BoardView."
                );

                enabled = false;
                return;
            }

            if (boardCameraFitter == null)
            {
                Debug.LogError(
                    "GameController requires a BoardCameraFitter."
                );

                enabled = false;
                return;
            }

            if (boardCameraFitter.GetComponent<Physics2DRaycaster>() == null)
            {
                boardCameraFitter.gameObject.AddComponent<Physics2DRaycaster>();
            }

            boardView.TileClicked += HandleTileClicked;

        }

        private void HandleTileClicked(TileView tileView)
        {
            if (!isActiveAndEnabled || IsPaused || isCompleted || isCompleting ||
                board == null ||
                tileView == null ||
                tileView.State == null)
            {
                return;
            }

            TileState tile = tileView.State;

            bool rotated = board.TryRotateTile(
                tile.X,
                tile.Y
            );

            if (!rotated)
            {
                return;
            }

            tileView.PlayRotationFeedback();
            GameFeedback.PlayPipeRotate();

            MoveCountChanged?.Invoke(board.MoveCount);

            bool solved =
                ConnectionChecker.Evaluate(board);

            boardView.RefreshPoweredTiles(true);

            Debug.Log(
                $"Hamle sayısı: {board.MoveCount}"
            );

            Debug.Log(
                $"Çözüldü mü: {solved}"
            );

            if (solved)
            {
                BeginCompletion();
            }
        }

        private void CompleteLevel()
        {
            if (isCompleted || board == null)
            {
                return;
            }

            isCompleted = true;
            GameFeedback.PlayLevelComplete();
            isCompleting = false;

            bool hasNextLevel = HasNextLevel;
            WasNewBest = !UsedHintThisAttempt && currentWorld != null && BestMovesProgress.TrySetBest(
                currentWorld.WorldId, CurrentLevelNumber, board.MoveCount);

            if (hasNextLevel)
            {
                progressService.UnlockLevel(
                    currentLevelIndex + 1
                );
            }
            else
            {
                worldProgressService.MarkWorldCompleted(currentWorld.WorldId);
            }

            Debug.Log(
                $"Bölüm {currentLevelIndex + 1} tamamlandı!"
            );

            LevelCompleted?.Invoke(hasNextLevel);
        }

        public void RestartLevel()
        {
            if (board != null)
                LoadLevel(currentLevelIndex);
        }

        public void CancelTransientVisuals()
        {
            if (completionCoroutine != null) StopCoroutine(completionCoroutine);
            completionCoroutine = null;
            isCompleting = false;
            if (boardView != null)
            {
                boardView.StopTransientEffects();
            }
        }
        public void LoadLevelByIndex(int levelIndex)
        {
            if (levelIndex < 0 ||
                levelIndex >= LevelCount)
            {
                Debug.LogWarning(
                    $"Geçersiz level index: {levelIndex}"
                );

                return;
            }

            if (!IsLevelUnlocked(levelIndex))
            {
                Debug.LogWarning(
                    $"Level {levelIndex + 1} henüz kilitli."
                );

                return;
            }

            LoadLevel(levelIndex);
        }

        public void LoadNextLevel()
        {
            if (!isCompleted)
            {
                return;
            }

            int nextLevelIndex =
                currentLevelIndex + 1;

            if (nextLevelIndex >= LevelCount)
            {
                Debug.Log(
                    "Tüm bölümler tamamlandı!"
                );

                return;
            }

            LoadLevel(nextLevelIndex);
        }

        private void LoadLevel(int levelIndex)
        {
            if (levelIndex < 0 ||
                levelIndex >= LevelCount)
            {
                return;
            }

            LevelDefinition level = currentWorld.Levels[levelIndex];

            if (level == null)
            {
                Debug.LogError(
                    $"Level {levelIndex} is null."
                );

                return;
            }

            Resume();
            CancelTransientVisuals();
            currentLevelIndex = levelIndex;
            isCompleted = false;
            WasNewBest = false;
            ResetHintAttempt();

            board =
                BoardBuilder.Build(level);
            currentLevelDefinition = level;

            boardView.Build(board, level.SolutionPath);

            boardCameraFitter.Fit();

            MoveCountChanged?.Invoke(board.MoveCount);

            bool solved =
                ConnectionChecker.Evaluate(board);

            boardView.RefreshPoweredTiles(false);

            Debug.Log(
                $"Bölüm {currentLevelIndex + 1} yüklendi."
            );

            Debug.Log(
                $"Başlangıçta çözüldü mü: {solved}"
            );

            LevelLoaded?.Invoke(
                currentLevelIndex + 1,
                LevelCount
            );

            if (solved)
            {
                BeginCompletion();
            }
        }

        private void BeginCompletion()
        {
            if (IsPaused || board == null || isCompleted || isCompleting) return;
            isCompleting = true;
            // Let the just-solved hint's brief ring finish while normal completion begins.
            if (Application.isPlaying && boardView.HasPendingRotations)
                completionCoroutine = StartCoroutine(WaitForRotationAndStartFlow());
            else StartCompletionFlow();
        }

        private IEnumerator WaitForRotationAndStartFlow()
        {
            while (boardView.HasPendingRotations) yield return null;
            completionCoroutine = null;
            StartCompletionFlow();
        }

        private void StartCompletionFlow()
        {
            bool hasSolvedPath = ConnectionChecker.TryGetSolvedPath(
                board,
                solvedPath
            );

            if (!hasSolvedPath || !boardView.PlayCompletionFeedback(solvedPath, HandleFlowCompleted, currentWorld?.WorldId.ToString()))
                HandleFlowCompleted();
        }

        private void HandleFlowCompleted()
        {
            if (isCompleting) CompleteLevel();
        }

        private void OnDisable()
        {
            Resume();
            CancelTransientVisuals();
        }

        private void OnDestroy()
        {
            Resume();
            CancelTransientVisuals();
            if (boardView != null)
            {
                boardView.TileClicked -=
                    HandleTileClicked;
            }
        }
    }
}
