Shader "UI/FlowNoise"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (0.2, 0.8, 1, 1)

        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 3.0
        _FlowX ("Flow X", Float) = 0.4
        _FlowY ("Flow Y", Float) = 0.0

        _DistortStrength ("Distort Strength", Range(0, 0.2)) = 0.03
        _Threshold ("Noise Threshold", Range(0, 1)) = 0.5
        _Softness ("Noise Softness", Range(0.001, 0.5)) = 0.15
        _Brightness ("Brightness", Range(0, 4)) = 1.2

        _HighlightStrength ("Highlight Strength", Range(0, 3)) = 0.8
        _HighlightWidth ("Highlight Width", Range(0.01, 1)) = 0.2
        _HighlightSpeed ("Highlight Speed", Float) = 0.6

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _NoiseScale;
            float _FlowX;
            float _FlowY;
            float _DistortStrength;
            float _Threshold;
            float _Softness;
            float _Brightness;

            float _HighlightStrength;
            float _HighlightWidth;
            float _HighlightSpeed;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;

                // Lấy đầy đủ màu và alpha gốc của Sprite
                fixed4 mainSample = tex2D(_MainTex, uv) + _TextureSampleAdd;

                // Áp dụng màu Tint của component Image và Material
                fixed4 baseColor = mainSample * i.color * _Color;

                // Noise layer thứ nhất
                float2 noiseUV1 =
                    uv * _NoiseScale +
                    float2(_FlowX, _FlowY) * _Time.y;

                // Noise layer thứ hai chạy hướng khác để chuyển động tự nhiên hơn
                float2 noiseUV2 =
                    uv * (_NoiseScale * 1.7) +
                    float2(-_FlowX * 0.6, _FlowY * 0.5) * _Time.y;

                float n1 = tex2D(_NoiseTex, noiseUV1).r;
                float n2 = tex2D(_NoiseTex, noiseUV2).r;

                float2 distortion =
                    (float2(n1, n2) - 0.5) * _DistortStrength;

                float noise = tex2D(
                    _NoiseTex,
                    uv * _NoiseScale +
                    distortion +
                    float2(_FlowX, _FlowY) * _Time.y
                ).r;

                float flowMask = smoothstep(
                    _Threshold - _Softness,
                    _Threshold + _Softness,
                    noise
                );

                // Dải sáng chạy ngang
                float bandPosition =
                    frac(uv.x + _Time.y * _HighlightSpeed);

                float distanceToBand =
                    abs(bandPosition - 0.5);

                float highlight =
                    1.0 - smoothstep(
                        0.0,
                        _HighlightWidth,
                        distanceToBand
                    );

                // Chỉ dùng noise để điều chỉnh độ sáng,
                // không thay thế màu gốc của Sprite
                float noiseBrightness =
                    lerp(0.85, _Brightness, flowMask);

                fixed3 finalRgb =
                    baseColor.rgb * noiseBrightness;

                // Thêm ánh sáng nhưng vẫn dựa trên màu gốc
                finalRgb +=
                    baseColor.rgb *
                    highlight *
                    flowMask *
                    _HighlightStrength;

                fixed4 finalColor = fixed4(
                    finalRgb,
                    baseColor.a
                );

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(
                    i.worldPosition.xy,
                    _ClipRect
                );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}