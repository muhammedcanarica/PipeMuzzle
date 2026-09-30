using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    /// <summary>Feathers only the outer paper margin; the source sprite and UVs stay intact.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class WorldMapArtworkEdge : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || mesh.currentVertCount != 4) return;
            UIVertex bottomLeft = default, topLeft = default, topRight = default, bottomRight = default;
            mesh.PopulateUIVertex(ref bottomLeft, 0);
            mesh.PopulateUIVertex(ref topLeft, 1);
            mesh.PopulateUIVertex(ref topRight, 2);
            mesh.PopulateUIVertex(ref bottomRight, 3);
            mesh.Clear();
            const int columns = 48, rows = 36;
            for (int y = 0; y <= rows; y++)
            {
                float v = y / (float)rows;
                for (int x = 0; x <= columns; x++)
                {
                    float u = x / (float)columns;
                    UIVertex vertex = bottomLeft;
                    vertex.position = Vector3.Lerp(Vector3.Lerp(bottomLeft.position, bottomRight.position, u),
                        Vector3.Lerp(topLeft.position, topRight.position, u), v);
                    vertex.uv0 = Vector4.Lerp(Vector4.Lerp(bottomLeft.uv0, bottomRight.uv0, u),
                        Vector4.Lerp(topLeft.uv0, topRight.uv0, u), v);
                    float radius = Mathf.Pow(Mathf.Pow(Mathf.Abs(u * 2f - 1f), 6f)
                        + Mathf.Pow(Mathf.Abs(v * 2f - 1f), 6f), 1f / 6f);
                    float opacity = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, .82f, radius));
                    Color32 color = bottomLeft.color;
                    color.a = (byte)Mathf.RoundToInt(color.a * opacity);
                    vertex.color = color;
                    mesh.AddVert(vertex);
                    if (x == columns || y == rows) continue;
                    int index = y * (columns + 1) + x;
                    mesh.AddTriangle(index, index + columns + 1, index + columns + 2);
                    mesh.AddTriangle(index, index + columns + 2, index + 1);
                }
            }
        }
    }
}
