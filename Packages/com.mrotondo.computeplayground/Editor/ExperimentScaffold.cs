using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mrotondo.ComputePlayground.Editor
{
    /// <summary>
    /// Scaffolds an experiment in two halves, because the generated type does not exist until
    /// the editor has recompiled. <see cref="WriteFiles"/> runs before the domain reload and
    /// <see cref="BuildScene"/> after it.
    /// <para>
    /// The scene is constructed in code rather than shipped as a template asset, so there is no
    /// serialized scene to rot across editor versions.
    /// </para>
    /// </summary>
    public static class ExperimentScaffold
    {
        internal const string PendingKey = "ComputePlayground.PendingExperiment";

        const string TemplateRoot = "Packages/com.mrotondo.computeplayground/Editor/Templates/";
        const string ExperimentRoot = "Assets/Experiments";

        public static bool IsValidName(string name) =>
            !string.IsNullOrEmpty(name) &&
            (char.IsLetter(name[0]) || name[0] == '_') &&
            name.All(character => char.IsLetterOrDigit(character) || character == '_');

        public static string FolderFor(string name) => $"{ExperimentRoot}/{name}";

        /// <summary>
        /// Writes the script and compute shader, then queues the scene half for after the
        /// recompile that AssetDatabase.Refresh triggers.
        /// </summary>
        public static void WriteFiles(string name)
        {
            if (!IsValidName(name))
                throw new ArgumentException($"'{name}' is not a valid C# type name.", nameof(name));

            string folder = FolderFor(name);
            Directory.CreateDirectory(folder);

            File.WriteAllText($"{folder}/{name}.cs", Template("Experiment.cs.txt", name));
            File.WriteAllText($"{folder}/{name}.compute", Template("Experiment.compute.txt", name));

            SessionState.SetString(PendingKey, name);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Creates and saves a scene holding a camera and the generated experiment component,
        /// with the generated compute shader assigned. Requires the script to have compiled.
        /// </summary>
        public static void BuildScene(string name)
        {
            Type type = TypeCache.GetTypesDerivedFrom<ComputeExperiment>()
                .FirstOrDefault(candidate => candidate.Name == name);

            if (type == null)
            {
                Debug.LogError($"Minimal Project: {name} did not compile; scene not created.");
                return;
            }

            string folder = FolderFor(name);
            string scenePath = $"{folder}/{name}.unity";

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("Camera", typeof(Camera)).tag = "MainCamera";

            var experimentObject = new GameObject(name);
            var experiment = (ComputeExperiment)experimentObject.AddComponent(type);

            var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>($"{folder}/{name}.compute");
            if (computeShader != null)
            {
                var serialized = new SerializedObject(experiment);
                serialized.FindProperty("compute").objectReferenceValue = computeShader;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // OnEnable already ran, before the shader existed on the component.
                experiment.Rebuild();
            }
            else
            {
                Debug.LogWarning($"Minimal Project: {folder}/{name}.compute not found; assign it by hand.");
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            Selection.activeObject = experimentObject;
            Debug.Log($"Minimal Project: created {scenePath}.");
        }

        static string Template(string fileName, string experimentName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(TemplateRoot + fileName);
            if (asset == null)
                throw new FileNotFoundException($"Missing template {TemplateRoot + fileName}");

            return asset.text.Replace("#NAME#", experimentName);
        }
    }

    /// <summary>
    /// Runs the scene half of the scaffold once the generated script has compiled.
    /// </summary>
    [InitializeOnLoad]
    static class ExperimentScaffoldCompletion
    {
        static ExperimentScaffoldCompletion()
        {
            string name = SessionState.GetString(ExperimentScaffold.PendingKey, string.Empty);
            if (string.IsNullOrEmpty(name))
                return;

            SessionState.EraseString(ExperimentScaffold.PendingKey);

            // Scene work is not safe during static initialisation.
            EditorApplication.delayCall += () => ExperimentScaffold.BuildScene(name);
        }
    }
}
