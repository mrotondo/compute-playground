using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mrotondo.ComputePlayground.Editor
{
    /// <summary>
    /// Front end for <see cref="ExperimentScaffold"/>.
    /// </summary>
    public class NewExperimentWindow : EditorWindow
    {
        string _name = "MyExperiment";

        [MenuItem("Tools/Minimal Project/New Experiment...", priority = 20)]
        public static void Open()
        {
            var window = GetWindow<NewExperimentWindow>(utility: true, title: "New Experiment");
            window.minSize = new Vector2(380f, 140f);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Creates a script, a compute shader, and a scene wired to both.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();

            _name = EditorGUILayout.TextField("Name", _name);

            bool valid = ExperimentScaffold.IsValidName(_name);
            bool exists = valid && Directory.Exists(ExperimentScaffold.FolderFor(_name));

            if (!valid)
                EditorGUILayout.HelpBox("Must be a valid C# type name.", MessageType.Warning);
            else if (exists)
                EditorGUILayout.HelpBox($"{ExperimentScaffold.FolderFor(_name)} already exists.", MessageType.Warning);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!valid || exists))
            {
                if (GUILayout.Button("Create"))
                {
                    ExperimentScaffold.WriteFiles(_name);
                    Close();
                }
            }
        }
    }
}
