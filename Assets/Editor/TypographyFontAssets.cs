using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PipeMuzzle.Editor
{
    /// <summary>Reproducible local TMP atlas generation; never runs automatically.</summary>
    internal static class TypographyFontAssets
    {
        [MenuItem("Ruilay/Typography/Generate Font Assets")]
        private static void Generate()
        {
            const string output = "Assets/Resources/Fonts";
            Directory.CreateDirectory(output);
            AssetDatabase.Refresh();
            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            string characters = new string(Enumerable.Range(32, 95).Select(i => (char)i).ToArray()) +
                "ÇçĞğİıÖöŞşÜü‹›·…–—‘’“”×";
            foreach (string family in new[] { "SourGummy", "Grandstander" })
            foreach (string weight in family == "SourGummy"
                ? new[] { "Regular", "Medium", "SemiBold" } : new[] { "SemiBold", "Bold" })
            {
                string name = family + "-" + weight;
                string path = output + "/" + name + ".asset";
                // Preserve existing GUIDs and tuned atlases on repeated runs.
                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) continue;
                Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/" + family + "/" + name + ".ttf");
                if (source == null) throw new System.InvalidOperationException("Missing source font: " + name);
                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source, 64, 7,
                    GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                asset.name = name;
                asset.atlasTextures[0].name = name + " Atlas";
                asset.material.name = name + " Material";
                if (!asset.TryAddCharacters(characters, out string missing))
                    throw new System.InvalidOperationException(name + " missing characters: " + missing);
                if (fallback != null) asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.AddObjectToAsset(asset.material, asset);
                foreach (Texture2D texture in asset.atlasTextures) AssetDatabase.AddObjectToAsset(texture, asset);
                EditorUtility.SetDirty(asset);
            }
            TMP_Settings.defaultFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(output + "/SourGummy-Regular.asset");
            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
            Debug.Log("Typography: five bundled TMP fonts generated; Sour Gummy is the default UI font.");
        }
    }
}
