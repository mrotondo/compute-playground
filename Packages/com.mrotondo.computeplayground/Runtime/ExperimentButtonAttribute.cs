using System;

namespace Mrotondo.ComputePlayground
{
    /// <summary>
    /// Draws a button in the inspector that calls this parameterless method.
    /// Exists so the template needs no third-party inspector package.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ExperimentButtonAttribute : Attribute
    {
        public string Label { get; }

        public ExperimentButtonAttribute(string label = null) => Label = label;
    }
}
