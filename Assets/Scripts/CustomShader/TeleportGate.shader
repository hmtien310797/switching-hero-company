Shader "Custom/TeleportGate"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Portal Colors)]
        _ColorInner  ("Inner Color",  Color) = (1.0, 0.95, 0.5, 1)
        _ColorMid    ("Mid Color",    Color) = (1.0, 0.45, 0.1, 1)
        _ColorOuter  ("Outer Color",  Color) = (0.5, 0.1,  0.9, 1)

        [Header(Swirl)]
        _SwirlSpeed    ("Swirl Speed",    Float) = 1.5
        _SwirlStrength ("Swirl Strength", Float) = 4.0

        [Header(Pulse Rings)]
        _PulseSpeed     ("Pulse Speed",     Float) = 3.0
        _RingCount      ("Ring Count",      Float) = 8.0
        _PulseIntensity ("Pulse Intensity", Float) = 0.5

        [Header(Glow)]
        _GlowIntensity ("Glow Intensity", Float) = 1.8
        _RimColor      ("Rim Color",      Color) = (1.0, 0.85, 0.25, 1)
        _RimIntensity  ("Rim Intensity",  Float) = 3.0

        [Header(Noise)]
        _NoiseScale      ("Noise Scale",      Float) = 4.0
        _NoiseSpeed      ("Noise Speed",      Float) = 0.4
        _DistortStrength ("Distort Strength", Float) = 0.08

        [Header(Interior)]
        _InteriorCenterY ("Interior Center Y", Range(0,1)) = 0.45
        _InteriorBlend   ("Interior Blend",    Range(0,1)) = 0.9
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "RenderPipeline"    = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4  color       : COLOR;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _ColorInner, _ColorMid, _ColorOuter;
                float  _SwirlSpeed, _SwirlStrength;
                float  _PulseSpeed, _RingCount, _PulseIntensity;
                float  _GlowIntensity;
                half4  _RimColor;
                float  _RimIntensity;
                float  _NoiseScale, _NoiseSpeed, _DistortStrength;
                float  _InteriorCenterY, _InteriorBlend;
            CBUFFER_END

            // ── Noise ────────────────────────────────────────────────────────

            float2 _Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float _SmoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = dot(_Hash2(i),                   f);
                float b = dot(_Hash2(i + float2(1, 0)), f - float2(1, 0));
                float c = dot(_Hash2(i + float2(0, 1)), f - float2(0, 1));
                float d = dot(_Hash2(i + float2(1, 1)), f - float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 0.5 + 0.5;
            }

            // Fractal Brownian Motion — 4 octaves
            float _Fbm(float2 p)
            {
                float  v   = 0.0;
                float  amp = 0.5;
                float2x2 rot = float2x2(1.6, 1.2, -1.2, 1.6);
                UNITY_UNROLL
                for (int i = 0; i < 4; i++)
                {
                    v   += amp * _SmoothNoise(p);
                    p    = mul(rot, p);
                    amp *= 0.5;
                }
                return v;
            }

            // ── Vertex ───────────────────────────────────────────────────────

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            // ── Fragment ─────────────────────────────────────────────────────

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y;

                // 1. Original sprite
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                clip(sprite.a - 0.01);

                // 2. Polar coordinates centered on the arch interior
                float2 center   = float2(0.5, _InteriorCenterY);
                float2 centered = uv - center;
                float  dist     = length(centered);
                float  angle    = atan2(centered.y, centered.x);

                // 3. Swirl: strongest at center, fades outward
                float swirlAmt = _SwirlStrength * exp(-dist * 3.5);
                float swirlAng = angle + swirlAmt * sin(t * _SwirlSpeed);
                float2 swirlUV = float2(cos(swirlAng), sin(swirlAng)) * dist * _NoiseScale + 0.5;

                // 4. Distortion layer
                float2 distBase = uv * _NoiseScale * 0.5;
                float  dx = _SmoothNoise(distBase + float2(t * _NoiseSpeed,        0)) * 2.0 - 1.0;
                float  dy = _SmoothNoise(distBase + float2(0, t * _NoiseSpeed * 1.3)) * 2.0 - 1.0;
                float2 distortion = float2(dx, dy) * _DistortStrength;

                // 5. FBM portal surface
                float n = _Fbm(swirlUV + distortion + float2(t * 0.15, t * 0.1));

                half4 portalColor;
                if (n < 0.35)
                    portalColor = lerp(_ColorOuter, _ColorMid,   n / 0.35);
                else if (n < 0.65)
                    portalColor = lerp(_ColorMid,   _ColorInner, (n - 0.35) / 0.30);
                else
                    portalColor = lerp(_ColorInner, half4(1,1,1,1), (n - 0.65) / 0.35);

                // 6. Pulse rings emanating from center
                float ring = sin(dist * _RingCount * PI - t * _PulseSpeed) * 0.5 + 0.5;
                ring = pow(ring, 4.0);
                portalColor.rgb += ring * _PulseIntensity * _ColorInner.rgb;

                // 7. Center glow hotspot
                float centerGlow = exp(-dist * 5.0) * _GlowIntensity;
                portalColor.rgb += centerGlow * _ColorInner.rgb;

                // 8. Interior vs frame detection
                //    Dark saturated pixels = arch frame border
                //    Bright/light pixels   = interior glow area → show more portal
                float luma       = dot(sprite.rgb, float3(0.299, 0.587, 0.114));
                float interiorW  = saturate((1.0 - luma) * sprite.a * 2.0);

                half4 final;
                final.rgb = lerp(sprite.rgb * _GlowIntensity, portalColor.rgb, interiorW * _InteriorBlend);

                // 9. Rim / edge glow — detects alpha gradient along the arch border
                float2 ts = float2(0.003, 0.003);
                float aU  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(ts.x, 0)).a;
                float aD  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(ts.x, 0)).a;
                float aL  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, ts.y)).a;
                float aR  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, ts.y)).a;
                float edge     = saturate((abs(aU - aD) + abs(aL - aR)) * 5.0);
                float rimPulse = sin(t * 2.5) * 0.3 + 0.7;
                final.rgb += edge * _RimColor.rgb * _RimIntensity * rimPulse;

                // 10. Sparkles — random bright flickers
                float sparkSeed = _SmoothNoise(uv * 28.0 + float2(floor(t * 5.0) * 7.3, floor(t * 4.0) * 5.1));
                float sparkle   = pow(max(0.0, sparkSeed - 0.80), 2.0) * 18.0 * sprite.a;
                final.rgb += sparkle * half3(1.0, 0.95, 0.6);

                final.a = sprite.a;
                return final * IN.color;
            }

            ENDHLSL
        }
    }
}
