using UnityEngine;

namespace Mrotondo.ComputePlayground
{
    /// <summary>
    /// An orientation test card. Most simulations — noise especially — are symmetric enough
    /// that a vertical flip is invisible in them. An F is not: it is asymmetric in both axes,
    /// so it catches a flip and a mirror, which is what the pipeline asset's flipY toggle needs.
    /// </summary>
    public static class TestPattern
    {
        const int Size = 64;

        static readonly Color32 Background = new Color32(20, 20, 26, 255);
        static readonly Color32 Glyph = new Color32(255, 255, 255, 255);
        static readonly Color32 Marker = new Color32(255, 40, 40, 255);

        public static Texture2D Create()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
            {
                name = "Orientation Test Pattern",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Background;

            // Texture2D row 0 is the bottom row, so the F's top bar sits at high y.
            Fill(pixels, 18, 10, 26, 54, Glyph);   // stem
            Fill(pixels, 18, 46, 46, 54, Glyph);   // top bar
            Fill(pixels, 18, 28, 40, 36, Glyph);   // middle bar
            Fill(pixels, 3, 3, 11, 11, Marker);    // bottom-left corner marker

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false);
            return texture;
        }

        static void Fill(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 colour)
        {
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                pixels[y * Size + x] = colour;
        }
    }
}
