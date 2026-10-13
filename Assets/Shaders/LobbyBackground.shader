Shader "OnlyOneUp/LobbyBackground"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 0.63, 0.27, 1)
        _ShadowColor("Shadow Color", Color) = (0.45, 0.25, 0.10, 1)
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.55
        _GradientColor("Gradient Color", Color) = (0.88, 0.44, 0.19, 1)
        _GradientStrength("Gradient Strength", Range(0, 1)) = 0
        _GradientStart("Gradient Start (World Y)", Float) = 0
        _GradientEnd("Gradient End (World Y)", Float) = 10
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Unlit" }
        Cull Back
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadowColor;
            half4 _GradientColor;
            float _ShadowStrength;
            float _GradientStrength;
            float _GradientStart;
            float _GradientEnd;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
        };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS = positions.positionCS;
            output.positionWS = positions.positionWS;
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "Background"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            half4 Frag(Varyings input) : SV_Target
            {
                // Dùng tọa độ thế giới để màu nối liền giữa hai cube khác kích thước.
                float height = saturate((input.positionWS.y - _GradientStart) /
                    max(_GradientEnd - _GradientStart, 0.001));
                half3 background = lerp(_BaseColor.rgb, _GradientColor.rgb,
                    height * _GradientStrength);
                // Chỉ lấy bóng, bỏ màu đèn và góc chiếu để nền không bị xỉn màu.
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half shadow = (1 - mainLight.shadowAttenuation) * _ShadowStrength;
                return half4(lerp(background, _ShadowColor.rgb, shadow), 1);
            }
            ENDHLSL
        }

        // Cho phép sàn tham gia depth prepass, kể cả khi URP dùng screen-space shadows.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
