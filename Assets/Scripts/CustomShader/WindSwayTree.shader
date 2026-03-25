Shader "Custom/WindSwayTree"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _WindStrengthX ("Wind Strength X", Range(0, 0.3)) = 0.08
        _WindStrengthY ("Wind Strength Y", Range(0, 0.1)) = 0.03

        _WindSpeed ("Wind Speed", Range(0, 10)) = 2
        _Stiffness ("Stiffness", Range(1, 5)) = 2.5
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
            float _WindStrengthX;
            float _WindStrengthY;
            float _WindSpeed;
            float _Stiffness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 pos = IN.positionOS.xyz;

                float t = _Time.y * _WindSpeed;

                // Mask chiều cao (fix gốc)
                float h = saturate((IN.uv.y - 0.05) * 1.2);

                // Bending (quan trọng)
                float bend = pow(h, _Stiffness);

                // Random per object
                float rand = frac(sin(dot(pos.xy, float2(12.9898,78.233))) * 43758.5453);

                // Tạo chuyển động elip (X + Y lệch phase)
                float wave = t + rand * 3;

                float offsetX = sin(wave) * _WindStrengthX;
                float offsetY = cos(wave * 0.8) * _WindStrengthY;

                // Áp dụng theo độ cao
                pos.x += offsetX * bend;
                pos.y += offsetY * bend;

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return tex * IN.color;
            }

            ENDHLSL
        }
    }
}