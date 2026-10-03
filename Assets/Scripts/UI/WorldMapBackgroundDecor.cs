using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    // Low contrast paint layers, kept behind petals and all interactive map content.
    [DisallowMultipleComponent]
    public sealed class WorldMapBackgroundDecor : MonoBehaviour
    {
        private readonly Sprite[] sprites = new Sprite[4];
        private readonly Texture2D[] textures = new Texture2D[4];
        private readonly RectTransform[] details = new RectTransform[16];
        private RectTransform map;
        private RectTransform sakura;
        private RectTransform bamboo;
        private RectTransform moon;
        private Vector2 lastSize;
        private Vector2 lastSakura;
        private Vector2 lastBamboo;
        private Vector2 lastMoon;

        public static void Create(RectTransform root)
        {
            GameObject layer = new("WatercolorBackgroundDecor", typeof(RectTransform), typeof(RectMask2D));
            layer.transform.SetParent(root, false);
            RectTransform rect = (RectTransform)layer.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            WorldMapBackgroundDecor decor = layer.AddComponent<WorldMapBackgroundDecor>();
            decor.map = root;
            decor.Build();
        }

        private void Build()
        {
            for (int kind = 0; kind < sprites.Length; kind++)
            {
                const int width = 512;
                const int height = 256;
                Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
                    { name = "Watercolor Background " + kind, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                Color[] pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float v = y / (float)(height - 1);
                    float noise = Mathf.PerlinNoise(u * 13f + kind * 7f, v * 9f);
                    float alpha;
                    if (kind == 0)
                    {
                        float radius = new Vector2((u - .5f) * 2f, (v - .5f) * 2f).magnitude;
                        alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - radius + (noise - .5f) * .16f) * 2f)) * (.45f + noise * .55f);
                    }
                    else if (kind == 1)
                    {
                        // Uneven overlapping peaks, with a softly feathered painted skyline.
                        float peak = Mathf.Max(.80f - Mathf.Abs(u - .38f) * 1.65f, .68f - Mathf.Abs(u - .73f) * 2.2f);
                        float ridge = Mathf.Max(.25f + .12f * Mathf.PerlinNoise(u * 8f, 3f), peak);
                        alpha = Mathf.Clamp01((ridge - v + (noise - .5f) * .022f) * 70f)
                            * Mathf.SmoothStep(0f, .22f, v) * Mathf.Pow(Mathf.Sin(u * Mathf.PI), .5f) * (.7f + noise * .3f);
                    }
                    else if (kind == 2) alpha = BambooInk(u, v) * (.65f + noise * .35f);
                    else alpha = SakuraInk(u, v, noise);
                    // Pigment variation is baked into the artwork, rather than a flat tinted rectangle.
                    float pigment = .90f + noise * .10f;
                    pixels[y * width + x] = new Color(pigment, pigment, pigment, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                textures[kind] = texture;
                // Keep fine trunks and disconnected leaves; automatic tight outlines can omit them.
                sprites[kind] = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * .5f,
                    100f, 0, SpriteMeshType.FullRect);
            }
            Add(0, "SakuraWash", 0, new Color(.88f, .53f, .63f, .24f));
            Add(1, "BambooWash", 0, new Color(.51f, .68f, .48f, .22f));
            Add(2, "MoonWash", 0, new Color(.60f, .62f, .83f, .24f));
            Add(3, "DistantSakuraHills", 1, new Color(.70f, .49f, .57f, .23f));
            Add(4, "DistantMoonMountains", 1, new Color(.48f, .55f, .73f, .25f));
            Add(5, "EdgeBambooSilhouette", 2, new Color(.40f, .57f, .38f, .25f));
            Add(6, "UpperCloudWash", 0, new Color(.73f, .70f, .82f, .18f));
            Add(7, "LowerMountainWash", 1, new Color(.55f, .62f, .76f, .20f));
            Add(8, "ConnectingMist", 0, new Color(.80f, .64f, .60f, .14f));
            Add(9, "LowerMist", 0, new Color(.62f, .67f, .83f, .18f));
            Add(10, "DistantSakuraTrees", 3, new Color(.75f, .42f, .55f, .24f));
            Add(11, "UpperBambooMountains", 1, new Color(.52f, .63f, .53f, .18f));
            Add(12, "PeachCloud", 0, new Color(.87f, .65f, .55f, .20f));
            Add(13, "SageCloud", 0, new Color(.58f, .71f, .57f, .18f));
            Add(14, "MoonlightWash", 0, new Color(.87f, .87f, .98f, .24f));
            Add(15, "UpperSakuraTrees", 3, new Color(.76f, .48f, .57f, .19f));
        }

        private static float BambooInk(float u, float v)
        {
            float ink = 0f;
            for (int stem = 0; stem < 9; stem++)
            {
                float stalk = .07f + stem * .105f + (v - .5f) * (.045f - stem % 4 * .025f);
                ink = Mathf.Max(ink, Mathf.Clamp01(1f - Mathf.Abs(u - stalk) * 220f) * .65f);
                for (int branch = 0; branch < 4; branch++)
                {
                    float y = .18f + branch * .19f + stem % 3 * .04f;
                    float direction = (branch + stem) % 2 == 0 ? 1f : -1f;
                    Vector2 delta = new(u - stalk - direction * .058f, v - y);
                    float along = delta.x * direction * .8f + delta.y * .6f;
                    float across = delta.x * -.6f + delta.y * direction * .8f;
                    float radius = new Vector2(along / .075f, across / .019f).magnitude;
                    ink = Mathf.Max(ink, Mathf.Clamp01((1f - radius) * 5f));
                }
            }
            return ink * Mathf.SmoothStep(0f, .12f, v) * Mathf.SmoothStep(0f, .12f, 1f - v);
        }

        private static float SakuraInk(float u, float v, float noise)
        {
            float ink = 0f;
            for (int tree = 0; tree < 3; tree++)
            {
                float x = .18f + tree * .31f;
                float top = .57f + tree % 2 * .12f;
                float trunk = Mathf.Clamp01(1f - Mathf.Abs(u - x - (v - .2f) * .055f) * 180f)
                    * Mathf.Clamp01((top - v) * 30f) * Mathf.SmoothStep(0f, .14f, v) * .6f;
                ink = Mathf.Max(ink, trunk);
                for (int crown = 0; crown < 5; crown++)
                {
                    Vector2 center = new(x + Mathf.Sin(crown * 2.4f) * .08f, top + Mathf.Cos(crown * 2.4f) * .10f);
                    float distance = new Vector2((u - center.x) / .13f, (v - center.y) / .17f).magnitude;
                    ink = Mathf.Max(ink, Mathf.Clamp01((1f - distance + (noise - .5f) * .20f) * 10f) * (.65f + noise * .35f));
                }
            }
            return ink * Mathf.Sin(u * Mathf.PI);
        }

        private void Add(int index, string name, int kind, Color color)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(transform, false);
            Image image = item.GetComponent<Image>();
            image.sprite = sprites[kind];
            // UI vertex colors need linear values to retain the intended muted paint tones.
            image.color = QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
            image.raycastTarget = false;
            details[index] = image.rectTransform;
            details[index].anchorMin = details[index].anchorMax = Vector2.one * .5f;
        }

        private void LateUpdate()
        {
            if (sakura == null) sakura = map.Find("Sakura Garden") as RectTransform;
            if (bamboo == null) bamboo = map.Find("Bamboo Workshop") as RectTransform;
            if (moon == null) moon = map.Find("Moon Shrine") as RectTransform;
            if (sakura == null || bamboo == null || moon == null) return;
            Vector2 size = map.rect.size;
            if (size == lastSize && sakura.anchoredPosition == lastSakura && bamboo.anchoredPosition == lastBamboo && moon.anchoredPosition == lastMoon) return;
            lastSize = size;
            lastSakura = sakura.anchoredPosition;
            lastBamboo = bamboo.anchoredPosition;
            lastMoon = moon.anchoredPosition;
            float scale = Mathf.Min(1f, size.x / 700f, size.y / 800f);
            Vector2 washSize = new(Mathf.Min(size.x * .65f, 1050f * scale), size.y * .49f);
            Place(0, lastSakura + new Vector2(-70f, 30f) * scale, washSize);
            Place(1, lastBamboo + new Vector2(100f, -20f) * scale, washSize);
            Place(2, lastMoon + new Vector2(-40f, -15f) * scale, washSize);
            Place(3, new Vector2(-size.x * .40f, size.y * .20f), new Vector2(size.x * .52f, size.y * .40f));
            Place(4, new Vector2(-size.x * .39f, -size.y * .27f), new Vector2(size.x * .53f, size.y * .38f));
            Place(5, new Vector2(size.x * .47f, -size.y * .04f), new Vector2(size.x * .25f, size.y * .54f));
            Place(6, new Vector2(size.x * .42f, size.y * .34f), new Vector2(size.x * .52f, size.y * .27f));
            Place(7, new Vector2(size.x * .31f, -size.y * .43f), new Vector2(size.x * .65f, size.y * .31f));
            Place(8, new Vector2(0f, size.y * .06f), new Vector2(size.x * .58f, size.y * .15f));
            Place(9, new Vector2(size.x * .20f, -size.y * .31f), new Vector2(size.x * .65f, size.y * .22f));
            Place(10, new Vector2(-size.x * .47f, size.y * .08f), new Vector2(size.x * .26f, size.y * .28f));
            Place(11, new Vector2(size.x * .39f, size.y * .32f), new Vector2(size.x * .53f, size.y * .36f));
            Place(12, new Vector2(-size.x * .25f, size.y * .42f), new Vector2(size.x * .60f, size.y * .24f));
            Place(13, new Vector2(size.x * .44f, -size.y * .15f), new Vector2(size.x * .42f, size.y * .26f));
            Place(14, lastMoon + new Vector2(0f, 60f) * scale, washSize * .65f);
            Place(15, new Vector2(-size.x * .46f, size.y * .44f), new Vector2(size.x * .33f, size.y * .26f));
        }

        private void Place(int index, Vector2 position, Vector2 size)
        {
            details[index].anchoredPosition = position;
            details[index].sizeDelta = size;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (Application.isPlaying) { Destroy(sprites[i]); Destroy(textures[i]); }
                else { DestroyImmediate(sprites[i]); DestroyImmediate(textures[i]); }
            }
        }
    }
}
