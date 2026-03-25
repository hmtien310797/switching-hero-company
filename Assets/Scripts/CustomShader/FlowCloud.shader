Shader "Custom/FlowCloud"
{
    Properties
    {
        _MainTex ("Cloud Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _MoveRangeX ("Move Range X", Range(0, 2)) = 0.5
        _MoveRangeY ("Move Range Y", Range(0, 1)) = 0.2

        _Speed ("Speed", Range(0, 5)) = 1

        _Offset ("Random Offset", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float _MoveRangeX;
            float _MoveRangeY;
            float _Speed;
            float _Offset;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float t = _Time.y * _Speed + _Offset;

                float3 pos = IN.positionOS.xyz;

                // Di chuyển qua lại theo sin (loop vô hạn)
                float moveX = sin(t) * _MoveRangeX;

                // Lên xuống nhẹ (lệch phase để tự nhiên)
                float moveY = cos(t * 0.7) * _MoveRangeY;

                pos.x += moveX;
                pos.y += moveY;

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return col * IN.color;
            }

            ENDHLSL
        }
    }
}