using System;
using System.Collections;
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
        private Button settingsButton;
        private RectTransform settingsEntryArea;
        private Vector2 settingsEntrySize;

        public void ConfigureSettings(ScreenManager screens)
        {
            if (settingsButton != null) return;
            GameObject area = new("SettingsEntry", typeof(RectTransform));
            area.transform.SetParent(transform, false);
            settingsEntryArea = (RectTransform)area.transform;
            settingsEntryArea.anchorMin = Vector2.zero;
            settingsEntryArea.anchorMax = Vector2.one;
            settingsEntryArea.offsetMin = settingsEntryArea.offsetMax = Vector2.zero;
            area.AddComponent<SafeAreaPanel>();
            TMP_Text label = CreateText("SettingsButton", "SETTINGS", settingsEntryArea, 18f, new Color32(148, 118, 116, 255));
            label.characterSpacing = 1.4f;
            label.raycastTarget = true;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -12f);
            rect.sizeDelta = new Vector2(116f, 76f);
            settingsButton = label.gameObject.AddComponent<Button>();
            settingsButton.targetGraphic = label;
            settingsButton.onClick.AddListener(screens.ShowSettings);
            LayoutSettingsEntry();
        }

        private void LateUpdate()
        {
            if (settingsEntryArea != null && settingsEntrySize != settingsEntryArea.rect.size)
                LayoutSettingsEntry();
        }

        private void LayoutSettingsEntry()
        {
            if (settingsButton == null) return;
            settingsEntrySize = settingsEntryArea.rect.size;
            float preferred = Mathf.Lerp(1f, 2.4f, Mathf.InverseLerp(1f, 1.65f, settingsEntrySize.y / Mathf.Max(1f, settingsEntrySize.x)));
            float scale = Mathf.Max(.1f, Mathf.Min(preferred, settingsEntrySize.x / 524f, settingsEntrySize.y / 474f));
            RectTransform rect = (RectTransform)settingsButton.transform;
            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = new Vector2(-20f, -8f) * scale;
        }

        [SerializeField] private Sprite characterMarkerSprite;
        private const float TravelDuration = .45f;

        private readonly List<WorldDefinition> worlds = new();
        private readonly List<Destination> destinations = new();
        private readonly List<Image[]> routes = new();
        private WorldProgressService progress;
        private Sprite cardSprite;
        private Texture2D cardTexture;
        private Sprite pathSprite;
        private Texture2D pathTexture;
        private Sprite paperSprite;
        private Texture2D paperTexture;
        private Material lockedArtworkMaterial;
        private Material artworkMaterial;
        private Image characterMarker;
        private Image markerHead;
        private Image markerBody;
        private Destination currentDestination;
        private Destination travelDestination;
        private Coroutine travelRoutine;
        private TMP_Text brandTitle;
        private TMP_Text journeyLabel;

        private sealed class Destination
        {
            public WorldDefinition World;
            public RectTransform Region;
            public WorldMapDestinationButton Button;
            public Image Artwork;
            public Image HoverWash;
            public Image Node;
            public TMP_Text Name;
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
            foreach (Destination destination in destinations) ApplyDestinationState(destination);
            if (Application.isPlaying && travelDestination == null)
            {
                WorldDefinition activeWorld = FindFirstObjectByType<GameController>()?.CurrentWorld;
                Destination active = destinations.Find(item => item.World == activeWorld &&
                    progress.GetAccessState(item.World) == WorldAccessState.Playable);
                if (active != null) currentDestination = active;
            }
            LayoutDestinations();
        }

        private void ApplyDestinationState(Destination destination)
        {
            WorldAccessState state = progress.GetAccessState(destination.World);
            bool playable = state == WorldAccessState.Playable;
            destination.Button.interactable = playable;
            destination.Artwork.color = playable ? Color.white : new Color(0.86f, 0.86f, 0.86f, 0.82f);
            destination.Artwork.material = playable ? artworkMaterial : lockedArtworkMaterial;
            destination.Name.color = UiColor(playable ? Accent(destination.World.WorldId) : new Color32(111, 105, 109, 255));
            destination.Status.text = state switch
            {
                WorldAccessState.Locked => "LOCKED",
                WorldAccessState.ComingSoon => "COMING SOON",
                _ when progress.IsWorldCompleted(destination.World.WorldId) => "COMPLETE",
                _ => $"{destination.World.LevelCount} LEVELS"
            };
            Color accent = playable ? Accent(destination.World.WorldId) : new Color32(128, 121, 125, 255);
            destination.Status.color = UiColor(accent);
            destination.Node.color = new Color(accent.r, accent.g, accent.b, playable ? .6f : .23f);
            destination.Button.RefreshVisualState();
        }

        private void Build()
        {
            RectTransform root = (RectTransform)transform;
            EnsureCardSprite();
            CreatePaperBackground(root);
            WorldMapBackgroundDecor.Create(root);
            WorldMapPetalAmbient.Create(root);
            Shader artworkShader = Resources.Load<Shader>("WorldMap/WorldMapArtwork");
            if (artworkShader != null)
            {
                lockedArtworkMaterial = new Material(artworkShader) { name = "World Map Muted Artwork" };
                lockedArtworkMaterial.SetFloat("_Saturation", .22f);
                artworkMaterial = new Material(artworkShader) { name = "World Map Paper Artwork" };
                // New regional sprites already carry organic alpha edges; do not fade the landmark itself.
                artworkMaterial.SetFloat("_EdgeSoftness", 0f);
                lockedArtworkMaterial.SetFloat("_EdgeSoftness", 0f);
            }
            else Debug.LogError("WorldMapUI missing Resources/WorldMap/WorldMapArtwork shader; using muted tint fallback.");
            brandTitle = CreateText("BrandTitle", "PIPE MUZZLE", root, 40f, new Color32(83, 65, 76, 255));
            journeyLabel = CreateText("JourneyLabel", "JOURNEY", root, 17f, new Color32(148, 118, 116, 255));
            journeyLabel.characterSpacing = 8f;
            // Path graphics are behind the art, and never intercept destination input.
            for (int index = 1; index < worlds.Count; index++)
            {
                Image[] segments = new Image[64];
                for (int segment = 0; segment < segments.Length; segment++)
                {
                    segments[segment] = CreateImage("JourneyPath", root, new Color(.64f, .53f, .40f, .24f));
                    segments[segment].sprite = pathSprite;
                }
                routes.Add(segments);
            }
            foreach (WorldDefinition world in worlds) CreateDestination(root, world);
            CreateCharacterMarker(root);
            currentDestination = destinations.Find(destination => progress.GetAccessState(destination.World) == WorldAccessState.Playable);
        }

        private void CreateDestination(RectTransform root, WorldDefinition world)
        {
            GameObject node = new(world.DisplayName, typeof(RectTransform));
            node.transform.SetParent(root, false);
            RectTransform rect = (RectTransform)node.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            // Only the small stop is interactive; the regional illustration is map scenery.
            Image wash = CreateImage("HoverWash", rect, Color.clear);
            wash.sprite = cardSprite;
            Image anchor = CreateImage("WorldNode", rect, Color.white);
            anchor.sprite = cardSprite;
            anchor.raycastTarget = true;
            WorldMapDestinationButton button = anchor.gameObject.AddComponent<WorldMapDestinationButton>();
            button.targetGraphic = anchor;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                if (button.IsInteractable()) SelectDestination(world);
            });
            Image artwork = CreateImage("Artwork", rect, Color.white);
            artwork.sprite = LoadArtwork(world.WorldId);
            artwork.preserveAspect = true;
            if (artwork.sprite == null)
            {
                artwork.sprite = cardSprite;
                artwork.type = Image.Type.Sliced;
                artwork.color = new Color(1f, .96f, .9f, .5f);
            }
            TMP_Text name = CreateText("WorldName", world.DisplayName, rect, 20f, Accent(world.WorldId));
            name.characterSpacing = .4f;
            TMP_Text status = CreateText("Status", "LOCKED", rect, 14f, Accent(world.WorldId));
            status.characterSpacing = 1.6f;
            button.Configure(wash, anchor.rectTransform);
            destinations.Add(new Destination { World = world, Region = rect, Button = button, Artwork = artwork,
                HoverWash = wash, Node = anchor, Name = name, Status = status });
        }

        private void CreateCharacterMarker(RectTransform root)
        {
            characterMarker = CreateImage("CharacterMarker", root, Color.white);
            characterMarker.preserveAspect = true;
            // A small silhouette is used until a dedicated character sprite is supplied.
            markerHead = CreateImage("SilhouetteHead", characterMarker.rectTransform, new Color32(108, 87, 96, 255));
            markerHead.sprite = cardSprite;
            markerBody = CreateImage("SilhouetteBody", characterMarker.rectTransform, new Color32(108, 87, 96, 255));
            markerBody.sprite = cardSprite;
            SetMarkerSprite(characterMarkerSprite != null ? characterMarkerSprite : Resources.Load<Sprite>("WorldMap/Journey/ChibiTraveler"));
        }

        public void SetMarkerSprite(Sprite sprite)
        {
            characterMarkerSprite = sprite;
            if (characterMarker == null) return;
            characterMarker.sprite = sprite;
            characterMarker.enabled = sprite != null;
            markerHead.gameObject.SetActive(sprite == null);
            markerBody.gameObject.SetActive(sprite == null);
        }

        private void SelectDestination(WorldDefinition world)
        {
            if (travelDestination != null || progress.GetAccessState(world) != WorldAccessState.Playable) return;
            Destination destination = destinations.Find(item => item.World == world);
            if (destination == null) return;
            // EditMode does not run frame-driven coroutines; navigation still uses the same terminal method.
            if (!Application.isPlaying)
            {
                CompleteTravel(destination);
                return;
            }
            travelDestination = destination;
            travelRoutine = StartCoroutine(TravelTo(destination));
        }

        private IEnumerator TravelTo(Destination destination)
        {
            Vector2 start = characterMarker.rectTransform.anchoredPosition;
            double started = Time.realtimeSinceStartupAsDouble;
            float elapsed = 0f;
            while (elapsed < TravelDuration)
            {
                // Count time since the click, not the possibly long frame before it.
                elapsed = (float)(Time.realtimeSinceStartupAsDouble - started);
                float t = Mathf.Clamp01(elapsed / TravelDuration);
                float eased = t * t * (3f - 2f * t);
                Vector2 end = MarkerPosition(destination);
                Vector2 curve = Vector2.right * Mathf.Sign(end.x - start.x) * characterMarker.rectTransform.sizeDelta.x * .42f;
                characterMarker.rectTransform.anchoredPosition = Vector2.Lerp(start, end, eased) + curve * Mathf.Sin(eased * Mathf.PI);
                yield return null;
            }
            travelRoutine = null;
            travelDestination = null;
            if (progress.GetAccessState(destination.World) == WorldAccessState.Playable) CompleteTravel(destination);
            else PositionMarker();
        }

        private void CompleteTravel(Destination destination)
        {
            currentDestination = destination;
            PositionMarker();
            WorldSelected?.Invoke(destination.World);
        }

        private Vector2 MarkerPosition(Destination destination) =>
            destination.Region.anchoredPosition + destination.Node.rectTransform.anchoredPosition +
            Vector2.up * characterMarker.rectTransform.sizeDelta.y * .45f;

        private void PositionMarker()
        {
            if (currentDestination != null && characterMarker != null && travelDestination == null)
                characterMarker.rectTransform.anchoredPosition = MarkerPosition(currentDestination);
        }

        private void OnEnable()
        {
            Refresh();
            PositionMarker();
        }

        private void OnDisable()
        {
            if (travelRoutine != null) StopCoroutine(travelRoutine);
            travelRoutine = null;
            travelDestination = null;
            PositionMarker();
        }

        private static Sprite LoadArtwork(WorldId world)
        {
            string primary = $"WorldMap/Journey/{world}";
            Sprite artwork = Resources.Load<Sprite>(primary);
            if (artwork != null) return artwork;
            Debug.LogError($"WorldMapUI missing artwork at Resources/{primary}; using destination fallback.");
            artwork = Resources.Load<Sprite>($"WorldMap/Final{world}");
            if (artwork != null) return artwork;
            string fallback = world switch
            {
                WorldId.SakuraGarden => "WorldMap/SakuraDestination",
                WorldId.BambooWorkshop => "WorldMap/BambooDestination",
                _ => "WorldMap/MoonDestination"
            };
            return Resources.Load<Sprite>(fallback);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (destinations.Count > 0) LayoutDestinations();
        }

        private void LayoutDestinations()
        {
            LayoutSettingsEntry();
            RectTransform root = (RectTransform)transform;
            Vector2 size = root.rect.size;
            if (size.x <= 0f || size.y <= 0f) return;
            float scale = Mathf.Min(1f, size.x / 700f, size.y / 800f);
            SetRect(brandTitle.rectTransform, new Vector2(0f, size.y * .5f - 48f * scale), new Vector2(size.x * .8f, 48f * scale));
            SetRect(journeyLabel.rectTransform, new Vector2(0f, size.y * .5f - 94f * scale), new Vector2(size.x * .8f, 28f * scale));
            brandTitle.fontSize = 40f * scale;
            journeyLabel.fontSize = 17f * scale;
            float top = size.y * .5f - 150f * scale;
            float bottom = -size.y * .5f + 38f * scale;
            float height = Mathf.Max(1f, top - bottom);
            float width = Mathf.Min(1400f, size.x - 72f * scale);
            bool wide = size.x / size.y >= 1.5f;
            float nodeHeight = Mathf.Min((wide ? 440f : 390f) * scale, height * (wide ? .46f : .29f));
            float nodeWidth = Mathf.Min(500f * scale, width * (wide ? .36f : .78f));
            float offset = wide ? width * .24f : Mathf.Min(width * .09f, (width - nodeWidth) * .4f);
            float[] rows = wide ? new[] { .78f, .50f, .22f } : new[] { .82f, .50f, .18f };
            for (int index = 0; index < destinations.Count; index++)
            {
                Destination destination = destinations[index];
                float x = index % 2 == 0 ? -offset : offset;
                SetRect(destination.Region, new Vector2(x, bottom + height * rows[index]), new Vector2(nodeWidth, nodeHeight));
                float artSide = Mathf.Max(1f, Mathf.Min(nodeWidth - 120f * scale, nodeHeight - 30f * scale));
                Vector2 artSize = Vector2.one * artSide;
                float inward = index % 2 == 0 ? 1f : -1f;
                SetRect(destination.Artwork.rectTransform, new Vector2(-inward * 36f * scale, 5f * scale), artSize);
                Vector2 anchorPosition = new(inward * (artSide * .5f + 5f * scale), -nodeHeight * .12f);
                SetRect(destination.Node.rectTransform, anchorPosition, Vector2.one * 56f * scale);
                SetRect(destination.HoverWash.rectTransform, anchorPosition, Vector2.one * 72f * scale);
                // Place the name beside the stop, never as a caption under the artwork.
                Vector2 labelPosition = anchorPosition + new Vector2(inward * 144f * scale, 3f * scale);
                SetRect(destination.Name.rectTransform, labelPosition, new Vector2(216f, 28f) * scale);
                destination.Name.fontSize = 20f * scale;
                destination.Name.enableAutoSizing = true;
                destination.Name.fontSizeMin = 15f * scale;
                destination.Name.fontSizeMax = 20f * scale;
                Vector2 badgePosition = labelPosition - Vector2.up * 24f * scale;
                Vector2 badgeSize = new(168f * scale, 20f * scale);
                SetRect(destination.Status.rectTransform, badgePosition, badgeSize);
                destination.Status.fontSize = 11f * scale;
            }
            if (characterMarker != null)
            {
                SetRect(characterMarker.rectTransform, characterMarker.rectTransform.anchoredPosition, Vector2.one * 84f * scale);
                SetRect(markerHead.rectTransform, new Vector2(0f, 10f * scale), Vector2.one * 17f * scale);
                SetRect(markerBody.rectTransform, new Vector2(0f, -8f * scale), new Vector2(26f, 25f) * scale);
                PositionMarker();
            }
            for (int index = 0; index < routes.Count; index++) LayoutRoute(index, wide, scale);
        }

        private void LayoutRoute(int index, bool wide, float scale)
        {
            RectTransform from = destinations[index].Region;
            RectTransform to = destinations[index + 1].Region;
            float direction = Mathf.Sign(to.anchoredPosition.x - from.anchoredPosition.x);
            Vector2 start = from.anchoredPosition + destinations[index].Node.rectTransform.anchoredPosition;
            Vector2 end = to.anchoredPosition + destinations[index + 1].Node.rectTransform.anchoredPosition;
            Vector2 curve = new(direction * 35f * scale, 0f);
            Image[] segments = routes[index];
            for (int segment = 0; segment < segments.Length; segment++)
            {
                float t0 = (segment + .5f) / segments.Length;
                float t1 = (segment + 1f) / segments.Length;
                Vector2 a = Vector2.Lerp(start, end, t0) + curve * Mathf.Sin(t0 * Mathf.PI);
                Vector2 b = Vector2.Lerp(start, end, t1) + curve * Mathf.Sin(t1 * Mathf.PI);
                Vector2 delta = b - a;
                RectTransform rect = segments[segment].rectTransform;
                float variation = .85f + .15f * Mathf.Sin(segment * 2.4f);
                SetRect(rect, a, new Vector2(10f, 5f) * scale * variation);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        private void CreatePaperBackground(RectTransform root)
        {
            const int rows = 256;
            paperTexture = new Texture2D(rows, rows, TextureFormat.RGBA32, false)
                { name = "World Map Watercolor Paper", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color[] pixels = new Color[rows * rows];
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < rows; x++)
            {
                Color color = Color.Lerp(new Color32(252, 239, 229, 255), new Color32(255, 247, 236, 255), y / (float)(rows - 1));
                float grain = (Mathf.PerlinNoise(x * .65f, y * .65f) - .5f) * .012f;
                float wash = (Mathf.PerlinNoise(x * .035f, y * .035f) - .5f) * .008f;
                pixels[y * rows + x] = new Color(color.r + grain + wash, color.g + grain + wash, color.b + grain + wash, 1f);
            }
            paperTexture.SetPixels(pixels);
            paperTexture.Apply(false, false);
            paperSprite = Sprite.Create(paperTexture, new Rect(0, 0, rows, rows), new Vector2(.5f, .5f));
            Image background = CreateImage("Background", root, Color.white);
            background.sprite = paperSprite;
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image CreateImage(string name, RectTransform parent, Color color)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            Image image = item.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(string name, string value, RectTransform parent, float size, Color color)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)item.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = UiColor(color);
            text.raycastTarget = false;
            return text;
        }

        private static Color Accent(WorldId world) => world switch
        {
            WorldId.SakuraGarden => new Color32(154, 78, 104, 255),
            WorldId.BambooWorkshop => new Color32(79, 108, 68, 255),
            _ => new Color32(83, 89, 139, 255)
        };

        // TMP vertex colors need linear values for the intended muted palette in this URP project.
        private static Color UiColor(Color color) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;

        private void EnsureCardSprite()
        {
            if (cardSprite != null) return;
            const int size = 64;
            const int radius = size / 2;
            cardTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "World Map Badge Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = new((size - 1) * .5f, (size - 1) * .5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 offset = new(x - center.x, y - center.y);
                    float distance = offset.magnitude / radius;
                    float noise = Mathf.PerlinNoise(x * .15f, y * .15f);
                    float edge = Mathf.Clamp01((.89f + noise * .06f - distance) * 18f);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - .68f) * 20f);
                    float alpha = edge * (.22f + .65f * ring) * (.8f + noise * .2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            cardTexture.SetPixels(pixels);
            cardTexture.Apply(false, false);
            cardSprite = Sprite.Create(cardTexture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            cardSprite.name = "World Map Badge";
            pathTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = "Map Trail Brush", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = (new Vector2(x, y) - center).magnitude / radius;
                float alpha = Mathf.Clamp01(1f - distance) * (.6f + .4f * Mathf.PerlinNoise(x * .12f, y * .12f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            pathTexture.SetPixels(pixels);
            pathTexture.Apply(false, false);
            pathSprite = Sprite.Create(pathTexture, new Rect(0, 0, size, size), Vector2.one * .5f);
        }

        private void OnDestroy()
        {
            ReleaseOwned(cardSprite);
            ReleaseOwned(cardTexture);
            ReleaseOwned(pathSprite);
            ReleaseOwned(pathTexture);
            ReleaseOwned(paperSprite);
            ReleaseOwned(paperTexture);
            ReleaseOwned(lockedArtworkMaterial);
            ReleaseOwned(artworkMaterial);
        }

        private static void ReleaseOwned(UnityEngine.Object owned)
        {
            if (owned == null) return;
            if (Application.isPlaying) Destroy(owned);
            else DestroyImmediate(owned);
        }
    }
}
