using System.Reflection;
using NUnit.Framework;
using PipeMuzzle.Data;
using PipeMuzzle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class ComicViewerPresentationTests
    {
        private GameObject root;
        private ComicViewerUI viewer;
        private static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ComicPresentation", typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920, 1080);
            viewer = root.AddComponent<ComicViewerUI>();
            typeof(ComicViewerUI).GetMethod("EnsureContent", Private).Invoke(viewer, null);
            typeof(ComicViewerUI).GetMethod("BindButtons", Private).Invoke(viewer, null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [TestCase("Sakura")]
        [TestCase("Bamboo")]
        [TestCase("Moon")]
        public void CounterTracksStoryLengthAndArtworkIsUnfiltered(string world)
        {
            ComicStoryDefinition story = Story(world + "_Intro");
            viewer.Play(story);
            ComicBackdropGraphic backdrop = root.transform.Find("ComicAtmosphere").GetComponent<ComicBackdropGraphic>();
            Assert.That(backdrop.GetComponent<CanvasRenderer>(), Is.Not.Null, "The atmosphere needs a CanvasRenderer to render its gradient.");
            Assert.That(backdrop.raycastTarget, Is.False);
            using (VertexHelper helper = new())
            {
                typeof(ComicBackdropGraphic).GetMethod("OnPopulateMesh", Private).Invoke(backdrop, new object[] { helper });
                UIVertex corner = default;
                UIVertex center = default;
                helper.PopulateUIVertex(ref corner, 0);
                helper.PopulateUIVertex(ref center, helper.currentVertCount / 2);
                Color edgeColor = corner.color;
                Color centerColor = center.color;
                Assert.That(Mathf.Max(edgeColor.r, edgeColor.g, edgeColor.b), Is.GreaterThan(.06f), "The edge tint should remain visible instead of turning near-black.");
                Assert.That(Mathf.Max(centerColor.r, centerColor.g, centerColor.b), Is.GreaterThan(Mathf.Max(edgeColor.r, edgeColor.g, edgeColor.b)));
            }
            TMP_Text counter = root.transform.Find("PanelCounter")?.GetComponent<TMP_Text>();
            Assert.That(counter, Is.Not.Null);
            Assert.That(counter.text, Is.EqualTo("1 / " + story.PanelCount));
            Assert.That(root.transform.Find("ContinueLabel").GetComponent<TMP_Text>().text, Is.EqualTo("NEXT  ›"));
            Image image = root.transform.Find("ComicFrame/PanelImage").GetComponent<Image>();
            Assert.That(image.sprite, Is.SameAs(story.Panels[0]));
            Assert.That(image.color, Is.EqualTo(Color.white));
            Assert.That(image.preserveAspect, Is.True);
            Assert.That(image.type, Is.EqualTo(Image.Type.Simple));
            typeof(ComicViewerUI).GetField("currentPanelIndex", Private).SetValue(viewer, story.PanelCount - 1);
            typeof(ComicViewerUI).GetMethod("ShowPanel", Private).Invoke(viewer, new object[] { story.Panels[story.PanelCount - 1] });
            Assert.That(counter.text, Is.EqualTo(story.PanelCount + " / " + story.PanelCount));
            Assert.That(root.transform.Find("ContinueLabel").GetComponent<TMP_Text>().text, Is.EqualTo("CONTINUE  ›"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SinglePanelContinueAndSkipCompleteExactlyOnce(bool skip)
        {
            ComicStoryDefinition single = ScriptableObject.CreateInstance<ComicStoryDefinition>();
            try
            {
                SerializedObject data = new(single);
                var panels = data.FindProperty("panels");
                panels.arraySize = 1;
                panels.GetArrayElementAtIndex(0).objectReferenceValue = Story("Sakura_After03").Panels[0];
                data.ApplyModifiedPropertiesWithoutUndo();
                viewer.Play(single);
                Assert.That(root.transform.Find("PanelCounter")?.GetComponent<TMP_Text>().text, Is.EqualTo("1 / 1"));
                Assert.That(root.transform.Find("ContinueLabel").GetComponent<TMP_Text>().text, Is.EqualTo("CONTINUE  ›"));
                int completed = 0;
                viewer.StoryCompleted += () => completed++;
                root.transform.Find(skip ? "SkipButton" : "AdvanceButton").GetComponent<Button>().onClick.Invoke();
                viewer.Advance();
                viewer.Skip();
                Assert.That(completed, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(single); }
        }

        [TestCase(1920, 1080)]
        [TestCase(2560, 1080)]
        [TestCase(1440, 1080)]
        [TestCase(800, 600)]
        [TestCase(600, 1000)]
        public void ArtworkAndControlsHaveSeparateVisibleAreas(int width, int height)
        {
            RectTransform panel = (RectTransform)root.transform;
            panel.sizeDelta = new Vector2(width, height);
            viewer.Play(Story("Moon_Intro"));
            Rect frame = Bounds((RectTransform)root.transform.Find("ComicFrame"));
            Rect back = Bounds((RectTransform)root.transform.Find("BackButton"));
            Rect skip = Bounds((RectTransform)root.transform.Find("SkipButton"));
            Rect next = Bounds((RectTransform)root.transform.Find("ContinueLabel"));
            Rect counter = Bounds((RectTransform)root.transform.Find("PanelCounter"));
            Assert.That(frame.Overlaps(back) || frame.Overlaps(skip) || frame.Overlaps(next) || frame.Overlaps(counter), Is.False);
            foreach (Rect rect in new[] { frame, back, skip, next, counter })
                Assert.That(panel.rect.Contains(rect.min) && panel.rect.Contains(rect.max), Is.True);
            Transform advance = root.transform.Find("AdvanceButton");
            Assert.That(advance.GetComponent<Button>().interactable, Is.True);
            Assert.That(back.Overlaps(skip), Is.False);
            Assert.That(advance.GetSiblingIndex(), Is.LessThan(root.transform.Find("BackButton").GetSiblingIndex()));
            Assert.That(advance.GetSiblingIndex(), Is.LessThan(root.transform.Find("SkipButton").GetSiblingIndex()));
            Assert.That(root.transform.Find("PanelCounter").GetComponent<TMP_Text>().raycastTarget, Is.False);
        }

        [Test]
        public void BackIsSeparateFromAdvanceAndCompletion()
        {
            viewer.Play(Story("Moon_Final"));
            int completed = 0;
            int backed = 0;
            viewer.StoryCompleted += () => completed++;
            viewer.BackRequested += () => backed++;
            root.transform.Find("BackButton").GetComponent<Button>().onClick.Invoke();
            viewer.Advance();
            Assert.That(backed, Is.EqualTo(1));
            Assert.That(completed, Is.Zero);
        }

        private Rect Bounds(RectTransform transform)
        {
            Vector3[] corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            Vector2 min = root.transform.InverseTransformPoint(corners[0]);
            Vector2 max = root.transform.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static ComicStoryDefinition Story(string name) =>
            AssetDatabase.LoadAssetAtPath<ComicStoryDefinition>("Assets/Data/Stories/" + name + ".asset");
    }
}
