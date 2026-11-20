Shader "AnatoLab/Ghost"
{
    Properties { _BaseColor ("Ghost Color", Color) = (0.2,0.65,0.72,0.12) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Ghost"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half rim = pow(1 - saturate(abs(dot(normalize(i.normalWS), normalize(GetWorldSpaceViewDir(i.positionWS))))), 2);
                return half4(_BaseColor.rgb * (0.65h + rim * 0.35h), _BaseColor.a * (0.4h + rim * 1.3h));
            }
            ENDHLSL
        }
    }
}

// AnatoLab package revision: 1795099051116948000
