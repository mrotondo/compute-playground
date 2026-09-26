using System;
using System.Collections.Generic;
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
        readonly struct Button
        {
            public readonly MethodInfo Method;
            public readonly string Label;

            public Button(MethodInfo method, string label)
            {
                Method = method;
                Label = label;
            }
        }

        /// <summary>
        /// OnInspectorGUI runs on every IMGUI event, which includes every mouse move over the
        /// inspector, and at least twice per repaint (Layout and Repaint). Reflecting over the
        /// target's methods in there cost hundreds of allocating calls per event -- enough GC
        /// churn on the main thread to drop the editor from ~500 to ~100 FPS while the mouse
        /// moved. Resolve once per type instead.
        /// </summary>
        static readonly Dictionary<Type, Button[]> Cache = new Dictionary<Type, Button[]>();

        Button[] _buttons;

        void OnEnable() => _buttons = ButtonsFor(target.GetType());

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Steps run", ((ComputeExperiment)target).StepCount.ToString());
            EditorGUILayout.Space();

            foreach (Button button in _buttons ?? ButtonsFor(target.GetType()))
            {
                if (!GUILayout.Button(button.Label))
                    continue;

                foreach (UnityEngine.Object each in targets)
                    button.Method.Invoke(each, null);
            }
        }

        /// <summary>
        /// TypeCache is built once at domain load, so this walks a short prebuilt list rather
        /// than reflecting over every inherited member of MonoBehaviour.
        /// </summary>
        static Button[] ButtonsFor(Type type)
        {
            if (Cache.TryGetValue(type, out Button[] cached))
                return cached;

            var buttons = new List<Button>();

            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<ExperimentButtonAttribute>())
            {
                if (method.IsStatic || method.GetParameters().Length > 0)
                    continue;

                if (method.DeclaringType == null || !method.DeclaringType.IsAssignableFrom(type))
                    continue;

                var attribute = method.GetCustomAttribute<ExperimentButtonAttribute>();
                buttons.Add(new Button(
                    method,
                    attribute.Label ?? ObjectNames.NicifyVariableName(method.Name)));
            }

            Button[] resolved = buttons.ToArray();
            Cache[type] = resolved;
            return resolved;
        }
    }
}
