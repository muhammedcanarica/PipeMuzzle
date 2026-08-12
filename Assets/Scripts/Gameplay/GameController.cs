using System;
using System.Collections.Generic;
using PipeMuzzle.Board;
using PipeMuzzle.Data;
using PipeMuzzle.View;
using UnityEngine;

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

        private int currentLevelIndex;
        private bool isCompleted;

        public event Action<int, int> LevelLoaded;
        public event Action<bool> LevelCompleted;
        public event Action<int> MoveCountChanged;
        public int LevelCount => levels.Count;

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

            if (levels == null || levels.Count == 0)
            {
                Debug.LogError(
                    "GameController requires at least one level."
                );

                enabled = false;
                return;
            }

            boardView.TileClicked += HandleTileClicked;

            LoadLevel(0);
        }

        private void HandleTileClicked(TileView tileView)
        {
            if (isCompleted)
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
            isCompleted = true;

            bool hasNextLevel =
                currentLevelIndex < levels.Count - 1;

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
                levelIndex >= levels.Count)
            {
                Debug.LogWarning(
                    $"Geçersiz level index: {levelIndex}"
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

            if (nextLevelIndex >= levels.Count)
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
                levelIndex >= levels.Count)
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
                levels.Count
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
