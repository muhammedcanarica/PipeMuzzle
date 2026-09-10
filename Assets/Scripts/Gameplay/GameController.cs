using System;
using System.Collections.Generic;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
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

        [Header("Levels")]
        [SerializeField]
        private List<LevelDefinition> levels = new();

        private BoardState board;
        private ProgressService progressService;

        private int currentLevelIndex;
        private bool isCompleted;

        public event Action<int, int> LevelLoaded;
        public event Action<bool> LevelCompleted;
        public event Action<int> MoveCountChanged;
        public int LevelCount => levels?.Count ?? 0;
        public int CurrentLevelNumber =>
            board == null ? 0 : currentLevelIndex + 1;
        public int CurrentMoveCount => board?.MoveCount ?? 0;
        public bool IsCompleted => isCompleted;
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
            progressService = new ProgressService();
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

            if (LevelCount == 0)
            {
                Debug.LogError(
                    "GameController requires at least one level."
                );

                enabled = false;
                return;
            }

            if (boardCameraFitter.GetComponent<Physics2DRaycaster>() == null)
            {
                boardCameraFitter.gameObject.AddComponent<Physics2DRaycaster>();
            }

            boardView.TileClicked += HandleTileClicked;

            LoadLevel(0);
        }

        private void HandleTileClicked(TileView tileView)
        {
            if (isCompleted ||
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

            tileView.Refresh();

            MoveCountChanged?.Invoke(board.MoveCount);

            bool solved =
                ConnectionChecker.Evaluate(board);

            Debug.Log(
                $"Hamle sayısı: {board.MoveCount}"
            );

            Debug.Log(
                $"Çözüldü mü: {solved}"
            );

            if (solved)
            {
                CompleteLevel();
            }
        }

        private void CompleteLevel()
        {
            if (isCompleted || board == null)
            {
                return;
            }

            isCompleted = true;

            bool hasNextLevel = HasNextLevel;

            if (hasNextLevel)
            {
                progressService.UnlockLevel(
                    currentLevelIndex + 1
                );
            }

            Debug.Log(
                $"Bölüm {currentLevelIndex + 1} tamamlandı!"
            );

            LevelCompleted?.Invoke(hasNextLevel);
        }

        public void RestartLevel()
        {
            LoadLevel(currentLevelIndex);
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

            LevelDefinition level =
                levels[levelIndex];

            if (level == null)
            {
                Debug.LogError(
                    $"Level {levelIndex} is null."
                );

                return;
            }

            currentLevelIndex = levelIndex;
            isCompleted = false;

            board =
                BoardBuilder.Build(level);

            boardView.Build(board);

            boardCameraFitter.Fit();

            MoveCountChanged?.Invoke(board.MoveCount);

            bool solved =
                ConnectionChecker.Evaluate(board);

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
                CompleteLevel();
            }
        }

        private void OnDestroy()
        {
            if (boardView != null)
            {
                boardView.TileClicked -=
                    HandleTileClicked;
            }
        }
    }
}
