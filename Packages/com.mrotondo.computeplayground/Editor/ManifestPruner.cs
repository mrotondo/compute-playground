using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Mrotondo.ComputePlayground.Editor
{
    /// <summary>
    /// Rewrites Packages/manifest.json down to an allowlist.
    /// <para>
    /// Writing the file directly and calling Client.Resolve is much simpler than a chain of
    /// async Client.Remove calls, each of which would trigger its own domain reload. Unity
    /// treats the manifest as a normal input file, so editing it is supported.
    /// </para>
    /// </summary>
    public static class ManifestPruner
    {
        const string ManifestPath = "Packages/manifest.json";
        const string OwnPackage = "com.mrotondo.computeplayground";

        /// <summary>
        /// Everything a compute experiment needs and nothing else. Built-in modules not listed
        /// here are simply absent from the player and from editor compilation.
        /// </summary>
        static readonly Dictionary<string, string> Keep = new Dictionary<string, string>
        {
            // Generates the .csproj files IDEs read. Swap for com.unity.ide.rider if you prefer.
            { "com.unity.ide.visualstudio", "2.0.23" },

            // Texture2D.EncodeToPNG, for saving frames out of an experiment.
            { "com.unity.modules.imageconversion", "1.0.0" },
            { "com.unity.modules.screencapture", "1.0.0" },

            // The editor's own UI is built on these; removing them breaks inspector windows.
            { "com.unity.modules.imgui", "1.0.0" },
            { "com.unity.modules.uielements", "1.0.0" },
            { "com.unity.modules.ui", "1.0.0" },
            { "com.unity.modules.jsonserialize", "1.0.0" },
        };

        [MenuItem("Tools/Minimal Project/Prune Packages", priority = 1)]
        public static void Prune()
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"Minimal Project: no manifest at {ManifestPath}.");
                return;
            }

            Dictionary<string, string> existing = ReadDependencies(File.ReadAllText(ManifestPath));

            var kept = new SortedDictionary<string, string>();

            // Keep this package at whatever version or git URL it is already pinned to.
            if (existing.TryGetValue(OwnPackage, out string own))
                kept[OwnPackage] = own;

            foreach (KeyValuePair<string, string> entry in Keep)
                kept[entry.Key] = existing.TryGetValue(entry.Key, out string version) ? version : entry.Value;

            string[] removed = existing.Keys.Where(key => !kept.ContainsKey(key)).OrderBy(key => key).ToArray();

            if (removed.Length == 0)
            {
                Debug.Log("Minimal Project: manifest is already pruned.");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog(
                "Prune Packages",
                $"Remove {removed.Length} package(s)?\n\n{string.Join("\n", removed)}",
                "Prune", "Cancel");

            if (!proceed)
                return;

            File.WriteAllText(ManifestPath, BuildManifest(kept));
            Client.Resolve();

            Debug.Log($"Minimal Project: removed {removed.Length} package(s):\n{string.Join("\n", removed)}");
        }

        /// <summary>
        /// Flat regex parse rather than a JSON library. The dependencies block is machine-written
        /// and one level deep, so this holds; it will not survive hand-formatted exotica.
        /// </summary>
        static Dictionary<string, string> ReadDependencies(string json)
        {
            var dependencies = new Dictionary<string, string>();

            Match block = Regex.Match(json, "\"dependencies\"\\s*:\\s*\\{(.*?)\\n\\s*\\}", RegexOptions.Singleline);
            if (!block.Success)
                return dependencies;

            foreach (Match entry in Regex.Matches(block.Groups[1].Value, "\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\""))
                dependencies[entry.Groups[1].Value] = entry.Groups[2].Value;

            return dependencies;
        }

        static string BuildManifest(SortedDictionary<string, string> dependencies)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"dependencies\": {");

            int index = 0;
            foreach (KeyValuePair<string, string> entry in dependencies)
            {
                string comma = ++index < dependencies.Count ? "," : string.Empty;
                builder.AppendLine($"    \"{entry.Key}\": \"{entry.Value}\"{comma}");
            }

            builder.AppendLine("  }");
            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}
