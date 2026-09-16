using UnityEngine;
using UnityEngine.Rendering;

namespace Mrotondo.ComputePlayground
{
    [CreateAssetMenu(
        menuName = "Rendering/Texture Blit Render Pipeline Asset",
        fileName = "TextureBlitRenderPipelineAsset")]
    public class TextureBlitRenderPipelineAsset : RenderPipelineAsset
    {
        [Tooltip("Hidden/ComputePlayground/TextureBlit, unless you have replaced it.")]
        [SerializeField] Shader blitShader;

        [Tooltip("Colour of the letterbox bars and of the screen when there is no source texture.")]
        [SerializeField] Color clearColor = Color.black;

        [Tooltip("Preserve the source texture's aspect ratio instead of stretching it to fill.")]
        [SerializeField] bool fitAspect = true;

        [Tooltip("Flip vertically. If your simulation renders upside down, toggle this.")]
        [SerializeField] bool flipY;

        public Shader BlitShader
        {
            get => blitShader;
            set => blitShader = value;
        }

        protected override RenderPipeline CreatePipeline() =>
            new TextureBlitRenderPipeline(blitShader, clearColor, fitAspect, flipY);
    }
}
