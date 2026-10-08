using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using PipeMuzzle.Data;
using PipeMuzzle.Feedback;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;
using UnityEngine;

namespace PipeMuzzle.Editor
{
    public static class PipeMuzzleWebBuild
    {
        public const string ProfilePath = "Assets/BuildProfiles/WebGL RC1.asset";
        private const string StartupScene = "Assets/Scenes/Gameplay.unity";

        // CLI: Unity -batchmode -quit -projectPath <project> -activeBuildProfile
        // "Assets/BuildProfiles/WebGL RC1.asset" -executeMethod PipeMuzzle.Editor.PipeMuzzleWebBuild.Build
        [MenuItem("Ruilay/Build Production WebGL")]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL ||
                AssetDatabase.GetAssetPath(BuildProfile.GetActiveBuildProfile()) != ProfilePath)
                throw new BuildFailedException("Activate the WebGL RC1 profile before building (CLI: -activeBuildProfile).");

            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            // This project uses one scene and ScreenManager panels, including story checkpoints.
            if (scenes.Length != 1 || scenes[0] != StartupScene || !File.Exists(StartupScene))
                throw new BuildFailedException("Production scene list must contain only Gameplay.unity as startup scene.");

            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
            EditorUserBuildSettings.allowDebugging = false;
            UserBuildSettings.codeOptimization = WasmCodeOptimization.RuntimeSpeed;
            if (PlayerSettings.productName != "Ruilay" || !PlayerSettings.WebGL.decompressionFallback)
                throw new BuildFailedException("Web profile must use Product Name Ruilay and decompression fallback.");

            int levels = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/Scripts/Data" }))
            {
                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                LevelValidationResult validation = LevelValidationUtility.Analyze(level);
                if (validation.Errors.Count > 0 || !validation.SearchComplete || validation.MinimumMoves == int.MaxValue)
                    throw new BuildFailedException($"Invalid level {level.name}: {string.Join("; ", validation.Errors)}");
                levels++;
            }
            if (levels != 36)
                throw new BuildFailedException($"Expected 36 production levels; found {levels}.");
            MusicTracks music = Resources.Load<MusicTracks>("Audio/MusicTracks");
            if (music == null || music.Main == null || music.ForWorld(WorldId.SakuraGarden) == null ||
                music.ForWorld(WorldId.BambooWorkshop) == null || music.ForWorld(WorldId.MoonShrine) == null)
                throw new BuildFailedException("All four background music tracks are required.");

            Debug.Log($"WEB_RELEASE_CONFIGURATION Unity={Application.unityVersion}; scenes={string.Join(",", scenes)}; " +
                $"levels={levels}; product={PlayerSettings.productName}; development=false; profiler=false; deepProfiling=false; " +
                $"optimization={UserBuildSettings.codeOptimization}; compression={PlayerSettings.WebGL.compressionFormat}; " +
                $"fallback={PlayerSettings.WebGL.decompressionFallback}; caching={PlayerSettings.WebGL.dataCaching}; " +
                $"memory={PlayerSettings.WebGL.initialMemorySize}/{PlayerSettings.WebGL.maximumMemorySize}; " +
                $"growth={PlayerSettings.WebGL.memoryGrowthMode}; template={PlayerSettings.WebGL.template}; " +
                $"graphics={string.Join(",", PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL))}; " +
                $"exceptions={PlayerSettings.WebGL.exceptionSupport}");

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/WebGL/Ruilay",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            File.WriteAllText("Builds/Ruilay-WebGL-release-summary.txt",
                $"Unity: {Application.unityVersion}\nScenes: {string.Join(",", scenes)}\n" +
                $"Options: {report.summary.options}\nResult: {report.summary.result}\n" +
                $"Errors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\n" +
                $"Size: {report.summary.totalSize}\nTime: {report.summary.totalTime}\n");
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0 ||
                (report.summary.options & BuildOptions.Development) != 0 || !File.Exists("Builds/WebGL/Ruilay/index.html"))
                throw new BuildFailedException($"WebGL release build failed: {report.summary.result}");
            Package();
            Debug.Log("WEB_RELEASE_BUILD_SUCCEEDED");
        }

        private static void Package()
        {
            string root = Path.GetFullPath("Builds/WebGL/Ruilay");
            string[] buildFiles = Directory.GetFiles(Path.Combine(root, "Build"));
            if (!buildFiles.Any(path => path.EndsWith(".loader.js")) ||
                !buildFiles.Any(path => path.Contains(".wasm")) ||
                !buildFiles.Any(path => path.Contains(".data")) ||
                !buildFiles.Any(path => path.Contains(".framework.js")))
                throw new BuildFailedException("WebGL output is missing required build files.");

            // Package the player files only; keep previous RC1 output and debug backups out.
            using (FileStream stream = new FileStream("Builds/Ruilay_WebGL.zip", FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(Path.Combine(root, "index.html"), "index.html");
                foreach (string folder in new[] { "Build", "TemplateData", "StreamingAssets" })
                {
                    string directory = Path.Combine(root, folder);
                    if (!Directory.Exists(directory)) continue;
                    foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                    {
                        string entry = file.Substring(root.Length + 1).Replace('\\', '/');
                        archive.CreateEntryFromFile(file, entry, System.IO.Compression.CompressionLevel.Optimal);
                    }
                }
            }
        }
    }
}
