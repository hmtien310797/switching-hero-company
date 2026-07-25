// HeroCharacterFX.shader
// Dùng cho các sprite NHÂN VẬT ĐÃ CẮT NỀN (ví dụ: thợ săn kiếm, nữ hoàng tím, đạo sĩ, cướp biển...)
// Gộp 3 hiệu ứng trong 1 pass duy nhất:
//   1) Viền hào quang (rim glow) nhấp nháy theo alpha
//   2) Tia sáng "quét bùa" chéo qua nhân vật (giống hiệu ứng cuộn giấy/logo)
//   3) Hạt sáng lấp lánh bay lên, dùng texture chấm (VD: ảnh 9_1.png / 16.png bạn có)
//
// Built-in Render Pipeline (CGPROGRAM). Nếu dự án dùng URP, xem ghi chú cuối file.

Shader "Hero/CharacterAuraFX"
{
    Properties
    {
        [PerRendererData] _MainTex ("Character Sprite (RGBA, cắt nền)", 2D) = "white" {}
        _Color ("Tint tổng", Color) = (1,1,1,1)

        // Rim Glow - hao quang vien
        [Header(Rim Glow)]
        _RimColor ("Màu hào quang viền", Color) = (0.6, 0.8, 1, 1)
        _RimWidth ("Độ dày viền (ảnh hưởng edge softness)", Range(0.001, 0.05)) = 0.006
        _RimIntensity ("Độ sáng viền", Range(0, 6)) = 2.0
        _RimPulseSpeed ("Tốc độ nhấp nháy viền", Range(0, 10)) = 1.5
        _RimPulseAmount ("Biên độ nhấp nháy", Range(0,1)) = 0.35

        // Rune Sweep - tia sang quet
        [Header(Rune Sweep)]
        _SweepColor ("Màu tia quét", Color) = (1, 0.95, 0.75, 1)
        _SweepWidth ("Độ rộng tia", Range(0.01, 1)) = 0.15
        _SweepSpeed ("Tốc độ quét", Range(0, 5)) = 0.6
        _SweepAngle ("Góc quét (độ)", Range(0, 180)) = 65
        _SweepIntensity ("Độ sáng tia quét", Range(0,5)) = 2.0
        _SweepGap ("Khoảng nghỉ giữa 2 lần quét", Range(0,5)) = 1.6

        // Sparkle Particles - hat sang
        [Header(Sparkle Particles)]
        [NoScaleOffset] _SparkleTex ("Texture hạt sáng (grayscale, tileable)", 2D) = "black" {}
        _SparkleColor ("Màu hạt sáng", Color) = (1,1,1,1)
        _SparkleTiling ("Mật độ hạt (x,y)", Vector) = (3,3,0,0)
        _SparkleSpeed ("Tốc độ bay lên", Range(0,3)) = 0.3
        _SparkleTwinkleSpeed ("Tốc độ nhấp nháy hạt", Range(0,10)) = 3
        _SparkleIntensity ("Độ sáng hạt", Range(0,3)) = 1

        [Header(Misc)]
        _EdgeSoftness ("Khoảng lấy mẫu viền (UV)", Range(0.001, 0.02)) = 0.006
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            fixed4 _RimColor;
            float _RimWidth; // dùng như hệ số nhân cho EdgeSoftness nếu muốn mở rộng
            float _RimIntensity;
            float _RimPulseSpeed;
            float _RimPulseAmount;

            fixed4 _SweepColor;
            float _SweepWidth;
            float _SweepSpeed;
            float _SweepAngle;
            float _SweepIntensity;
            float _SweepGap;

            sampler2D _SparkleTex;
            fixed4 _SparkleColor;
            float4 _SparkleTiling;
            float _SparkleSpeed;
            float _SparkleTwinkleSpeed;
            float _SparkleIntensity;

            float _EdgeSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // Ước lượng viền (rim) bằng cách so alpha tại pixel hiện tại với alpha trung bình
            // của 4 điểm lân cận -> nơi alpha giảm nhanh (biên nhân vật) sẽ sáng lên.
            float EdgeMask(float2 uv, float softness)
            {
                float a0     = tex2D(_MainTex, uv).a;
                float aUp    = tex2D(_MainTex, uv + float2(0, softness)).a;
                float aDown  = tex2D(_MainTex, uv - float2(0, softness)).a;
                float aLeft  = tex2D(_MainTex, uv - float2(softness, 0)).a;
                float aRight = tex2D(_MainTex, uv + float2(softness, 0)).a;
                float avg = (aUp + aDown + aLeft + aRight) * 0.25;
                float edge = saturate((a0 - avg) * -4.0);
                return saturate(edge) * a0;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;
                float alpha = tex.a;
                clip(alpha - 0.001);

                // ---- 1) Rim Glow nhấp nháy ----
                float softness = _EdgeSoftness * max(_RimWidth * 10.0, 1.0);
                float rim = EdgeMask(i.uv, softness) * _RimIntensity;
                float pulse = 1.0 + sin(_Time.y * _RimPulseSpeed) * _RimPulseAmount;
                fixed3 rimGlow = _RimColor.rgb * rim * pulse;

                // ---- 2) Tia sáng quét chéo (rune sweep) ----
                float rad = radians(_SweepAngle);
                float2 dir = float2(cos(rad), sin(rad));
                float proj = dot(i.uv - 0.5, dir) + 0.5; // toạ độ chéo 0..1
                float period = 1.0 + _SweepGap;
                float cycle = frac(_Time.y * _SweepSpeed / period) * period;
                float dist = abs(proj - cycle);
                float sweepMask = smoothstep(_SweepWidth, 0.0, dist) * alpha;
                fixed3 sweep = _SweepColor.rgb * sweepMask * _SweepIntensity;

                // ---- 3) Hạt sáng lấp lánh bay lên ----
                float2 sparkleUV = i.uv * _SparkleTiling.xy;
                sparkleUV.y -= _Time.y * _SparkleSpeed;
                float sparkleSample = tex2D(_SparkleTex, sparkleUV).r;
                float twinkle = 0.5 + 0.5 * sin(_Time.y * _SparkleTwinkleSpeed
                                    + sparkleUV.x * 17.0 + sparkleUV.y * 23.0);
                float sparkle = sparkleSample * twinkle * alpha;
                fixed3 sparkleCol = _SparkleColor.rgb * sparkle * _SparkleIntensity;

                fixed3 finalRGB = tex.rgb + rimGlow + sweep + sparkleCol;
                return fixed4(finalRGB, alpha);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}

/*
 CHUYỂN SANG URP:
 - Đổi CGPROGRAM/ENDCG -> HLSLPROGRAM/ENDHLSL
 - #include "UnityCG.cginc" -> #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 - UnityObjectToClipPos(v.vertex) -> TransformObjectToHClip(v.vertex.xyz)
 - Thêm Tags { "RenderPipeline"="UniversalPipeline" }
 Phần logic fragment (rim/sweep/sparkle) giữ nguyên 100%.
*/
