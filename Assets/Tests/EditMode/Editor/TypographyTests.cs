using NUnit.Framework;
using TMPro;
using PipeMuzzle.UI;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace PipeMuzzle.Tests.EditMode
{
    public sealed class TypographyTests
    {
        [Test]
        public void InactiveHudButtonUsesSemiBoldBeforeItsScreenOpens()
        {
            var root = new GameObject("HiddenButton", typeof(RectTransform), typeof(UnityEngine.UI.Button));
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(root.transform, false);
            root.SetActive(false);
            try
            {
                TMP_Text text = label.GetComponent<TMP_Text>();
                typeof(GameUI).GetMethod("StyleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .Invoke(null, new object[] { text, 24f, Color.white, FontStyles.Normal });
                Assert.That(text.font.name, Is.EqualTo("SourGummy-SemiBold"));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ShippingSceneUsesBundledFontsAndMatchingAtlasMaterials()
        {
            string serialized = File.ReadAllText("Assets/Scenes/Gameplay.unity");
            int count = 0;
            foreach (string component in Regex.Split(serialized, "(?m)(?=^--- !u!)"))
            {
                Match reference = Regex.Match(component, @"m_fontAsset: \{fileID: 11400000, guid: ([a-f0-9]+), type: 2\}");
                if (!reference.Success) continue;
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    AssetDatabase.GUIDToAssetPath(reference.Groups[1].Value));
                Assert.That(font, Is.Not.Null);
                Assert.That(font.faceInfo.familyName, Is.EqualTo("Sour Gummy").Or.EqualTo("Grandstander"));
                Match materialReference = Regex.Match(component, @"m_sharedMaterial: \{fileID: (-?\d+), guid: ([a-f0-9]+), type: 2\}");
                Assert.That(materialReference.Success, Is.True);
                Material material = AssetDatabase.LoadAllAssetsAtPath(
                    AssetDatabase.GUIDToAssetPath(materialReference.Groups[2].Value)).OfType<Material>()
                    .Single(candidate => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out string _, out long id)
                        && id == long.Parse(materialReference.Groups[1].Value));
                Assert.That(material.GetTexture("_MainTex"), Is.EqualTo(font.atlasTexture));
                Assert.That(component, Does.Contain("m_fontStyle: 0"));
                count++;
            }
            Assert.That(count, Is.GreaterThan(0));
        }

        [Test]
        public void ApplyingRealWeightPreservesLayoutAndClearsSyntheticBold()
        {
            var root = new GameObject("TypographyProbe", typeof(RectTransform), typeof(TextMeshProUGUI));
            try
            {
                TMP_Text text = root.GetComponent<TMP_Text>();
                text.fontStyle = FontStyles.Bold;
                text.fontSize = 23;
                text.characterSpacing = 1.6f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 17;
                text.fontSizeMax = 27;
                text.alignment = TextAlignmentOptions.Left;
                Vector2 size = text.rectTransform.sizeDelta;
                UiTypography.Apply(text, UiFontRole.Emphasis);
                Assert.That(text.font.name, Is.EqualTo("SourGummy-SemiBold"));
                Assert.That(text.fontStyle.HasFlag(FontStyles.Bold), Is.False);
                Assert.That(text.fontSize, Is.EqualTo(23));
                Assert.That(text.characterSpacing, Is.EqualTo(1.6f));
                Assert.That(text.enableAutoSizing, Is.True);
                Assert.That(text.fontSizeMin, Is.EqualTo(17));
                Assert.That(text.fontSizeMax, Is.EqualTo(27));
                Assert.That(text.alignment, Is.EqualTo(TextAlignmentOptions.Left));
                Assert.That(text.rectTransform.sizeDelta, Is.EqualTo(size));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("SourGummy-Regular", "Sour Gummy")]
        [TestCase("SourGummy-Medium", "Sour Gummy")]
        [TestCase("SourGummy-SemiBold", "Sour Gummy")]
        [TestCase("Grandstander-SemiBold", "Grandstander")]
        [TestCase("Grandstander-Bold", "Grandstander")]
        public void BundledFontsContainGameTextWithoutFallback(string assetName, string family)
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/" + assetName);
            Assert.That(font, Is.Not.Null, "Missing bundled TMP font: " + assetName);
            Assert.That(font.faceInfo.familyName, Is.EqualTo(family));
            Assert.That(font.sourceFontFile, Is.Not.Null, "Dynamic atlas needs its local TTF in builds.");
            Assert.That(font.HasCharacters("Ruilay Sakura Garden Bamboo Workshop Moon Shrine " +
                "JOURNEY COMPLETE 12 LEVELS LOCKED SETTINGS HINT 3/3 ‹ MAP · " +
                "Muhammed Can Arıca ÇçĞğİıÖöŞşÜü", out uint[] missing, false, true),
                Is.True, "Missing glyphs: " + (missing == null ? "" : string.Join(",", missing)));
        }
    }
}
