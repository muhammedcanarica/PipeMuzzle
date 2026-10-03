using PipeMuzzle.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public sealed class GameplayHintUI : MonoBehaviour
    {
        private GameController controller;
        private Button button;

        public void Configure(GameController source)
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.RemoveListener(ShowHint);
            controller = source;
            if (isActiveAndEnabled) button.onClick.AddListener(ShowHint);
            Refresh();
        }

        private void OnEnable() { if (button != null) { button.onClick.AddListener(ShowHint); Refresh(); } }
        private void Update() => Refresh();
        private void Refresh() { if (button != null) button.interactable = controller != null && controller.CanHint; }
        private void ShowHint() { controller?.TryShowHint(); Refresh(); }
        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(ShowHint);
            controller?.CancelHint();
        }
    }
}
