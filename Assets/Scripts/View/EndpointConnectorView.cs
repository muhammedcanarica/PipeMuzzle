using System.Collections.Generic;
using UnityEngine;

namespace PipeMuzzle.View
{
    [DisallowMultipleComponent, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class EndpointConnectorView : MonoBehaviour
    {
        // Aperture reaches inside the authored basin. Outside it, UVs and geometry
        // are identical to the normal pipe, including the half-cell connector seam.
        private const float Aperture = .22f;
        private Mesh mesh;
        private Sprite sprite;
        private MeshRenderer connector;
        private MaterialPropertyBlock properties;
        private Vector2 portDirection;

        public void Configure(SpriteRenderer source, bool visible, Vector2 direction = default)
        {
            if (connector == null) connector = GetComponent<MeshRenderer>();
            connector.enabled = visible && source != null && source.sprite != null;
            if (!connector.enabled) return;
            properties ??= new MaterialPropertyBlock();
            if (sprite != source.sprite || portDirection != direction) BuildMesh(source.sprite, direction);
            connector.sharedMaterial = source.sharedMaterial;
            connector.sortingLayerID = source.sortingLayerID;
            connector.sortingOrder = source.sortingOrder;
            // Reuse the original texture; no readable texture copies or extra pipe assets.
            properties.SetTexture("_MainTex", sprite.texture);
            properties.SetColor("_Color", source.color);
            properties.SetColor("_RendererColor", Color.white);
            connector.SetPropertyBlock(properties);
        }

        private void BuildMesh(Sprite artwork, Vector2 direction)
        {
            DisposeMesh();
            sprite = artwork;
            portDirection = direction;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            Bounds bounds = sprite.bounds;
            Rect texture = sprite.textureRect;
            // Authored levels show only the arm facing the real path neighbor.
            // A board without solution data keeps the original preview behavior.
            if (direction == Vector2.zero)
            {
                Quad(bounds.min.x, bounds.min.y, bounds.max.x, -Aperture);
                Quad(bounds.min.x, Aperture, bounds.max.x, bounds.max.y);
                Quad(bounds.min.x, -Aperture, -Aperture, Aperture);
                Quad(Aperture, -Aperture, bounds.max.x, Aperture);
            }
            else if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                if (direction.x > 0f) Quad(Aperture, bounds.min.y, bounds.max.x, bounds.max.y);
                else Quad(bounds.min.x, bounds.min.y, -Aperture, bounds.max.y);
            }
            else if (direction.y > 0f) Quad(bounds.min.x, Aperture, bounds.max.x, bounds.max.y);
            else Quad(bounds.min.x, bounds.min.y, bounds.max.x, -Aperture);
            mesh = new Mesh { name = "Endpoint Original Pipe Ports", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            var colors = new Color[vertices.Count];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            mesh.colors = colors;
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;

            void Quad(float left, float bottom, float right, float top)
            {
                int first = vertices.Count;
                Point(left, bottom); Point(left, top); Point(right, top); Point(right, bottom);
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }

            void Point(float x, float y)
            {
                vertices.Add(new Vector3(x, y, 0f));
                uv.Add(new Vector2(
                    (texture.x + sprite.pivot.x + x * sprite.pixelsPerUnit) / sprite.texture.width,
                    (texture.y + sprite.pivot.y + y * sprite.pixelsPerUnit) / sprite.texture.height));
            }
        }

        private void DisposeMesh()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
            mesh = null;
        }

        private void OnDestroy() => DisposeMesh();
    }
}
