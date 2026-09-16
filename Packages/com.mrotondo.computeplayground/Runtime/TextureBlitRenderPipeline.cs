using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mrotondo.ComputePlayground
{
    /// <summary>
    /// The entire render pipeline: clear, then draw one texture to the camera target.
    /// No culling, no lighting, no shadows, no depth prepass, no post.
    /// </summary>
    public class TextureBlitRenderPipeline : RenderPipeline
    {
        static readonly int SourceTexId = Shader.PropertyToID("_SourceTex");
        static readonly int BlitScaleId = Shader.PropertyToID("_BlitScale");
        static readonly int FlipYId = Shader.PropertyToID("_FlipY");

        /// <summary>
        /// The texture drawn to the screen. Set by <see cref="ComputeExperiment"/>; assign it
        /// yourself if you are not using that base class. A static is deliberate here -- these
        /// projects have exactly one thing on screen, and this avoids wiring a scene reference
        /// into the pipeline asset.
        /// </summary>
        public static RenderTexture Source;

        readonly Material _material;
        readonly Color _clearColor;
        readonly bool _fitAspect;
        readonly bool _flipY;

        public TextureBlitRenderPipeline(Shader blitShader, Color clearColor, bool fitAspect, bool flipY)
        {
            _clearColor = clearColor;
            _fitAspect = fitAspect;
            _flipY = flipY;

            if (blitShader != null)
                _material = new Material(blitShader) { hideFlags = HideFlags.HideAndDontSave };
            else
                Debug.LogError(
                    "TextureBlitRenderPipelineAsset has no blit shader assigned. " +
                    "Run Tools > Minimal Project > Apply Settings to repair it.");
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (_material == null)
                return;

            var cmd = new CommandBuffer { name = "Texture Blit" };

            foreach (Camera camera in cameras)
            {
                context.SetupCameraProperties(camera);

                cmd.Clear();
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                cmd.ClearRenderTarget(true, true, _clearColor);

                if (Source != null)
                {
                    cmd.SetGlobalTexture(SourceTexId, Source);
                    cmd.SetGlobalVector(BlitScaleId, FitScale(camera));
                    cmd.SetGlobalFloat(FlipYId, _flipY ? 1f : 0f);

                    // A single oversized triangle, scaled down to the letterboxed rect.
                    // DrawProcedural rather than CommandBuffer.Blit: Blit is legacy API that
                    // mutates render state behind your back and is not SRP-safe.
                    cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
                }

                context.ExecuteCommandBuffer(cmd);
            }

            context.Submit();
            cmd.Release();
        }

        /// <summary>
        /// Clip-space scale that fits the source inside the viewport without distorting it.
        /// </summary>
        Vector4 FitScale(Camera camera)
        {
            if (!_fitAspect || Source == null || Source.height == 0 || camera.pixelHeight == 0)
                return new Vector4(1f, 1f, 0f, 0f);

            float sourceAspect = (float)Source.width / Source.height;
            float viewAspect = (float)camera.pixelWidth / camera.pixelHeight;

            return sourceAspect > viewAspect
                ? new Vector4(1f, viewAspect / sourceAspect, 0f, 0f)   // letterbox: bars top/bottom
                : new Vector4(sourceAspect / viewAspect, 1f, 0f, 0f);  // pillarbox: bars left/right
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_material == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(_material);
            else
                Object.DestroyImmediate(_material);
        }
    }
}
