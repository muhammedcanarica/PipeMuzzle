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

        [SerializeField]
        private Button restartButton;

        [SerializeField]
        private Button nextButton;

        private void Start()
        {
            if (gameController == null)
            {
                Debug.LogError(
                    "GameUI requires a GameController."
                );

                enabled = false;
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

            completionPanel.SetActive(false);
        }

        private void HandleMoveCountChanged(int moveCount)
        {
            moveCountText.text = $"HAMLE: {moveCount}";
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

        private void OnDestroy()
        {
            if (gameController != null)
            {
                gameController.LevelLoaded -=
                    HandleLevelLoaded;

                gameController.LevelCompleted -=
                    HandleLevelCompleted;

                gameController.MoveCountChanged -=
                    HandleMoveCountChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(
                    gameController.RestartLevel
                );
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(
                    gameController.LoadNextLevel
                );
            }
        }
    }
}
