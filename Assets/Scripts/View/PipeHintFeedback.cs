using UnityEngine;

namespace PipeMuzzle.View
{
    [DisallowMultipleComponent]
    public sealed class PipeHintFeedback : MonoBehaviour
    {
        private const float Duration = .9f;
        private LineRenderer ring;
        private Material material;
        private TileView tile;
        private float elapsed;
        private float radius;
        private Color tint;
        public bool IsPlaying => tile != null;

        public bool TryShow(TileView target, float cellSize, Color color)
        {
            if (IsPlaying || !isActiveAndEnabled || target == null || cellSize <= 0f) return false;
            if (ring == null)
            {
                var visual = new GameObject("PipeHintRing", typeof(LineRenderer));
                visual.transform.SetParent(transform, false);
                ring = visual.GetComponent<LineRenderer>();
                material = new Material(Shader.Find("Sprites/Default"))
                    { name = "Pipe Hint Ring", hideFlags = HideFlags.HideAndDontSave };
                ring.sharedMaterial = material;
                ring.useWorldSpace = true;
                ring.loop = true;
                ring.positionCount = 64;
                ring.sortingOrder = 100;
                ring.numCornerVertices = 2;
            }
            tile = target;
            elapsed = 0f;
            radius = cellSize * .46f;
            tint = color;
            ring.startWidth = ring.endWidth = cellSize * .024f;
            ring.enabled = true;
            Draw();
            return true;
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float deltaTime)
        {
            if (!IsPlaying) { StopAndClear(); return; }
            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed >= Duration || !tile.gameObject.activeInHierarchy) { StopAndClear(); return; }
            Draw();
        }

        private void Draw()
        {
            // Own only the overlay; rotation, click squash and tutorial keep their transforms.
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / Duration) * Mathf.PI);
            float size = radius * (1f + .06f * pulse);
            Color color = tint;
            color.a = .12f + .46f * pulse;
            ring.startColor = ring.endColor = color;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.positionCount;
                ring.SetPosition(i, tile.transform.position + new Vector3(Mathf.Cos(angle) * size, Mathf.Sin(angle) * size, -.01f));
            }
        }

        public void StopAndClear()
        {
            tile = null;
            elapsed = 0f;
            if (ring != null) ring.enabled = false;
        }

        private void OnDisable() => StopAndClear();
        private void OnDestroy()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
