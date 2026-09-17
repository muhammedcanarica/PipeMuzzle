using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PipeMuzzle.Editor
{
    public static class PipeSpriteAlignmentValidator
    {
        private const float ExpectedPixelsPerUnit = 410f;
        private const int ExpectedTextureSize = 627;

        private static readonly string[] SpritePaths =
        {
            "Assets/Art/Tiles/SoftBlossom/pipe_straight.png",
            "Assets/Art/Tiles/SoftBlossom/pipe_corner.png",
            "Assets/Art/Tiles/SoftBlossom/pipe_threeway.png",
            "Assets/Art/Tiles/SoftBlossom/pipe_cross.png"
        };

        [MenuItem("PipeMuzzle/Validate Pipe Sprite Alignment")]
        public static void Validate()
        {
            List<string> errors = new List<string>();

            foreach (string path in SpritePaths)
            {
                ValidateSprite(path, errors);
            }

            if (errors.Count == 0)
            {
                Debug.Log(
                    "Pipe sprite alignment validation passed: " +
                    "dimensions, centered pivots, PPU and Full Rect match."
                );
                return;
            }

            Debug.LogError(
                "Pipe sprite alignment validation failed:\n- " +
                string.Join("\n- ", errors)
            );
        }

        private static void ValidateSprite(
            string path,
            List<string> errors)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (importer == null || sprite == null)
            {
                errors.Add(path + ": sprite or TextureImporter is missing.");
                return;
            }

            if (sprite.rect.width != ExpectedTextureSize ||
                sprite.rect.height != ExpectedTextureSize)
            {
                errors.Add(
                    path + ": expected a 627 x 627 sprite rect, got " +
                    sprite.rect.width + " x " + sprite.rect.height + "."
                );
            }

            Vector2 expectedPivot = sprite.rect.size * 0.5f;
            if (Vector2.Distance(sprite.pivot, expectedPivot) > 0.01f)
            {
                errors.Add(
                    path + ": pivot is " + sprite.pivot +
                    ", expected centered " + expectedPivot + "."
                );
            }

            if (!Mathf.Approximately(
                    sprite.pixelsPerUnit,
                    ExpectedPixelsPerUnit))
            {
                errors.Add(
                    path + ": PPU is " + sprite.pixelsPerUnit +
                    ", expected " + ExpectedPixelsPerUnit + "."
                );
            }

            TextureImporterSettings settings =
                new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                errors.Add(path + ": mesh type must be Full Rect.");
            }
        }
    }
}
