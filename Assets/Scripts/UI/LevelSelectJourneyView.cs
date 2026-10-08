using System.Collections.Generic;
using PipeMuzzle.Data;
using PipeMuzzle.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.UI
{
    public enum LevelJourneyNodeState { Locked, Available, Completed, Current }

    [DisallowMultipleComponent]
    public sealed class LevelSelectJourneyView : MonoBehaviour
    {
        private sealed class Stop
        {
            public Button Button;
            public Image Ring;
            public Image Glow;
            public Image Ornament;
            public TMP_Text Number;
            public LevelSelectNodeFeedback Feedback;
            public LevelJourneyNodeState State;
        }

        private readonly List<Stop> stops = new();
        private readonly List<Image> trail = new();
        private readonly Vector2[] positions = new Vector2[12];
        private LevelJourneyPaint paint;
        private RectTransform panel;
        private RectTransform path;
        private RectTransform trailRoot;
        private TMP_Text title;
        private TMP_Text subtitle;
        private TMP_Text bestPreview;
        private int focusedIndex = -1;
        private Button back;
        private Image environment;
        private Image regionalArt;
        private Image wash;
        private Image edgeWash;
        private Image marker;
        private WorldDefinition world;
        private Color accent;
        private int current = -1;
        private Vector2 lastSize;

        public LevelJourneyNodeState GetNodeState(int index) => stops[index].State;

        public void Configure(WorldDefinition definition, IReadOnlyList<Button> buttons, RectTransform area,
            TMP_Text heading, Image background, Button mapButton, RectTransform oldRoute)
        {
            world = definition;
            panel = (RectTransform)transform;
            path = area;
            title = heading;
            back = mapButton;
            accent = world.WorldId switch
            {
                WorldId.SakuraGarden => new Color32(153, 76, 103, 255),
                WorldId.BambooWorkshop => new Color32(78, 105, 65, 255),
                _ => new Color32(81, 84, 137, 255)
            };
            if (paint == null) Build(buttons);
            if (oldRoute != null) oldRoute.gameObject.SetActive(false);
            if (background != null)
            {
                background.sprite = null;
                background.color = Ui(new Color32(255, 246, 234, 255));
                background.raycastTarget = false;
            }
            environment.sprite = world.LevelSelectTheme.BackgroundSprite;
            environment.color = new Color(1f, 1f, 1f, .14f);
            regionalArt.sprite = Resources.Load<Sprite>("WorldMap/Journey/" + world.WorldId);
            regionalArt.color = new Color(1f, 1f, 1f, .18f);
            wash.color = Ui(WithAlpha(accent, .13f));
            edgeWash.color = Ui(WithAlpha(accent, .10f));
            if (title != null)
            {
                title.text = world.DisplayName.ToUpperInvariant();
                title.color = Ui(accent);
                UiTypography.Apply(title, UiFontRole.Emphasis);
                title.alignment = TextAlignmentOptions.Center;
                title.characterSpacing = .4f;
            }
            subtitle.color = Ui(WithAlpha(accent, .72f));
            bestPreview.color = Ui(WithAlpha(accent, .80f));
            foreach (Stop stop in stops) stop.Feedback.ClearFocus();
            focusedIndex = -1;
            bestPreview.gameObject.SetActive(false);
            if (back != null)
            {
                Image image = back.GetComponent<Image>();
                image.sprite = paint.Sprites[3];
                image.color = Ui(WithAlpha(accent, .12f));
                back.transition = Selectable.Transition.ColorTint;
                ColorBlock colors = ColorBlock.defaultColorBlock;
                colors.normalColor = new Color(1f, 1f, 1f, .85f);
                colors.highlightedColor = Color.white;
                back.colors = colors;
                TMP_Text label = back.GetComponentInChildren<TMP_Text>(true);
                if (label != null) { label.text = "‹  MAP"; label.color = Ui(accent); UiTypography.Apply(label, UiFontRole.Emphasis); }
            }
            lastSize = Vector2.zero;
            Layout();
        }

        private void Build(IReadOnlyList<Button> buttons)
        {
            paint = new LevelJourneyPaint();
            RectTransform backdrop = Layer("LevelJourneyBackdrop", panel);
            backdrop.SetAsFirstSibling();
            environment = Image("WatercolorEnvironment", backdrop);
            Stretch(environment.rectTransform);
            wash = Image("RegionalWash", backdrop, paint.Sprites[3]);
            edgeWash = Image("EdgeMist", backdrop, paint.Sprites[3]);
            regionalArt = Image("DistantLandmark", backdrop);
            regionalArt.preserveAspect = true;
            trailRoot = Layer("LevelJourneyRoute", path);
            trailRoot.SetAsFirstSibling();
            for (int i = 0; i < 11 * 18; i++) trail.Add(Image("TrailBrush", trailRoot, paint.Sprites[2]));
            for (int i = 0; i < buttons.Count && i < 12; i++)
            {
                Button button = buttons[i];
                if (button == null) continue;
                button.transition = Selectable.Transition.None;
                Image image = button.GetComponent<Image>();
                image.sprite = paint.Sprites[0];
                image.type = UnityEngine.UI.Image.Type.Simple;
                image.raycastTarget = true;
                Image glow = Image("CurrentWash", (RectTransform)button.transform, paint.Sprites[3]);
                glow.transform.SetAsFirstSibling();
                Image ring = Image("ProgressRing", (RectTransform)button.transform, paint.Sprites[1]);
                Image ornament = Image("CheckpointOrnament", (RectTransform)button.transform);
                TMP_Text number = button.GetComponentInChildren<TMP_Text>(true);
                if (number == null) number = Text("Number", (RectTransform)button.transform, (i + 1).ToString());
                number.raycastTarget = false;
                number.transform.SetAsLastSibling();
                LevelSelectNodeFeedback feedback = button.GetComponent<LevelSelectNodeFeedback>();
                if (feedback == null) feedback = button.gameObject.AddComponent<LevelSelectNodeFeedback>();
                feedback.FocusChanged += HandleNodeFocusChanged;
                stops.Add(new Stop { Button = button, Ring = ring, Glow = glow, Ornament = ornament, Number = number, Feedback = feedback });
            }
            subtitle = Text("LevelJourneySubtitle", panel, "JOURNEY  ·  12 STOPS");
            bestPreview = Text("LevelJourneyBest", panel, "");
            bestPreview.gameObject.SetActive(false);
            marker = Image("LevelJourneyMarker", panel, Resources.Load<Sprite>("WorldMap/Journey/ChibiTraveler"));
            marker.preserveAspect = true;
            if (title != null) title.transform.SetAsLastSibling();
            if (back != null) back.transform.SetAsLastSibling();
        }

        public void Refresh(GameController controller)
        {
            if (world == null || controller == null) return;
            current = -1;
            for (int i = 0; i < stops.Count; i++) if (controller.IsLevelUnlocked(i)) current = i;
            bool complete = new WorldProgressService().IsWorldCompleted(world.WorldId);
            int ornament = 4 + (int)world.WorldId;
            for (int i = 0; i < stops.Count; i++)
            {
                Stop stop = stops[i];
                bool unlocked = controller.IsLevelUnlocked(i);
                stop.State = !unlocked ? LevelJourneyNodeState.Locked : complete || i < current
                    ? LevelJourneyNodeState.Completed : i == current ? LevelJourneyNodeState.Current : LevelJourneyNodeState.Available;
                bool done = stop.State == LevelJourneyNodeState.Completed;
                bool next = stop.State == LevelJourneyNodeState.Current;
                stop.Button.interactable = unlocked;
                stop.Button.GetComponent<Image>().sprite = paint.Sprites[0];
                stop.Button.GetComponent<Image>().color = Ui(unlocked ? Color.Lerp(Color.white, accent, done ? .16f : .08f) : new Color(.87f, .85f, .82f, .74f));
                stop.Number.text = (i + 1).ToString();
                stop.Number.color = Ui(unlocked ? accent : new Color(.48f, .46f, .45f));
                UiTypography.Apply(stop.Number, UiFontRole.Emphasis);
                stop.Ring.color = Ui(WithAlpha(accent, unlocked ? next ? .72f : done ? .40f : .24f : .12f));
                stop.Glow.color = Ui(WithAlpha(accent, next ? .22f : 0f));
                stop.Ornament.sprite = paint.Sprites[ornament];
                stop.Ornament.color = Ui(WithAlpha(accent, unlocked ? .80f : .25f));
                stop.Ornament.gameObject.SetActive((i + 1) % 3 == 0);
                stop.Feedback.Configure(stop.Button, stop.Glow, next ? .22f : 0f);
            }
            foreach (Image brush in trail) brush.color = Ui(WithAlpha(accent, .25f));
            marker.gameObject.SetActive(current >= 0);
            Layout();
        }

        private void LateUpdate()
        {
            if (panel != null && panel.rect.size != lastSize) Layout();
        }

        private void Layout()
        {
            Vector2 size = panel.rect.size;
            if (size.x <= 0 || size.y <= 0 || stops.Count == 0) return;
            lastSize = size;
            float scale = Mathf.Min(1f, size.x / 850f, size.y / 800f);
            bool wide = size.x / size.y >= 1.45f;
            int columns = wide ? 4 : 3;
            int rows = 12 / columns;
            // Reserve the entire marker height above the first row, including short desktop views.
            Vector2 area = new(Mathf.Min(1160f * scale, size.x * .78f), Mathf.Min(650f * scale, size.y - 370f * scale));
            path.anchorMin = path.anchorMax = Vector2.one * .5f;
            path.pivot = Vector2.one * .5f;
            path.anchoredPosition = new Vector2(0, -30f * scale);
            path.sizeDelta = area;
            if (title != null)
            {
                Center(title.rectTransform, new Vector2(0, size.y * .5f - 58f * scale), new Vector2(size.x * .66f, 48f * scale));
                title.fontSize = 32f * scale;
            }
            Center(subtitle.rectTransform, new Vector2(0, size.y * .5f - 100f * scale), new Vector2(size.x * .7f, 26f * scale));
            subtitle.fontSize = 13f * scale;
            subtitle.characterSpacing = 3f;
            Center(bestPreview.rectTransform, new Vector2(0, -size.y * .5f + 40f * scale), new Vector2(size.x * .7f, 28f * scale));
            bestPreview.fontSize = 17f * scale;
            bestPreview.characterSpacing = 1f;
            if (back != null)
            {
                Center((RectTransform)back.transform, new Vector2(-size.x * .5f + 84f * scale, size.y * .5f - 54f * scale), new Vector2(112f, 48f) * scale);
                TMP_Text label = back.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.fontSize = 18f * scale;
            }
            Center(wash.rectTransform, new Vector2(-size.x * .22f, size.y * .08f), new Vector2(size.x * .8f, size.y * .72f));
            Center(edgeWash.rectTransform, new Vector2(size.x * .35f, -size.y * .22f), new Vector2(size.x * .65f, size.y * .52f));
            Center(regionalArt.rectTransform, new Vector2(size.x * .42f, size.y * .19f), Vector2.one * Mathf.Min(size.y * .60f, size.x * .44f));
            for (int i = 0; i < stops.Count; i++)
            {
                int row = i / columns;
                int col = row % 2 == 0 ? i % columns : columns - 1 - i % columns;
                float x = Mathf.Lerp(-area.x * .5f + 60f * scale, area.x * .5f - 60f * scale, col / (float)(columns - 1));
                float y = Mathf.Lerp(area.y * .5f - 45f * scale, -area.y * .5f + 45f * scale, row / (float)(rows - 1)) + Mathf.Sin(i * 1.9f) * 15f * scale;
                positions[i] = new Vector2(x, y);
                Stop stop = stops[i];
                RectTransform node = (RectTransform)stop.Button.transform;
                float diameter = (i == 11 ? 96f : (i + 1) % 3 == 0 ? 86f : 76f) * scale;
                Center(node, node.parent.InverseTransformPoint(path.TransformPoint(positions[i])), Vector2.one * diameter);
                Center(stop.Ring.rectTransform, Vector2.zero, Vector2.one * diameter * .98f);
                Center(stop.Glow.rectTransform, Vector2.zero, Vector2.one * diameter * 1.5f);
                Center(stop.Number.rectTransform, Vector2.zero, Vector2.one * diameter * .72f);
                stop.Number.fontSize = 25f * scale;
                Center(stop.Ornament.rectTransform, new Vector2(diameter * .37f, diameter * .32f), Vector2.one * (i == 11 ? 29f : 24f) * scale);
            }
            for (int connection = 0; connection < stops.Count - 1; connection++)
            for (int stamp = 0; stamp < 18; stamp++)
            {
                float t = stamp / 17f;
                Vector2 point = Curve(connection, t);
                Vector2 tangent = Curve(connection, Mathf.Min(t + .02f, 1f)) - Curve(connection, Mathf.Max(t - .02f, 0f));
                RectTransform brush = trail[connection * 18 + stamp].rectTransform;
                Center(brush, point, new Vector2(28f, world.WorldId == WorldId.BambooWorkshop ? 9f : 7f) * scale);
                brush.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
            }
            if (current >= 0)
            {
                Vector2 point = panel.InverseTransformPoint(path.TransformPoint(positions[current]));
                Center(marker.rectTransform, point + Vector2.up * 70f * scale, Vector2.one * 92f * scale);
            }
        }

        private Vector2 Curve(int index, float t)
        {
            Vector2 a = positions[Mathf.Max(index - 1, 0)];
            Vector2 b = positions[index];
            Vector2 c = positions[index + 1];
            Vector2 d = positions[Mathf.Min(index + 2, stops.Count - 1)];
            return .5f * ((2f * b) + (c - a) * t + (2f * a - 5f * b + 4f * c - d) * t * t + (-a + 3f * b - 3f * c + d) * t * t * t);
        }

        private static Image Image(string name, RectTransform parent, Sprite sprite = null)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Text(string name, RectTransform parent, string value)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            TMP_Text text = item.GetComponent<TMP_Text>();
            text.text = value;
            UiTypography.Apply(text, name == "Number" ? UiFontRole.Emphasis :
                name == "LevelJourneySubtitle" ? UiFontRole.Label : UiFontRole.Body);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Layer(string name, RectTransform parent)
        {
            GameObject item = new(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)item.transform;
            Stretch(rect);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
        private static Color Ui(Color color) => QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        private void HandleNodeFocusChanged(LevelSelectNodeFeedback feedback)
        {
            if (world == null || bestPreview == null) return;
            if (feedback.IsFocused) focusedIndex = stops.FindIndex(stop => stop.Feedback == feedback);
            else if (focusedIndex < 0 || !stops[focusedIndex].Feedback.IsFocused)
                focusedIndex = stops.FindIndex(stop => stop.Feedback.IsFocused);
            bestPreview.gameObject.SetActive(focusedIndex >= 0);
            if (focusedIndex < 0) return;
            int? best = BestMovesProgress.GetBest(world.WorldId, focusedIndex + 1);
            bestPreview.text = $"LEVEL {focusedIndex + 1:00}  ·  BEST {(best.HasValue ? best.Value.ToString() : "--")}";
        }

        private void OnDestroy()
        {
            foreach (Stop stop in stops)
                if (stop.Feedback != null) stop.Feedback.FocusChanged -= HandleNodeFocusChanged;
            paint?.Release();
        }
    }
}
