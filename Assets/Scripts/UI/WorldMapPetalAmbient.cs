using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    // A fixed, decorative UI layer: no particle system, input, or progression state.
    [DisallowMultipleComponent]
    public sealed class WorldMapPetalAmbient : MonoBehaviour
    {
        private const int SakuraPetalCount = 12;
        private const int CenterPetalCount = 4;
        private const int BambooPetalCount = 4;
        private const int PetalCount = 22;
        private readonly Image[] petals = new Image[PetalCount];
        private readonly float[] phases = new float[PetalCount];
        private RectTransform map;
        private Sprite petalSprite;
        private Texture2D petalTexture;

        public static void Create(RectTransform mapRoot)
        {
            GameObject layer = new("AmbientSakuraPetals", typeof(RectTransform), typeof(RectMask2D));
            layer.transform.SetParent(mapRoot, false);
            RectTransform rect = (RectTransform)layer.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            WorldMapPetalAmbient ambient = layer.AddComponent<WorldMapPetalAmbient>();
            ambient.map = mapRoot;
            ambient.CreatePetals();
        }

        private void CreatePetals()
        {
            const int size = 48;
            petalTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = "Ambient Sakura Petal", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = y / (float)(size - 1);
                float u = (x / (float)(size - 1) - .5f) * 2f;
                // Tapered teardrop with a tiny cherry blossom notch at the broad end.
                float width = Mathf.Pow(Mathf.Sin(v * Mathf.PI), .75f) * (.48f + .42f * v);
                float edge = Mathf.Clamp01((width - Mathf.Abs(u)) * 18f);
                float notch = 1f - Mathf.Clamp01((v - .83f - Mathf.Abs(u) * .5f) * 24f);
                Color color = Color.Lerp(new Color(1f, .84f, .88f), new Color(.88f, .54f, .65f), v);
                color.a = edge * notch * Mathf.Clamp01(v * 20f) * Mathf.Clamp01((1f - v) * 20f);
                pixels[y * size + x] = color;
            }
            petalTexture.SetPixels(pixels);
            petalTexture.Apply(false, true);
            petalSprite = Sprite.Create(petalTexture, new Rect(0, 0, size, size), Vector2.one * .5f);
            for (int i = 0; i < petals.Length; i++)
            {
                GameObject leaf = new("Petal", typeof(RectTransform), typeof(Image));
                leaf.transform.SetParent(transform, false);
                Image image = leaf.GetComponent<Image>();
                image.sprite = petalSprite;
                image.raycastTarget = false;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.one * .5f;
                petals[i] = image;
                phases[i] = (i * .173f + .13f) % 1f;
            }
            Animate(0f);
        }

        private void LateUpdate()
        {
            if (Application.isPlaying) Animate(Mathf.Min(Time.unscaledDeltaTime, .05f));
        }

        private void Animate(float delta)
        {
            Vector2 size = map.rect.size;
            if (size.x <= 0f || size.y <= 0f) return;
            float scale = Mathf.Min(1f, size.x / 700f, size.y / 800f);
            Vector2 sakuraCenter = RegionCenter("Sakura Garden", new Vector2(-size.x * .2f, size.y * .2f));
            Vector2 bambooCenter = RegionCenter("Bamboo Workshop", new Vector2(size.x * .2f, 0f));
            Vector2 moonCenter = RegionCenter("Moon Shrine", new Vector2(-size.x * .2f, -size.y * .25f));
            for (int i = 0; i < petals.Length; i++)
            {
                bool nearSakura = i < SakuraPetalCount;
                bool nearCenter = !nearSakura && i < SakuraPetalCount + CenterPetalCount;
                bool nearBamboo = !nearSakura && !nearCenter && i < SakuraPetalCount + CenterPetalCount + BambooPetalCount;
                Vector2 center = nearSakura ? sakuraCenter : nearCenter ? (sakuraCenter + bambooCenter) * .5f : nearBamboo ? bambooCenter : moonCenter;
                float height = size.y * (nearSakura ? .46f : nearBamboo ? .42f : .34f);
                phases[i] = Mathf.Repeat(phases[i] + delta * (7f + i % 6 * .6f) * scale / height, 1f);
                float t = phases[i];
                float lane = ((i * .381966f) % 1f - .5f);
                float drift = Mathf.Sin(t * Mathf.PI * 2f + i * 1.7f) * 18f * scale;
                float x = center.x + lane * Mathf.Min(size.x * .55f, 760f * scale);
                float y = center.y + (.5f - t) * height;
                Image leaf = petals[i];
                leaf.rectTransform.anchoredPosition = new Vector2(x + drift + t * 22f * scale, y);
                leaf.rectTransform.sizeDelta = new Vector2(12f + i % 4 * 2.5f, 18f + i % 4 * 4f) * scale;
                leaf.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 37f + Mathf.Sin(t * Mathf.PI * 2f + i) * 22f);
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f - t) / .12f);
                float alpha = nearSakura ? .34f : nearCenter ? .29f : nearBamboo ? .25f : .20f;
                leaf.color = new Color(1f, 1f, 1f, fade * (alpha + i % 3 * .035f));
            }
        }

        private Vector2 RegionCenter(string name, Vector2 fallback)
        {
            RectTransform region = map.Find(name) as RectTransform;
            RectTransform artwork = region != null ? region.Find("Artwork") as RectTransform : null;
            return artwork != null ? region.anchoredPosition + artwork.anchoredPosition : fallback;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                Destroy(petalSprite);
                Destroy(petalTexture);
            }
            else
            {
                DestroyImmediate(petalSprite);
                DestroyImmediate(petalTexture);
            }
        }
    }
}
