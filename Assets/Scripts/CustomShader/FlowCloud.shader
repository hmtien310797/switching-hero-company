Shader "Custom/FlowCloud"
{
    Properties
    {
        _MainTex ("Cloud Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _SpeedX ("Speed X", Float) = 0.05
        _SpeedY ("Speed Y", Float) = 0.01

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
            float _SpeedX;
            float _SpeedY;
            float _Offset;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // ❌ KHÔNG di chuyển vertex nữa
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y + _Offset;

                // 👉 UV scroll + wrap
                float2 uv = IN.uv + float2(_SpeedX, _SpeedY) * t;

                // 👉 loop lại khi vượt UV
                uv = frac(uv);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                return col * IN.color;
            }

            ENDHLSL
        }
    }
}