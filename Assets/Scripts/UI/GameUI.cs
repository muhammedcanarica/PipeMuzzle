using PipeMuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameController gameController;

        [SerializeField]
        private TMP_Text levelText;

        [SerializeField]
        private TMP_Text moveCountText;

        [SerializeField]
        private GameObject completionPanel;

        [SerializeField]
        private TMP_Text completionText;

        private TMP_Text completionMoveCountText;

        [SerializeField]
        private Button restartButton;

        [SerializeField]
        private Button nextButton;

        private Button completionRestartButton;

        private bool isBound;

        private void OnEnable()
        {
            EnsureCompletionControls();

            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    "GameUI has missing Inspector references.",
                    this
                );

                enabled = false;
                return;
            }

            Bind();
            RefreshFromCurrentState();
        }

        private void EnsureCompletionControls()
        {
            if (completionPanel == null)
            {
                return;
            }

            if (completionMoveCountText == null &&
                moveCountText != null)
            {
                completionMoveCountText = Instantiate(
                    moveCountText,
                    completionPanel.transform
                );

                completionMoveCountText.name =
                    "CompletionMoveCountText";
                completionMoveCountText.fontSize = 30f;
                completionMoveCountText.alignment =
                    TextAlignmentOptions.Center;

                ConfigureRect(
                    completionMoveCountText.rectTransform,
                    new Vector2(0f, 10f),
                    new Vector2(420f, 50f)
                );
            }

            if (completionRestartButton == null &&
                restartButton != null)
            {
                completionRestartButton = Instantiate(
                    restartButton,
                    completionPanel.transform
                );

                completionRestartButton.name =
                    "CompletionRestartButton";

                ConfigureRect(
                    completionRestartButton.GetComponent<RectTransform>(),
                    new Vector2(-140f, -70f),
                    new Vector2(220f, 64f)
                );
            }

            if (completionText != null)
            {
                ConfigureRect(
                    completionText.rectTransform,
                    new Vector2(0f, 80f),
                    new Vector2(600f, 80f)
                );
            }

            if (nextButton != null)
            {
                ConfigureRect(
                    nextButton.GetComponent<RectTransform>(),
                    new Vector2(140f, -70f),
                    new Vector2(220f, 64f)
                );
            }
        }

        private static void ConfigureRect(
            RectTransform rectTransform,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.localScale = Vector3.one;
        }

        private bool HasRequiredReferences()
        {
            return gameController != null &&
                   levelText != null &&
                   moveCountText != null &&
                   completionPanel != null &&
                   completionText != null &&
                   completionMoveCountText != null &&
                   restartButton != null &&
                   nextButton != null &&
                   completionRestartButton != null;
        }

        private void Bind()
        {
            if (isBound)
            {
                return;
            }

            gameController.LevelLoaded +=
                HandleLevelLoaded;

            gameController.LevelCompleted +=
                HandleLevelCompleted;

            gameController.MoveCountChanged +=
                HandleMoveCountChanged;

            restartButton.onClick.AddListener(
                gameController.RestartLevel
            );

            nextButton.onClick.AddListener(
                gameController.LoadNextLevel
            );

            completionRestartButton.onClick.AddListener(
                gameController.RestartLevel
            );

            isBound = true;
        }

        private void RefreshFromCurrentState()
        {
            int levelNumber = gameController.CurrentLevelNumber;

            if (levelNumber > 0)
            {
                levelText.text = $"LEVEL {levelNumber}";
            }

            HandleMoveCountChanged(
                gameController.CurrentMoveCount
            );

            if (gameController.IsCompleted)
            {
                HandleLevelCompleted(
                    gameController.HasNextLevel
                );
            }
            else
            {
                completionPanel.SetActive(false);
            }
        }

        private void HandleMoveCountChanged(int moveCount)
        {
            moveCountText.text = $"MOVES {moveCount}";
            completionMoveCountText.text =
                $"Moves: {moveCount}";
        }

        private void HandleLevelLoaded(
            int levelNumber,
            int _)
        {
            levelText.text =
                $"LEVEL {levelNumber}";

            completionPanel.SetActive(false);
        }

        private void HandleLevelCompleted(
            bool hasNextLevel)
        {
            completionPanel.SetActive(true);

            if (hasNextLevel)
            {
                completionText.text =
                    "LEVEL COMPLETE!";

                nextButton.gameObject.SetActive(true);
            }
            else
            {
                completionText.text =
                    "ALL LEVELS COMPLETE!";

                nextButton.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (!isBound)
            {
                return;
            }

            gameController.LevelLoaded -=
                HandleLevelLoaded;

            gameController.LevelCompleted -=
                HandleLevelCompleted;

            gameController.MoveCountChanged -=
                HandleMoveCountChanged;

            restartButton.onClick.RemoveListener(
                gameController.RestartLevel
            );

            nextButton.onClick.RemoveListener(
                gameController.LoadNextLevel
            );

            completionRestartButton.onClick.RemoveListener(
                gameController.RestartLevel
            );

            isBound = false;
        }
    }
}
