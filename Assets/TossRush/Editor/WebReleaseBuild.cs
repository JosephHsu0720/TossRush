using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TossRush.Editor
{
    public static class WebReleaseBuild
    {
        const string ScenePath = "Assets/TossRush/Scenes/TossRush.unity";

        [MenuItem("TossRush/Release/Apply Web Settings")]
        public static void ApplySettings()
        {
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.template = "PROJECT:TossRush";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.stripEngineCode = true;
            // Keep conservative managed stripping for the first browser release.
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
            AssetDatabase.SaveAssets();
            Debug.Log("TossRush Web: 1280x720, Gzip + fallback, 128 MB initial memory, Minimal stripping.");
        }

        [MenuItem("TossRush/Release/Build itch.io ZIP")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new BuildFailedException("Exit Play Mode and wait for compilation before building.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new BuildFailedException("Install the Web build support module for this Editor.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new BuildFailedException("Save or resolve unsaved scene changes before building.");
            ApplySettings();
            string id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string output = "Builds/WebGL/" + id;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, target = BuildTarget.WebGL,
                locationPathName = output, options = BuildOptions.DetailedBuildReport
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Web build failed: " + report.summary.result);

            CopyNotices(output);
            Directory.CreateDirectory("Artifacts/Release");
            string archive = "Artifacts/Release/TossRush-Web-" + id + ".zip";
            ZipFile.CreateFromDirectory(output, archive, System.IO.Compression.CompressionLevel.Optimal, false);
            using (var zip = ZipFile.OpenRead(archive))
            {
                if (!zip.Entries.Any(e => e.FullName == "index.html") || zip.Entries.Count > 1000
                    || zip.Entries.Sum(e => e.Length) > 500L * 1024 * 1024
                    || zip.Entries.Any(e => e.Length > 200L * 1024 * 1024 || e.FullName.Length > 240))
                    throw new BuildFailedException("Archive does not meet itch.io HTML5 limits.");
            }
            Directory.CreateDirectory("Docs/Release");
            File.WriteAllText("Docs/Release/LastBuild.json", JsonUtility.ToJson(new BuildRecord {
                utc = DateTime.UtcNow.ToString("o"), unity = Application.unityVersion,
                output = output, archive = archive, zipBytes = new FileInfo(archive).Length,
                buildBytes = report.summary.totalSize, warnings = report.summary.totalWarnings,
                errors = report.summary.totalErrors, seconds = report.summary.totalTime.TotalSeconds
            }, true));
            Debug.Log("itch.io ZIP ready: " + Path.GetFullPath(archive));
        }

        static void CopyNotices(string output)
        {
            string[] sources = {
                "LICENSE", "THIRD_PARTY_NOTICES.md", "Docs/AssetSources.md",
                "Docs/Licenses/CC0-1.0.txt", "Docs/Licenses/Unity-uGUI-LICENSE.md",
                "Assets/TossRush/ThirdParty/Kenney/furniture-kit/License.txt",
                "Assets/TossRush/ThirdParty/Kenney/ui-pack/License.txt",
                "Assets/TossRush/ThirdParty/Kenney/interface-sounds/License.txt",
                "Assets/TossRush/ThirdParty/SubspaceAudio/INFO.txt",
                "Assets/TossRush/ThirdParty/WovenSFX/License.txt"
            };
            foreach (string source in sources)
            {
                string destination = Path.Combine(output, source);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination, true);
            }
            // Preserve package-supplied notices as well as the project's asset credits.
            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (!Directory.Exists(package.resolvedPath)) continue;
                foreach (string source in Directory.GetFiles(package.resolvedPath))
                {
                    string name = Path.GetFileName(source);
                    if (name.IndexOf("license", StringComparison.OrdinalIgnoreCase) < 0
                        && name.IndexOf("notice", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string destination = Path.Combine(output, "Licenses", "Packages", package.name, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(source, destination, true);
                }
            }
        }

        [Serializable] sealed class BuildRecord
        {
            public string utc, unity, output, archive;
            public long zipBytes;
            public ulong buildBytes;
            public int warnings, errors;
            public double seconds;
        }
    }

    public sealed class JamSceneBuildCheck : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.WebGL) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var game in root.GetComponentsInChildren<GameDirector>(true))
                    if (game.roundDuration <= 0 || game.roundDuration > 20 || game.startingStageIndex != 0 || game.repeatStartingStage)
                        throw new BuildFailedException("Jam release requires 0 < duration <= 20, starting stage 0 and normal rotation.");
        }
    }
}
