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

        private sealed class Card
        {
            public WorldDefinition World;
            public Button Button;
            public Image Background;
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
                    Debug.LogError($"WorldMapUI skipped a duplicate WorldId: {id}.");
                    continue;
                }
                valid.Add(world);
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
                card.Background.color = state == WorldAccessState.Playable
                    ? Accent(card.World.WorldId)
                    : new Color32(151, 141, 150, 255);
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
            Image background = CreateImage("Background", root, Vector2.zero, new Color32(255, 239, 235, 255), Vector2.zero, Vector2.one);
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
            rect.sizeDelta = new Vector2(610, 140);
            Image image = card.GetComponent<Image>();
            Button button = card.GetComponent<Button>();
            button.onClick.AddListener(() => WorldSelected?.Invoke(world));
            CreateText(world.DisplayName, rect, new Vector2(0, 20), 30, Color.white);
            TMP_Text status = CreateText("LOCKED", rect, new Vector2(0, -28), 18, new Color(1f, 1f, 1f, .88f));
            cards.Add(new Card { World = world, Button = button, Background = image, Status = status });
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
    }
}
