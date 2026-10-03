using UnityEngine;

namespace PipeMuzzle.UI
{
    // Small owned brush textures for the Level Select presentation only.
    internal sealed class LevelJourneyPaint
    {
        private readonly Texture2D[] textures = new Texture2D[7];
        public readonly Sprite[] Sprites = new Sprite[7];

        public LevelJourneyPaint()
        {
            const int size = 96;
            for (int kind = 0; kind < Sprites.Length; kind++)
            {
                Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
                    { name = "Level Journey Brush " + kind, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                Color[] pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new((x / (float)(size - 1) - .5f) * 2f, (y / (float)(size - 1) - .5f) * 2f);
                    float r = p.magnitude;
                    float noise = Mathf.PerlinNoise(x * .16f, y * .16f);
                    float alpha = kind switch
                    {
                        0 => Mathf.Clamp01((.88f + noise * .045f - r) * 35f) * (.90f + noise * .1f),
                        1 => Mathf.Clamp01(1f - Mathf.Abs(r - .80f) * 32f) * (.7f + noise * .3f),
                        2 => Mathf.Clamp01(1f - r) * (.55f + noise * .45f),
                        3 => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - r) * 1.5f)) * (.6f + noise * .4f),
                        4 => Blossom(p),
                        5 => BambooLeaf(p),
                        _ => Mathf.Clamp01((.78f - r) * 30f) * Mathf.Clamp01(((p - new Vector2(.30f, .15f)).magnitude - .67f) * 30f)
                    };
                    float grain = kind == 0 ? .95f + noise * .05f : 1f;
                    pixels[y * size + x] = new Color(grain, grain, grain, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                textures[kind] = texture;
                Sprites[kind] = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect);
            }
        }

        private static float Blossom(Vector2 p)
        {
            float ink = 0f;
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                Vector2 center = new(Mathf.Sin(angle) * .42f, Mathf.Cos(angle) * .42f);
                ink = Mathf.Max(ink, Mathf.Clamp01((.36f - (p - center).magnitude) * 30f));
            }
            return ink;
        }

        private static float BambooLeaf(Vector2 p)
        {
            float ink = 0f;
            for (int i = -1; i <= 1; i++)
            {
                float angle = i * .65f;
                float x = p.x * Mathf.Cos(angle) - p.y * Mathf.Sin(angle);
                float y = p.x * Mathf.Sin(angle) + p.y * Mathf.Cos(angle);
                float radius = new Vector2((x - i * .17f) / .20f, (y - .08f) / .75f).magnitude;
                ink = Mathf.Max(ink, Mathf.Clamp01((1f - radius) * 12f));
            }
            return ink;
        }

        public void Release()
        {
            for (int i = 0; i < Sprites.Length; i++)
                if (Application.isPlaying) { Object.Destroy(Sprites[i]); Object.Destroy(textures[i]); }
                else { Object.DestroyImmediate(Sprites[i]); Object.DestroyImmediate(textures[i]); }
        }
    }
}
