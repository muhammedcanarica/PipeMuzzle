using PipeMuzzle.Feedback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    /// <summary>One onClick listener per real Button; shared persistent audio, no local sources.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UiSfxFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Play);
        }
        private void Play()
        {
            // onClick already handles pointer/submit validity. Navigation may have hidden the button.
            if (button != null) GameFeedback.PlayUiClick();
        }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Play); }
        public void OnPointerEnter(PointerEventData data)
        {
            if (button == null) button = GetComponent<Button>();
            CursorManager.EnterButton(button, data);
        }
        public void OnPointerExit(PointerEventData data) => CursorManager.ExitTarget(button, data);
        public void OnPointerDown(PointerEventData data) => CursorManager.Press(data);
        public void OnPointerUp(PointerEventData data) => CursorManager.ReleasePress(data);
        private void OnDisable() => CursorManager.ReleaseTarget(button);
        public static void BindHierarchy(Transform root)
        {
            foreach (Button item in root.GetComponentsInChildren<Button>(true))
                if (item.GetComponent<UiSfxFeedback>() == null) item.gameObject.AddComponent<UiSfxFeedback>();
        }
    }
}
