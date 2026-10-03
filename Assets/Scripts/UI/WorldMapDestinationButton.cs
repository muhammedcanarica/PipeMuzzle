using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    /// <summary>Subtle map-only hover and press; unavailable destinations stay still.</summary>
    public sealed class WorldMapDestinationButton : Button
    {
        private Image hoverWash;
        private RectTransform worldNode;
        private float targetScale = 1f;
        private float targetHighlight;

        public void Configure(Image wash, RectTransform node)
        {
            hoverWash = wash;
            worldNode = node;
            RefreshVisualState();
        }

        public void RefreshVisualState() => DoStateTransition(currentSelectionState, true);

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            bool available = IsInteractable();
            targetScale = available ? state switch
            {
                SelectionState.Highlighted => 1.03f,
                SelectionState.Selected => 1.02f,
                SelectionState.Pressed => .98f,
                _ => 1f
            } : 1f;
            targetHighlight = available && (state == SelectionState.Highlighted ||
                state == SelectionState.Selected || state == SelectionState.Pressed) ? .09f : 0f;
            if (instant || !Application.isPlaying)
            {
                if (worldNode != null) worldNode.localScale = Vector3.one * targetScale;
                SetHighlight(targetHighlight);
            }
        }

        private void Update()
        {
            float blend = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            if (worldNode != null) worldNode.localScale = Vector3.Lerp(worldNode.localScale, Vector3.one * targetScale, blend);
            if (hoverWash != null)
                SetHighlight(Mathf.Lerp(hoverWash.color.a, targetHighlight, blend));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            targetScale = 1f;
            targetHighlight = 0f;
            transform.localScale = Vector3.one;
            if (worldNode != null) worldNode.localScale = Vector3.one;
            SetHighlight(0f);
        }

        private void SetHighlight(float alpha)
        {
            if (hoverWash != null) hoverWash.color = new Color(.76f, .65f, .56f, alpha);
        }
    }
}
