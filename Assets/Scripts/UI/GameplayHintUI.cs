using PipeMuzzle.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public sealed class GameplayHintUI : MonoBehaviour
    {
        private GameController controller;
        private Button button;
        private TMP_Text label;

        public void Configure(GameController source)
        {
            if (button == null) button = GetComponent<Button>();
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            button.onClick.RemoveListener(ShowHint);
            controller = source;
            if (isActiveAndEnabled) button.onClick.AddListener(ShowHint);
            Refresh();
        }

        private void OnEnable() { if (button != null) { button.onClick.AddListener(ShowHint); Refresh(); } }
        private void Update() => Refresh();
        private void Refresh()
        {
            if (button != null) button.interactable = controller != null && controller.CanHint;
            if (label != null)
            {
                string text = $"HINT {controller?.RemainingHints ?? GameController.HintsPerAttempt}/{GameController.HintsPerAttempt}";
                if (label.text != text) label.text = text;
            }
        }
        private void ShowHint() { controller?.TryShowHint(); Refresh(); }
        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(ShowHint);
            controller?.CancelHint();
        }
    }
}
