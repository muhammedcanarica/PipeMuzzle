using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    /// <summary>Final presentation only; progression stays in WorldProgressService.</summary>
    [DisallowMultipleComponent]
    public sealed class JourneyCompleteUI : MonoBehaviour
    {
        private static readonly WorldId[] Worlds =
            { WorldId.SakuraGarden, WorldId.BambooWorkshop, WorldId.MoonShrine };
        private static readonly string[] Names = { "SAKURA GARDEN", "BAMBOO WORKSHOP", "MOON SHRINE" };
        private readonly TMP_Text[] worldStatus = new TMP_Text[3];
        private ScreenManager screens;
        private RectTransform safeArea, surface;
        private GameObject journey, credits;
        private TMP_Text levelCount;
        private Texture2D washTexture;
        private Sprite washSprite;
        private Material artworkMaterial;

        public void Configure(ScreenManager manager)
        {
            screens = manager;
            RectTransform root = (RectTransform)transform;
            Stretch(root);
            var paper = gameObject.AddComponent<Image>();
            paper.color = ColorForUi(new Color32(255, 247, 236, 255));
            CreateWashes();
            safeArea = Rect("SafeArea", transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaPanel>();
            surface = Rect("Surface", safeArea);
            surface.sizeDelta = new Vector2(1080, 680);
            journey = Rect("Journey", surface).gameObject;
            Stretch((RectTransform)journey.transform);
            credits = Rect("Credits", surface).gameObject;
            Stretch((RectTransform)credits.transform);

            Text("Title", journey.transform, "JOURNEY COMPLETE", 46, 240, 1000, 70);
            levelCount = Text("LevelCount", journey.transform, "", 19, 177, 600, 36);
            Shader shader = Resources.Load<Shader>("WorldMap/WorldMapArtwork");
            if (shader != null) artworkMaterial = new Material(shader);
            for (int i = 0; i < Worlds.Length; i++)
            {
                float x = (i - 1) * 340;
                RectTransform art = Rect($"World{i + 1}", journey.transform);
                art.anchoredPosition = new Vector2(x, 25);
                art.sizeDelta = new Vector2(290, 230);
                Image image = art.gameObject.AddComponent<Image>();
                image.sprite = Resources.Load<Sprite>($"WorldMap/Journey/{Worlds[i]}");
                image.preserveAspect = true;
                image.raycastTarget = false;
                if (artworkMaterial != null) image.material = artworkMaterial;
                TMP_Text name = Text($"WorldName{i + 1}", journey.transform, Names[i], 20, -106, 330, 36);
                name.rectTransform.anchoredPosition = new Vector2(x, -106);
                worldStatus[i] = Text($"WorldStatus{i + 1}", journey.transform, "", 15, -141, 330, 30);
                worldStatus[i].rectTransform.anchoredPosition = new Vector2(x, -141);
            }
            Text("Thanks", journey.transform, "THANK YOU FOR PLAYING", 19, -202, 900, 40);
            Button("WorldMapButton", journey.transform, "WORLD MAP", -145, -273,
                () => screens.ShowWorldMap());
            Button("CreditsButton", journey.transform, "CREDITS", 145, -273, ShowCredits);

            Text("Title", credits.transform, "PIPE MUZZLE", 46, 220, 1000, 70);
            Text("Role", credits.transform, "Game Design & Development", 23, 85, 1000, 50);
            Text("Name", credits.transform, "Muhammed Can Arıca", 32, 25, 1000, 60);
            Text("Engine", credits.transform, "Created with Unity", 22, -74, 1000, 50);
            Text("Thanks", credits.transform, "THANK YOU FOR PLAYING", 19, -181, 1000, 50);
            Button("BackButton", credits.transform, "BACK", 0, -273, ShowJourney);
            ShowJourney();
        }

        public void ShowJourney()
        {
            var progress = new WorldProgressService();
            int completed = 0;
            for (int i = 0; i < Worlds.Length; i++)
            {
                bool done = progress.IsWorldCompleted(Worlds[i]);
                if (done) completed += 12;
                worldStatus[i].text = done ? "COMPLETE" : "";
            }
            levelCount.text = $"{completed} / 36 LEVELS";
            credits.SetActive(false);
            journey.SetActive(true);
            FitSurface();
        }

        private void ShowCredits()
        {
            journey.SetActive(false);
            credits.SetActive(true);
        }

        private void LateUpdate() => FitSurface();

        private void FitSurface()
        {
            if (safeArea == null || surface == null) return;
            Vector2 available = safeArea.rect.size;
            float scale = Mathf.Min(1, Mathf.Max(0.01f, (available.x - 48) / 1080),
                Mathf.Max(0.01f, (available.y - 48) / 680));
            surface.localScale = Vector3.one * scale;
        }

        private void CreateWashes()
        {
            const int size = 128;
            washTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1).magnitude;
                float noise = Mathf.PerlinNoise(x * .055f, y * .055f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1 - distance), 1.4f) * (.6f + noise * .4f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            washTexture.SetPixels(pixels);
            washTexture.Apply();
            washSprite = Sprite.Create(washTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            Color[] colors = { new Color(0.91f, .59f, .66f, .18f),
                new Color(.49f, .68f, .49f, .18f), new Color(.60f, .54f, .78f, .18f) };
            for (int i = 0; i < colors.Length; i++)
            {
                RectTransform wash = Rect($"Wash{i + 1}", transform);
                wash.anchorMin = wash.anchorMax = new Vector2(.15f + i * .35f, i == 1 ? .30f : .65f);
                wash.sizeDelta = new Vector2(950, 800);
                Image image = wash.gameObject.AddComponent<Image>();
                image.sprite = washSprite;
                image.color = ColorForUi(colors[i]);
                image.raycastTarget = false;
            }
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static TMP_Text Text(string name, Transform parent, string value, float size,
            float y, float width, float height)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(width, height);
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = ColorForUi(new Color32(94, 80, 101, 255));
            text.raycastTarget = false;
            return text;
        }

        private static void Button(string name, Transform parent, string label, float x, float y,
            UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = Rect(name, parent);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(260, 68);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = ColorForUi(new Color32(239, 225, 228, 255));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            TMP_Text text = Text("Label", rect, label, 22, 0, 250, 60);
            text.characterSpacing = 1.4f;
        }

        private void OnDestroy()
        {
            Release(artworkMaterial);
            Release(washSprite);
            Release(washTexture);
        }

        private static void Release(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }

        private static Color ColorForUi(Color color) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
    }
}
