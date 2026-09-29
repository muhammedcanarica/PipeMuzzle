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

        private sealed class Card
        {
            public WorldDefinition World;
            public Button Button;
            public Image Background;
            public Image AccentBand;
            public Image Artwork;
            public TMP_Text Status;
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
                card.Background.color = locked
                    ? new Color32(225, 219, 223, 255)
                    : CardSurface(card.World.WorldId);
                card.AccentBand.color = locked
                    ? new Color32(151, 141, 150, 255)
                    : Accent(card.World.WorldId);
                card.Artwork.color = locked
                    ? new Color(.55f, .53f, .58f, .62f)
                    : Color.white;
                card.Status.text = state switch
                {
                    WorldAccessState.Locked => "LOCKED",
                    WorldAccessState.ComingSoon => "COMING SOON",
                    _ => $"{card.World.LevelCount} LEVELS"
                };
            }
        }

        private void Build()
        {
            RectTransform root = transform as RectTransform;
            EnsureCardSprite();
            Image background = CreateImage("Background", root, Vector2.zero, new Color32(255, 242, 237, 255), Vector2.zero, Vector2.one);
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;
            CreateText("PIPE MUZZLE", root, new Vector2(0, 410), 48, new Color32(73, 54, 70, 255));
            CreateText("WORLD MAP", root, new Vector2(0, 350), 24, new Color32(174, 105, 128, 255));

            float[] positions = { -265f, 0f, 265f };
            for (int index = 0; index < worlds.Count && index < positions.Length; index++)
            {
                WorldDefinition world = worlds[index];
                if (index > 0) CreateRoute(root, positions[index - 1] + 120f);
                CreateDestination(root, world, positions[index]);
            }
        }

        private void CreateDestination(RectTransform root, WorldDefinition world, float y)
        {
            GameObject card = new(world.DisplayName, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(root, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(610, 154);
            Image image = card.GetComponent<Image>();
            image.sprite = cardSprite;
            image.type = Image.Type.Sliced;
            image.color = CardSurface(world.WorldId);
            Button button = card.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => WorldSelected?.Invoke(world));
            Shadow shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.18f, 0.13f, 0.16f, .14f);
            shadow.effectDistance = new Vector2(0f, -4f);
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.17f, 0.2f, .12f);
            outline.effectDistance = new Vector2(1f, -1f);

            Image accentBand = CreateImage("AccentBand", rect, new Vector2(0f, 57f), Accent(world.WorldId), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            accentBand.rectTransform.sizeDelta = new Vector2(570f, 14f);

            Image artworkFrame = CreateImage("WorldArtworkFrame", rect, new Vector2(-188f, -3f), new Color(1f, 1f, 1f, .92f), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            artworkFrame.sprite = cardSprite;
            artworkFrame.type = Image.Type.Sliced;
            artworkFrame.rectTransform.sizeDelta = new Vector2(184f, 112f);
            Shadow artworkShadow = artworkFrame.gameObject.AddComponent<Shadow>();
            artworkShadow.effectColor = new Color(0.14f, .1f, .12f, .16f);
            artworkShadow.effectDistance = new Vector2(0f, -2f);

            Image artwork = CreateImage("WorldArtwork", artworkFrame.rectTransform, Vector2.zero, Color.white, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            artwork.sprite = world.Story != null && world.Story.PanelCount > 0
                ? world.Story.Panels[0]
                : null;
            artwork.preserveAspect = true;
            artwork.rectTransform.sizeDelta = new Vector2(168f, 96f);

            TMP_Text tagline = CreateText(WorldTagline(world.WorldId), rect, new Vector2(103f, 29f), 15f, Accent(world.WorldId));
            tagline.rectTransform.sizeDelta = new Vector2(305f, 28f);
            TMP_Text title = CreateText(world.DisplayName, rect, new Vector2(103f, -2f), 28f, new Color32(73, 54, 70, 255));
            title.rectTransform.sizeDelta = new Vector2(330f, 44f);

            Image statusSurface = CreateImage("StatusSurface", rect, new Vector2(103f, -48f), new Color(1f, 1f, 1f, .72f), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            statusSurface.sprite = cardSprite;
            statusSurface.type = Image.Type.Sliced;
            statusSurface.rectTransform.sizeDelta = new Vector2(210f, 30f);
            TMP_Text status = CreateText("LOCKED", rect, new Vector2(103f, -48f), 15f, new Color(0.24f, .2f, .25f, .82f));
            status.rectTransform.sizeDelta = new Vector2(200f, 30f);
            cards.Add(new Card { World = world, Button = button, Background = image, AccentBand = accentBand, Artwork = artwork, Status = status });
        }

        private static void CreateRoute(RectTransform root, float y)
        {
            Image route = CreateImage("JourneyPath", root, new Vector2(0, y), new Color32(228, 158, 178, 255), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            route.rectTransform.sizeDelta = new Vector2(10, 140);
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

        private static string WorldTagline(WorldId world) => world switch
        {
            WorldId.SakuraGarden => "PETAL PATH",
            WorldId.BambooWorkshop => "BAMBOO WORKSHOP",
            _ => "MOONLIT STEPS"
        };

        private void EnsureCardSprite()
        {
            if (cardSprite != null) return;
            const int size = 64;
            const int radius = 13;
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
