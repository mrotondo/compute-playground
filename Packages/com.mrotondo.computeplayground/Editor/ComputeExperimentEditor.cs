using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Mrotondo.ComputePlayground.Editor
{
    /// <summary>
    /// Default inspector plus a button for every [ExperimentButton] method.
    /// Applies to every ComputeExperiment subclass.
    /// </summary>
    [CustomEditor(typeof(ComputeExperiment), editorForChildClasses: true)]
    public class ComputeExperimentEditor : UnityEditor.Editor
    {
        const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var experiment = (ComputeExperiment)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Steps run", experiment.StepCount.ToString());
            EditorGUILayout.Space();

            foreach (MethodInfo method in target.GetType().GetMethods(Flags))
            {
                var attribute = method.GetCustomAttribute<ExperimentButtonAttribute>();
                if (attribute == null || method.GetParameters().Length > 0)
                    continue;

                string label = attribute.Label ?? ObjectNames.NicifyVariableName(method.Name);
                if (!GUILayout.Button(label))
                    continue;

                foreach (Object each in targets)
                    method.Invoke(each, null);
            }
        }
    }
}
