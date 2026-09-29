using System;
using System.Collections.Generic;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    [DisallowMultipleComponent]
    public sealed class WorldMapUI : MonoBehaviour
    {
        public event Action<WorldDefinition> WorldSelected;

        private readonly List<WorldDefinition> worlds = new();
        private readonly List<Card> cards = new();
        private WorldProgressService progress;
        private Sprite cardSprite;
        private Texture2D cardTexture;
        private RectTransform artwork;
        private RectTransform safeAreaRoot;
        private TMP_Text mapTitle;
        private TMP_Text mapSubtitle;
        private readonly List<Route> routes = new();
        private bool layoutInitialized;
        private bool portraitLayout;

        private sealed class Route
        {
            public Image[] Dots;
            public Image Node;
        }

        private sealed class Card
        {
            public WorldDefinition World;
            public Button Button;
            public Image Destination;
            public Image LabelSurface;
            public Outline LabelBorder;
            public GameObject LockIcon;
            public GameObject FlowerIcon;
        }

        public void Initialize()
        {
            if (progress != null) return;

            progress = new WorldProgressService();
            worlds.AddRange(ValidateWorlds(Resources.LoadAll<WorldDefinition>("Worlds")));
            worlds.Sort((first, second) => first.WorldId.CompareTo(second.WorldId));
            Build();
            Refresh();
        }

        private static List<WorldDefinition> ValidateWorlds(IEnumerable<WorldDefinition> candidates)
        {
            List<WorldDefinition> valid = new();
            HashSet<WorldId> seen = new();
            HashSet<WorldId> duplicates = new();
            foreach (WorldDefinition world in candidates)
            {
                if (world == null) continue;
                WorldId id = world.WorldId;
                if (id != WorldId.SakuraGarden &&
                    id != WorldId.BambooWorkshop &&
                    id != WorldId.MoonShrine)
                {
                    Debug.LogError($"WorldMapUI skipped an unknown WorldId: {(int)id}.");
                    continue;
                }
                if (!seen.Add(id))
                {
                    if (duplicates.Add(id))
                        Debug.LogError($"WorldMapUI skipped a duplicate WorldId: {id}.");
                    valid.RemoveAll(candidate => candidate.WorldId == id);
                    continue;
                }
                valid.Add(world);
            }
            foreach (WorldId id in new[] { WorldId.SakuraGarden, WorldId.BambooWorkshop, WorldId.MoonShrine })
            {
                if (!valid.Exists(world => world.WorldId == id))
                    Debug.LogError($"WorldMapUI has no valid asset for WorldId: {id}.");
            }
            return valid;
        }

        public void Refresh()
        {
            if (progress == null) return;
            foreach (Card card in cards)
            {
                WorldAccessState state = progress.GetAccessState(card.World);
                card.Button.interactable = state == WorldAccessState.Playable;
                bool locked = state == WorldAccessState.Locked;
                card.Destination.color = locked ? new Color(1f, 1f, 1f, .82f) : Color.white;
                card.LabelSurface.color = locked
                    ? new Color(1f, .98f, .98f, .95f)
                    : new Color(1f, .97f, .96f, .97f);
                Color border = locked ? new Color32(169, 158, 172, 255) : Accent(card.World.WorldId);
                border.a = locked ? .14f : .22f;
                card.LabelBorder.effectColor = border;
                card.LockIcon.SetActive(locked);
                card.FlowerIcon.SetActive(!locked);
            }
        }

        private void Build()
        {
            RectTransform root = transform as RectTransform;
            EnsureCardSprite();
            Image background = CreateImage("Background", root, Vector2.zero, new Color32(255, 249, 246, 255), Vector2.zero, Vector2.one);
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;
            GameObject safeArea = new("MapSafeArea", typeof(RectTransform));
            safeArea.transform.SetParent(root, false);
            safeAreaRoot = safeArea.GetComponent<RectTransform>();
            safeAreaRoot.anchorMin = Vector2.zero;
            safeAreaRoot.anchorMax = Vector2.one;
            safeAreaRoot.offsetMin = safeAreaRoot.offsetMax = Vector2.zero;
            safeArea.AddComponent<SafeAreaPanel>();
            GameObject layout = new("MapArtwork", typeof(RectTransform));
            layout.transform.SetParent(safeAreaRoot, false);
            artwork = layout.GetComponent<RectTransform>();
            artwork.anchorMin = artwork.anchorMax = new Vector2(.5f, .5f);
            mapTitle = CreateText("PIPE MUZZLE", artwork, Vector2.zero, 48f, new Color32(83, 67, 83, 255));
            mapTitle.characterSpacing = 7f;
            mapSubtitle = CreateText("WORLD MAP", artwork, Vector2.zero, 21f, new Color32(188, 132, 151, 255));
            mapSubtitle.characterSpacing = 10f;

            CreateRoute(artwork);
            CreateRoute(artwork);
            foreach (WorldDefinition world in worlds)
                CreateDestination(artwork, world);
            FitArtwork();
        }

        private void OnRectTransformDimensionsChange() => FitArtwork();
        private void LateUpdate() => FitArtwork();

        private void FitArtwork()
        {
            if (artwork == null || safeAreaRoot == null || mapTitle == null) return;
            Rect available = safeAreaRoot.rect;
            bool portrait = available.height > available.width;
            if (!layoutInitialized || portrait != portraitLayout) ApplyLayout(portrait);
            float scale = Mathf.Min((available.width - 32f) / artwork.sizeDelta.x,
                (available.height - 32f) / artwork.sizeDelta.y, 1.35f);
            artwork.localScale = Vector3.one * Mathf.Max(scale, .01f);
        }

        private void ApplyLayout(bool portrait)
        {
            portraitLayout = portrait;
            layoutInitialized = true;
            artwork.sizeDelta = portrait ? new Vector2(1000f, 1300f) : new Vector2(1600f, 1080f);
            mapTitle.rectTransform.anchoredPosition = new Vector2(0f, portrait ? 590f : 480f);
            mapSubtitle.rectTransform.anchoredPosition = new Vector2(0f, portrait ? 545f : 435f);
            foreach (Card card in cards)
            {
                RectTransform rect = card.Destination.rectTransform;
                rect.anchoredPosition = DestinationPosition(card.World.WorldId, portrait);
                rect.sizeDelta = portrait ? new Vector2(420f, 315f) : new Vector2(520f, 390f);
                card.LabelSurface.rectTransform.anchoredPosition = LabelPosition(card.World.WorldId, portrait);
            }
            if (portrait)
            {
                PositionRoute(routes[0], new Vector2(-5f, -210f), new Vector2(150f, -265f), new Vector2(60f, -165f));
                PositionRoute(routes[1], new Vector2(70f, 185f), new Vector2(170f, 260f), new Vector2(65f, 265f));
            }
            else
            {
                PositionRoute(routes[0], new Vector2(-200f, -50f), new Vector2(-15f, 30f), new Vector2(80f, -70f));
                PositionRoute(routes[1], new Vector2(90f, 170f), new Vector2(205f, 260f), new Vector2(75f, 275f));
            }
        }

        private void CreateDestination(RectTransform root, WorldDefinition world)
        {
            GameObject card = new(world.DisplayName, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(root, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            Image image = card.GetComponent<Image>();
            image.sprite = DestinationSprite(world.WorldId);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            Button button = card.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, .92f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(() => WorldSelected?.Invoke(world));
            Image labelSurface = CreateImage("WorldLabelBubble", rect, Vector2.zero, Color.white, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            labelSurface.raycastTarget = true;
            labelSurface.sprite = cardSprite;
            labelSurface.type = Image.Type.Sliced;
            labelSurface.rectTransform.sizeDelta = new Vector2(274f, 68f);
            Outline labelBorder = labelSurface.gameObject.AddComponent<Outline>();
            labelBorder.effectDistance = Vector2.one;
            Shadow labelShadow = labelSurface.gameObject.AddComponent<Shadow>();
            labelShadow.effectColor = new Color(.55f, .35f, .42f, .07f);
            labelShadow.effectDistance = new Vector2(0f, -2f);

            Image iconBadge = CreateImage("IconBadge", labelSurface.rectTransform, new Vector2(-111f, 0f), CardSurface(world.WorldId), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            iconBadge.sprite = cardSprite;
            iconBadge.type = Image.Type.Sliced;
            iconBadge.rectTransform.sizeDelta = new Vector2(46f, 46f);
            GameObject lockIcon = CreateLockIcon(iconBadge.rectTransform);
            GameObject flowerIcon = CreateFlowerIcon(iconBadge.rectTransform, Accent(world.WorldId));
            TMP_Text name = CreateText(world.DisplayName, labelSurface.rectTransform, new Vector2(26f, 10f), 19f, new Color32(82, 67, 82, 255));
            name.alignment = TextAlignmentOptions.Left;
            name.rectTransform.sizeDelta = new Vector2(204f, 27f);
            TMP_Text levels = CreateText($"{world.LevelCount} LEVELS", labelSurface.rectTransform, new Vector2(26f, -15f), 13f, new Color32(171, 143, 158, 255));
            levels.alignment = TextAlignmentOptions.Left;
            levels.fontStyle = FontStyles.Normal;
            levels.characterSpacing = 2f;
            levels.rectTransform.sizeDelta = new Vector2(204f, 25f);
            cards.Add(new Card { World = world, Button = button, Destination = image, LabelSurface = labelSurface, LabelBorder = labelBorder, LockIcon = lockIcon, FlowerIcon = flowerIcon });
        }

        private GameObject CreateFlowerIcon(RectTransform parent, Color color)
        {
            GameObject flower = new("FlowerIcon", typeof(RectTransform));
            flower.transform.SetParent(parent, false);
            RectTransform flowerRect = flower.GetComponent<RectTransform>();
            flowerRect.anchorMin = flowerRect.anchorMax = new Vector2(.5f, .5f);
            flowerRect.sizeDelta = new Vector2(52f, 52f);
            for (int index = 0; index < 5; index++)
            {
                float angle = index * Mathf.PI * 2f / 5f;
                Vector2 position = new(Mathf.Sin(angle) * 12f, Mathf.Cos(angle) * 12f);
                Image petal = CreateImage("Petal", flowerRect, position, color, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
                petal.sprite = cardSprite;
                petal.type = Image.Type.Sliced;
                petal.rectTransform.sizeDelta = new Vector2(16f, 20f);
                petal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -index * 72f);
            }
            Image center = CreateImage("FlowerCenter", flowerRect, Vector2.zero, new Color32(255, 216, 120, 255), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            center.sprite = cardSprite;
            center.type = Image.Type.Sliced;
            center.rectTransform.sizeDelta = new Vector2(12f, 12f);
            return flower;
        }

        private GameObject CreateLockIcon(RectTransform parent)
        {
            GameObject lockIcon = new("LockIcon", typeof(RectTransform));
            lockIcon.transform.SetParent(parent, false);
            RectTransform lockRect = lockIcon.GetComponent<RectTransform>();
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(.5f, .5f);
            lockRect.sizeDelta = new Vector2(60f, 60f);
            Image shackle = CreateImage("LockShackle", lockRect, new Vector2(0f, 8f), new Color32(105, 96, 119, 255), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            shackle.sprite = cardSprite;
            shackle.type = Image.Type.Sliced;
            shackle.rectTransform.sizeDelta = new Vector2(28f, 31f);
            Image opening = CreateImage("LockOpening", shackle.rectTransform, new Vector2(0f, -2f), CardSurface(WorldId.MoonShrine), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            opening.sprite = cardSprite;
            opening.type = Image.Type.Sliced;
            opening.rectTransform.sizeDelta = new Vector2(16f, 24f);
            Image body = CreateImage("LockBody", lockRect, new Vector2(0f, -10f), new Color32(105, 96, 119, 255), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            body.sprite = cardSprite;
            body.type = Image.Type.Sliced;
            body.rectTransform.sizeDelta = new Vector2(36f, 28f);
            return lockIcon;
        }

        private static Vector2 DestinationPosition(WorldId world, bool portrait) => world switch
        {
            WorldId.SakuraGarden => portrait ? new Vector2(-225f, -310f) : new Vector2(-470f, -230f),
            WorldId.BambooWorkshop => portrait ? new Vector2(225f, 15f) : new Vector2(350f, -40f),
            _ => portrait ? new Vector2(-160f, 350f) : new Vector2(-190f, 220f)
        };

        private static Vector2 LabelPosition(WorldId world, bool portrait) => portrait ? new Vector2(0f, -195f) : world switch
        {
            WorldId.SakuraGarden => new Vector2(405f, -20f),
            WorldId.BambooWorkshop => new Vector2(0f, -235f),
            _ => new Vector2(405f, 115f)
        };

        private void CreateRoute(RectTransform root)
        {
            const int dotCount = 24;
            Route route = new() { Dots = new Image[dotCount + 1] };
            for (int index = 0; index <= dotCount; index++)
            {
                Image dot = CreateImage("JourneyPath", root, Vector2.zero, new Color32(216, 167, 184, 155), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
                dot.sprite = cardSprite;
                dot.type = Image.Type.Sliced;
                dot.rectTransform.sizeDelta = new Vector2(6f, 6f);
                route.Dots[index] = dot;
            }
            Image node = CreateImage("JourneyNode", root, Vector2.zero, new Color32(216, 167, 184, 230), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            node.sprite = cardSprite;
            node.type = Image.Type.Sliced;
            node.rectTransform.sizeDelta = new Vector2(22f, 22f);
            Image center = CreateImage("JourneyNodeCenter", node.rectTransform, Vector2.zero, new Color32(255, 244, 243, 255), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            center.sprite = cardSprite;
            center.type = Image.Type.Sliced;
            center.rectTransform.sizeDelta = new Vector2(12f, 12f);
            route.Node = node;
            routes.Add(route);
        }

        private static void PositionRoute(Route route, Vector2 start, Vector2 control, Vector2 end)
        {
            for (int index = 0; index < route.Dots.Length; index++)
            {
                float t = index / (float)(route.Dots.Length - 1);
                route.Dots[index].rectTransform.anchoredPosition = Vector2.Lerp(
                    Vector2.Lerp(start, control, t), Vector2.Lerp(control, end, t), t);
            }
            route.Node.rectTransform.anchoredPosition = Vector2.Lerp(
                Vector2.Lerp(start, control, .5f), Vector2.Lerp(control, end, .5f), .5f);
        }

        private static Image CreateImage(string name, RectTransform parent, Vector2 position, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            Image image = item.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(string value, RectTransform parent, Vector2 position, float size, Color color)
        {
            GameObject item = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(560, 60);
            TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = size; text.fontStyle = FontStyles.Bold; text.alignment = TextAlignmentOptions.Center; text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Color Accent(WorldId world) => world switch
        {
            WorldId.SakuraGarden => new Color32(230, 111, 146, 255),
            WorldId.BambooWorkshop => new Color32(103, 143, 86, 255),
            _ => new Color32(91, 101, 157, 255)
        };

        private static Color CardSurface(WorldId world) => world switch
        {
            WorldId.SakuraGarden => new Color32(255, 244, 239, 255),
            WorldId.BambooWorkshop => new Color32(249, 241, 220, 255),
            _ => new Color32(237, 241, 250, 255)
        };

        private static Sprite DestinationSprite(WorldId world) => Resources.Load<Sprite>(world switch
        {
            WorldId.SakuraGarden => "WorldMap/FinalSakuraGarden",
            WorldId.BambooWorkshop => "WorldMap/FinalBambooWorkshop",
            _ => "WorldMap/FinalMoonShrine"
        });

        private void EnsureCardSprite()
        {
            if (cardSprite != null) return;
            const int size = 64;
            const int radius = 30;
            cardTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "World Map Card Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = new((size - 1) * .5f, (size - 1) * .5f);
            float inset = center.x - radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 offset = new(Mathf.Abs(x - center.x), Mathf.Abs(y - center.y));
                    Vector2 corner = new(Mathf.Max(offset.x - inset, 0f), Mathf.Max(offset.y - inset, 0f));
                    float alpha = corner.sqrMagnitude <= radius * radius ? 1f : 0f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            cardTexture.SetPixels(pixels);
            cardTexture.Apply(false, false);
            cardSprite = Sprite.Create(cardTexture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            cardSprite.name = "World Map Rounded Card";
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                Destroy(cardSprite);
                Destroy(cardTexture);
            }
            else
            {
                DestroyImmediate(cardSprite);
                DestroyImmediate(cardTexture);
            }
        }
    }
}
