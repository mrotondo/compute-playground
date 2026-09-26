using UnityEditor;

namespace Mrotondo.ComputePlayground.Editor
{
    /// <summary>
    /// Keeps experiments stepping in edit mode.
    /// <para>
    /// [ExecuteAlways] gets Update called outside Play mode, but only when something pumps the
    /// player loop -- and in edit mode the only things that do are incidental editor repaints,
    /// such as a toolbar button highlighting under the cursor. The visible symptom is a
    /// simulation that advances only while the mouse moves over particular parts of the editor.
    /// </para>
    /// <para>
    /// EditorApplication.update fires regardless of repaints, so pumping the loop from here
    /// decouples stepping from mouse movement. Gated on an experiment actually wanting to run,
    /// so an idle project is not driving the player loop for nothing.
    /// </para>
    /// </summary>
    [InitializeOnLoad]
    static class EditModeTicker
    {
        static EditModeTicker() => EditorApplication.update += Tick;

        static void Tick()
        {
            // Play mode has a real game loop; leave it alone.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!ComputeExperiment.AnyWantsContinuousUpdate)
                return;

            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
