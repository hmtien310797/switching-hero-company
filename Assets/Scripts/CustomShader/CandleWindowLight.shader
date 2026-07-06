Shader "Custom/CandleWindowLight"
{
    Properties
    {
        _MainTex          ("Window Light Texture", 2D)      = "white" {}
        _Color            ("Tint",                 Color)   = (1,1,1,1)

        [Header(Flicker)]
        _FlickerSpeed     ("Flicker Speed",        Float)   = 8.0
        _FlickerStrength  ("Flicker Strength",     Range(0,1)) = 0.25
        _FlickerMin       ("Min Brightness",       Range(0,1)) = 0.75

        [Header(Sway Light Source Moves)]
        _SwaySpeed        ("Sway Speed",           Float)   = 2.5
        _SwayStrength     ("Sway Strength",        Float)   = 0.018

        [Header(Color Shift)]
        _WarmColor        ("Warm Color",           Color)   = (1.0, 0.85, 0.35, 1)
        _CoolColor        ("Cool Color",           Color)   = (1.0, 0.55, 0.15, 1)
        _ColorShiftSpeed  ("Color Shift Speed",    Float)   = 1.8

        [Header(Intensity Pulse)]
        _GlowIntensity    ("Glow Intensity",       Float)   = 1.2
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

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            float4 _Color;
            float  _FlickerSpeed, _FlickerStrength, _FlickerMin;
            float  _SwaySpeed, _SwayStrength;
            float4 _WarmColor, _CoolColor;
            float  _ColorShiftSpeed, _GlowIntensity;

            // Flicker ngọn nến: ghép nhiều sin tần số lệch nhau → không đều, tự nhiên
            float CandleFlicker(float t, float speed)
            {
                float f = 0.0;
                f += sin(t * speed * 1.00        ) * 0.40;
                f += sin(t * speed * 2.70 + 0.80 ) * 0.25;
                f += sin(t * speed * 0.43 + 1.50 ) * 0.20;
                f += sin(t * speed * 5.13 + 3.10 ) * 0.10;
                f += sin(t * speed * 8.91 + 0.25 ) * 0.05;
                return f * 0.5 + 0.5; // normalize 0..1
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv    = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y;

                // ── 1. Sway: nguồn sáng lắc nhẹ như ngọn nến ────────────
                float sx = sin(t * _SwaySpeed * 1.00 + 0.0) * 0.6
                         + sin(t * _SwaySpeed * 2.30 + 1.2) * 0.4;
                float sy = sin(t * _SwaySpeed * 0.70 + 0.5) * 0.5
                         + sin(t * _SwaySpeed * 1.80 + 2.0) * 0.5;
                float2 swayedUV = uv + float2(sx, sy) * _SwayStrength;

                // ── 2. Sample texture với UV đã sway ─────────────────────
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, swayedUV);

                // ── 3. Flicker brightness ─────────────────────────────────
                float flicker = CandleFlicker(t, _FlickerSpeed);
                float brightness = lerp(_FlickerMin, 1.0, flicker);
                brightness = lerp(1.0, brightness, _FlickerStrength);

                // ── 4. Color shift warm ↔ cool theo nhịp flicker ─────────
                float colorPhase = CandleFlicker(t, _ColorShiftSpeed) ;
                float3 candleColor = lerp(_CoolColor.rgb, _WarmColor.rgb, colorPhase);

                // Nhân color shift vào vùng sáng (dựa vào luminance texture)
                float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, col.rgb * candleColor, luma * 0.6);

                // ── 5. Áp flicker và intensity ────────────────────────────
                col.rgb *= brightness * _GlowIntensity;

                return col * IN.color;
            }
            ENDHLSL
        }
    }
}
