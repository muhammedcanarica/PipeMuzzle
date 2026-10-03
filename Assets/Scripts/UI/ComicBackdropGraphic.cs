using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    // A small UI mesh supplies the gradient and vignette without a new artwork/texture asset.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ComicBackdropGraphic : MaskableGraphic
    {
        private Color center = new Color32(57, 34, 48, 255);

        public void SetTheme(Color tint)
        {
            if (center == tint) return;
            center = tint;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            const int columns = 24;
            const int rows = 16;
            for (int y = 0; y <= rows; y++)
            for (int x = 0; x <= columns; x++)
            {
                float u = x / (float)columns;
                float v = y / (float)rows;
                float distance = new Vector2((u - .5f) * 1.45f, (v - .52f) * 1.6f).magnitude;
                float vignette = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance));
                Color tint = Color.Lerp(center, center * .40f, vignette);
                float atmosphere = Mathf.Exp(-((u - .28f) * (u - .28f) * 6f + (v - .65f) * (v - .65f) * 8f)) * .018f;
                tint.r += atmosphere;
                tint.g += atmosphere * .7f;
                tint.b += atmosphere * .8f;
                tint.a = 1f;
                if (QualitySettings.activeColorSpace == ColorSpace.Linear) tint = tint.linear;
                helper.AddVert(new Vector3(Mathf.Lerp(rect.xMin, rect.xMax, u), Mathf.Lerp(rect.yMin, rect.yMax, v), 0), tint, Vector2.zero);
            }
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int a = y * (columns + 1) + x;
                helper.AddTriangle(a, a + columns + 1, a + 1);
                helper.AddTriangle(a + 1, a + columns + 1, a + columns + 2);
            }
        }
    }
}
