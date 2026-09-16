Shader "Hidden/ComputePlayground/TextureBlit"
{
    SubShader
    {
        Pass
        {
            Name "TextureBlit"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            // Deliberately includes nothing. No UnityCG.cginc (built-in pipeline legacy),
            // no SRP Core headers. That keeps this shader compiling across editor versions.
            Texture2D _SourceTex;
            SamplerState sampler_SourceTex;   // inherits the RenderTexture's own filterMode
            float4 _BlitScale;                // xy: clip-space scale for aspect fit
            float _FlipY;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                // One oversized triangle: ids 0,1,2 -> uv (0,0) (2,0) (0,2),
                // so uv 0..1 spans exactly the region the scaled triangle covers.
                float2 uv = float2((vertexID << 1) & 2, vertexID & 2);

                Varyings output;
                output.positionCS = float4((uv * 2.0 - 1.0) * _BlitScale.xy, 0.0, 1.0);
                output.uv = float2(uv.x, lerp(uv.y, 1.0 - uv.y, _FlipY));
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return _SourceTex.Sample(sampler_SourceTex, input.uv);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
