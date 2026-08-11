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

            restartButton.onClick.AddListener(
                gameController.RestartLevel
            );

            nextButton.onClick.AddListener(
                gameController.LoadNextLevel
            );

            completionPanel.SetActive(false);
        }

        private void HandleLevelLoaded(
            int levelNumber,
            int totalLevels)
        {
            levelText.text =
                $"PIPE MUZZLE  •  BÖLÜM {levelNumber} / {totalLevels}";

            completionPanel.SetActive(false);
        }

        private void HandleLevelCompleted(
            bool hasNextLevel)
        {
            completionPanel.SetActive(true);

            if (hasNextLevel)
            {
                completionText.text =
                    "BÖLÜM TAMAMLANDI!";

                nextButton.gameObject.SetActive(true);
            }
            else
            {
                completionText.text =
                    "TÜM BÖLÜMLER TAMAMLANDI!";

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
