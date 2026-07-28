Shader "Custom/CurvedWorldSpriteStars"
{
    Properties
    {
        [MainTexture] _MainTex ("Main Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1,1,1,1)

        [Header(Curved World)]
        _HorizontalCurvature ("Horizontal Curvature", Float) = 0.002
        _ForwardCurvature ("Forward Curvature", Float) = 0.0003
        _CurveStartDistance ("Forward Curve Start Distance", Float) = 0
        _HorizontalStartDistance ("Horizontal Curve Start Distance", Float) = 0
        _CurveOffsetY ("Curve Offset Y", Float) = 0

        [Header(Sprite Stars)]
        [NoScaleOffset] _StarTex ("Star Sprite Texture", 2D) = "white" {}
        _StarSpriteRect ("Star Sprite UV Rect", Vector) = (0,0,1,1)
        _StarColor ("Star Tint", Color) = (1,1,1,1)
        _GridSize ("Grid Density", Range(1,120)) = 45
        _StarThreshold ("Star Threshold", Range(0,1)) = 0.78
        _StarScale ("Star Scale", Range(0.02,1)) = 0.3
        _StarBrightness ("Star Brightness", Range(0,10)) = 2.5
        _TwinkleSpeed ("Twinkle Speed", Range(0,10)) = 2
        _TwinkleMin ("Twinkle Minimum", Range(0,1)) = 0.05
        _Aspect ("Aspect Ratio W/H", Float) = 1
        _StarRegionY ("Star Region Bottom", Range(0,1)) = 0.67
        _StarFade ("Star Region Fade", Range(0,0.5)) = 0.1
        _StarEnabled ("Star Enabled", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_StarTex);
            SAMPLER(sampler_StarTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;

                float _HorizontalCurvature;
                float _ForwardCurvature;
                float _CurveStartDistance;
                float _HorizontalStartDistance;
                float _CurveOffsetY;

                float4 _StarSpriteRect;
                half4 _StarColor;
                float _GridSize;
                float _StarThreshold;
                float _StarScale;
                float _StarBrightness;
                float _TwinkleSpeed;
                float _TwinkleMin;
                float _Aspect;
                float _StarRegionY;
                float _StarFade;
                float _StarEnabled;
            CBUFFER_END

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 Hash2(float2 p)
            {
                float2 q = float2(
                    dot(p, float2(127.1, 311.7)),
                    dot(p, float2(269.5, 183.3))
                );
                return frac(sin(q) * 43758.5453);
            }

            float3 ApplyCurvedWorld(float3 worldPos)
            {
                float xDistance = abs(worldPos.x - _WorldSpaceCameraPos.x);
                float zDistance = abs(worldPos.z - _WorldSpaceCameraPos.z);

                xDistance = max(0.0, xDistance - _HorizontalStartDistance);
                zDistance = max(0.0, zDistance - _CurveStartDistance);

                worldPos.y -= xDistance * xDistance * _HorizontalCurvature;
                worldPos.y -= zDistance * zDistance * _ForwardCurvature;
                worldPos.y += _CurveOffsetY;
                return worldPos;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                worldPos = ApplyCurvedWorld(worldPos);

                output.positionHCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 SampleStarCell(float2 cell, float2 uv, float timeValue)
            {
                float exists = step(_StarThreshold, Hash(cell + 3.7));
                float2 randomOffset = Hash2(cell) * 0.7 + 0.15;
                float2 starCenter = (cell + randomOffset) / max(_GridSize, 1.0);

                float2 delta = uv - starCenter;
                delta.x *= max(_Aspect, 0.0001);

                float cellSize = 1.0 / max(_GridSize, 1.0);
                float spriteSize = max(cellSize * _StarScale, 0.00001);
                float2 localUV = delta / spriteSize + 0.5;

                float inside = step(0.0, localUV.x) * step(localUV.x, 1.0)
                             * step(0.0, localUV.y) * step(localUV.y, 1.0);

                float2 atlasUV = _StarSpriteRect.xy + localUV * _StarSpriteRect.zw;
                half4 spriteSample = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, atlasUV);

                float phase = Hash(cell * 3.17) * TWO_PI;
                float speed = Hash(cell * 5.73) * 2.0 + 0.5;
                float pulse = sin(timeValue * _TwinkleSpeed * speed + phase) * 0.5 + 0.5;
                float twinkle = lerp(_TwinkleMin, 1.0, pulse);

                half alpha = spriteSample.a * inside * exists * twinkle * _StarEnabled;
                half3 rgb = spriteSample.rgb * _StarColor.rgb * _StarBrightness * alpha;
                return half4(rgb, alpha * _StarColor.a);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

                float regionMask = smoothstep(
                    _StarRegionY - max(_StarFade, 0.0001),
                    _StarRegionY,
                    input.uv.y
                );

                float2 baseCell = floor(input.uv * max(_GridSize, 1.0));
                half4 stars = 0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        stars += SampleStarCell(baseCell + float2(x, y), input.uv, _Time.y);
                    }
                }

                baseColor.rgb += stars.rgb * regionMask;
                return baseColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
