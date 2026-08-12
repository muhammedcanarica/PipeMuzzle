using System.Collections.Generic;
using PipeMuzzle.Gameplay;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameController gameController;

        [SerializeField]
        private GameObject levelSelectPanel;

        [SerializeField]
        private GameObject gameplayHUD;

        [Header("Level Buttons")]
        [SerializeField]
        private List<Button> levelButtons = new();

        private readonly List<UnityAction> buttonActions = new();

        private void Start()
        {
            if (gameController == null)
            {
                Debug.LogError(
                    "LevelSelectUI requires a GameController."
                );

                enabled = false;
                return;
            }

            SetupButtons();

            ShowLevelSelect();
        }

        private void SetupButtons()
        {
            buttonActions.Clear();

            for (int i = 0; i < levelButtons.Count; i++)
            {
                Button button = levelButtons[i];

                if (button == null)
                {
                    continue;
                }

                int levelIndex = i;

                UnityAction action =
                    () => SelectLevel(levelIndex);

                buttonActions.Add(action);

                button.onClick.AddListener(action);

                button.interactable =
                    levelIndex < gameController.LevelCount;
            }
        }

        private void SelectLevel(int levelIndex)
        {
            if (levelIndex < 0 ||
                levelIndex >= gameController.LevelCount)
            {
                return;
            }

            levelSelectPanel.SetActive(false);
            gameplayHUD.SetActive(true);

            gameController.LoadLevelByIndex(levelIndex);
        }

        public void ShowLevelSelect()
        {
            gameplayHUD.SetActive(false);
            levelSelectPanel.SetActive(true);
        }

        private void OnDestroy()
        {
            int actionIndex = 0;

            for (int i = 0; i < levelButtons.Count; i++)
            {
                Button button = levelButtons[i];

                if (button == null)
                {
                    continue;
                }

                if (actionIndex >= buttonActions.Count)
                {
                    break;
                }

                button.onClick.RemoveListener(
                    buttonActions[actionIndex]
                );

                actionIndex++;
            }
        }
    }
}