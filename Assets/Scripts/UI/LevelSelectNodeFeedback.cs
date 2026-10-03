using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelSelectNodeFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private Button button;
        private Image glow;
        private bool hovered;
        private bool selected;
        private bool pressed;
        private float baseAlpha;

        public void Configure(Button source, Image highlight, float alpha)
        {
            button = source;
            glow = highlight;
            baseAlpha = alpha;
            if (!button.IsInteractable()) hovered = selected = pressed = false;
            if (!Application.isPlaying) Apply(true);
        }

        private void Update() => Apply(false);

        private void Apply(bool instant)
        {
            if (button == null || glow == null) return;
            bool playable = button.IsInteractable();
            float scale = playable ? pressed ? .97f : hovered || selected ? 1.045f : 1f : 1f;
            float alpha = baseAlpha + (playable && (hovered || selected) ? .10f : 0f);
            float blend = instant ? 1f : 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale, blend);
            Color color = glow.color;
            color.a = Mathf.Lerp(color.a, alpha, blend);
            glow.color = color;
        }

        public void OnPointerEnter(PointerEventData eventData) { if (button != null && button.IsInteractable()) hovered = true; }
        public void OnPointerExit(PointerEventData eventData) { hovered = pressed = false; }
        public void OnPointerDown(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left && button != null && button.IsInteractable()) pressed = true; }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; }
        public void OnSelect(BaseEventData eventData) { if (button != null && button.IsInteractable()) selected = true; }
        public void OnDeselect(BaseEventData eventData) { selected = pressed = false; }
        private void OnDisable()
        {
            hovered = selected = pressed = false;
            transform.localScale = Vector3.one;
            if (glow != null) { Color color = glow.color; color.a = baseAlpha; glow.color = color; }
        }
    }
}
