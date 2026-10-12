Shader "OnlyOneUp/EggTrajectoryLine"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; };
            struct Output { float4 position : SV_POSITION; };
            Output vert(Input v)
            {
                Output o;
                o.position = UnityObjectToClipPos(v.vertex);
                return o;
            }
            half4 frag(Output i) : SV_Target { return half4(1, 0.015, 0.015, 1); }
            ENDHLSL
        }
    }
}
