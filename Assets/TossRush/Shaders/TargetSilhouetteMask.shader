Shader "TossRush/Target Silhouette Mask"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+1" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ColorMask 0
            // Fill the entire projected proxy even where a slat lies in front of it.
            // Occlusion is still respected by the visible outline pass's depth test.
            Cull Off ZWrite Off ZTest Always
            // Use a user stencil bit; upper bits are reserved by URP.
            Stencil { Ref 1 ReadMask 1 WriteMask 1 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 vert(float4 positionOS:POSITION):SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }
            half4 frag():SV_Target { return 0; }
            ENDHLSL
        }
    }
}
