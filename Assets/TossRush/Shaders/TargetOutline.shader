Shader "TossRush/Target Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1,0.9,0.35,1)
        _OutlineWidth("Outline Width (pixels)", Range(0,8)) = 2.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+2" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            // The earlier mask fills the complete target silhouette, including seams and holes.
            // Only fragments outside that silhouette may draw the expanded hull.
            Stencil { Ref 1 ReadMask 1 WriteMask 0 Comp NotEqual Pass Keep }
            Cull Off ZWrite Off ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _OutlineColor;
            float _OutlineWidth;
            CBUFFER_END
            // The authored mesh contains sixteen copies of the complete silhouette.
            // UV0 stores each copy's screen-space dilation direction. Unlike normal
            // extrusion, this also expands flat caps, hard corners and thin parts.
            struct Attributes { float4 positionOS:POSITION; float2 direction:TEXCOORD0; };
            float4 vert(Attributes v):SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float4 clip = TransformWorldToHClip(positionWS);
                clip.xy += v.direction * (_OutlineWidth * 2 / _ScreenParams.xy) * clip.w;
                // Lift only outline depth slightly to retain the floor-contact edge.
                // The stencil still excludes the object's entire unexpanded interior.
                float3 view = TransformWorldToView(positionWS);
                view.z += 0.035;
                float4 biasedClip = TransformWViewToHClip(view);
                clip.z = biasedClip.z / biasedClip.w * clip.w;
                return clip;
            }
            half4 frag():SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
