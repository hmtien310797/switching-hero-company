// RelicFireFrame.shader
// Dùng cho các sprite "phù điêu / relic / medallion" (icon khắc chạm, viền tròn/bo góc).
// Gộp 2 nhóm hiệu ứng trong 1 pass:
//   1) Hiệu ứng cho CHÍNH phù điêu: tự phát sáng nhấp nháy (breathing glow) + viền hào quang
//      (rim) + tia sáng quét chéo (shine sweep) + phóng to/nhỏ nhẹ theo nhịp thở (vertex).
//   2) Lửa cháy PROCEDURAL bao quanh phù điêu: không cần texture lửa riêng. Lửa BÁM SÁT
//      hình dạng thật của phù điêu bằng cách dò kênh alpha của chính _MainTex theo nhiều
//      hướng quanh mỗi pixel (không dùng SDF hình học giả định) -> ôm trọn mọi hình dạng
//      (cuộn giấy, khiên, mề đay...) chứ không chỉ hình chữ nhật/oval.
//
// YÊU CẦU VỀ TEXTURE / QUAD:
//   Sprite/RectTransform phải có viền TRONG SUỐT (padding) đủ rộng quanh hình phù điêu
//   (khuyến nghị pad thêm ~35-50% mỗi cạnh) thì lửa mới có "đất" để cháy ra ngoài rìa.
//   Nếu dùng Sprite Editor, đặt Mesh Type = Full Rect để giữ nguyên phần trong suốt đó.
//
// Chỉnh _FireSearchRadius (tầm dò biên) / _FireThickness / _FireAspect trong Inspector
// cho khớp với biên dạng thật của phù điêu (xem preview trong Scene và chỉnh bằng mắt).
//
// Built-in Render Pipeline (CGPROGRAM). Nếu dự án dùng URP: đổi CGPROGRAM/ENDCG ->
// HLSLPROGRAM/ENDHLSL, include Core.hlsl, UnityObjectToClipPos -> TransformObjectToHClip,
// và thêm Tags { "RenderPipeline"="UniversalPipeline" }. Phần logic fragment giữ nguyên.

Shader "Custom/RelicFireFrame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Phù điêu (RGBA, có viền trong suốt padding quanh)", 2D) = "white" {}
        _Color ("Tint tổng", Color) = (1,1,1,1)

        [Header(Relic Breathing Phu dieu tu phat sang)]
        _GlowSpeed ("Tốc độ nhấp nháy sáng", Range(0,5)) = 1.0
        _GlowMin ("Độ sáng thấp nhất", Range(0,2)) = 0.85
        _GlowMax ("Độ sáng cao nhất", Range(0,2)) = 1.25

        [Header(Relic Rim Vien hao quang phu dieu)]
        _RimColor ("Màu viền", Color) = (1, 0.75, 0.35, 1)
        _RimIntensity ("Độ sáng viền", Range(0,6)) = 2.0
        _RimPulseSpeed ("Tốc độ nhấp nháy viền", Range(0,10)) = 1.5
        _RimPulseAmount ("Biên độ nhấp nháy viền", Range(0,1)) = 0.35
        _EdgeSoftness ("Khoảng lấy mẫu viền (UV)", Range(0.001,0.02)) = 0.006

        [Header(Relic Shine Sweep Tia sang quet cheo)]
        _ShineColor ("Màu tia quét", Color) = (1, 0.95, 0.8, 1)
        _ShineWidth ("Độ rộng tia", Range(0.01,1)) = 0.18
        _ShineSpeed ("Tốc độ quét", Range(0,5)) = 0.5
        _ShineAngle ("Góc quét (độ)", Range(0,180)) = 65
        _ShineIntensity ("Độ sáng tia quét", Range(0,4)) = 1.5
        _ShineGap ("Khoảng nghỉ giữa 2 lần quét", Range(0,6)) = 2.2

        [Header(Relic Breathe Scale Phong to nho nhe)]
        _BreatheAmount ("Biên độ phóng to/nhỏ", Range(0,0.15)) = 0.03
        _BreatheSpeed ("Tốc độ thở", Range(0,5)) = 1.0

        [Header(Fire Frame Shape Bam sat vien alpha that cua phu dieu)]
        _FireSearchRadius ("Tầm dò biên (UV, chứa cả do day lua)", Range(0.02,0.5)) = 0.2
        _FireAspect ("Hiệu chỉnh tỉ lệ khung hình (W/H)", Float) = 1.0
        _FireEdgeSoftness ("Độ mượt mép trong (sát vật thể)", Range(0.001,0.1)) = 0.02

        [Header(Fire Frame Look Mau va do day ngon lua)]
        _FireColorCore ("Màu lõi lửa (sát vật thể)", Color) = (1, 0.95, 0.6, 1)
        _FireColorMid ("Màu giữa", Color) = (1, 0.5, 0.1, 1)
        _FireColorOuter ("Màu ngoài (tan dần)", Color) = (0.7, 0.05, 0.02, 0)
        _FireThickness ("Độ dày lớp lửa", Range(0.01,0.5)) = 0.14
        _FireIntensity ("Độ sáng tổng lửa", Range(0,4)) = 1.4

        [Header(Fire Frame Motion Chuyen dong ngon lua)]
        _FireNoiseScale ("Tỉ lệ nhiễu (chi tiết ngọn lửa)", Range(1,40)) = 14
        _FireRiseSpeed ("Tốc độ lửa bốc lên", Range(0,4)) = 1.2
        _FireFlickerSpeed ("Tốc độ rung/nhấp nháy", Range(0,10)) = 4.0
        _FireTurbulence ("Độ méo biên (ngọn lửa liếm ra ngoài)", Range(0,0.3)) = 0.09
        _FireLickHeight ("Độ vươn lên của lưỡi lửa phía trên", Range(0,0.6)) = 0.22
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

            float _GlowSpeed, _GlowMin, _GlowMax;

            fixed4 _RimColor;
            float _RimIntensity, _RimPulseSpeed, _RimPulseAmount, _EdgeSoftness;

            fixed4 _ShineColor;
            float _ShineWidth, _ShineSpeed, _ShineAngle, _ShineIntensity, _ShineGap;

            float _BreatheAmount, _BreatheSpeed;

            float _FireSearchRadius, _FireAspect, _FireEdgeSoftness;

            fixed4 _FireColorCore, _FireColorMid, _FireColorOuter;
            float _FireThickness, _FireIntensity;

            float _FireNoiseScale, _FireRiseSpeed, _FireFlickerSpeed, _FireTurbulence, _FireLickHeight;

            v2f vert (appdata v)
            {
                v2f o;

                // Phóng to/nhỏ nhẹ quanh pivot (yêu cầu pivot đặt ở tâm phù điêu)
                float breathe = 1.0 + sin(_Time.y * _BreatheSpeed) * _BreatheAmount;
                v.vertex.xy *= breathe;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // ---- Noise: hash -> value noise -> fbm (dùng cho lửa) ----
            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float FBM(float2 p)
            {
                float sum = 0.0, amp = 0.5, freq = 1.0;
                [unroll]
                for (int k = 0; k < 3; k++)
                {
                    sum += ValueNoise(p * freq) * amp;
                    freq *= 2.03;
                    amp *= 0.5;
                }
                return sum;
            }

            // Ước lượng viền (rim) bằng cách so alpha tại pixel hiện tại với alpha trung bình
            // của 4 điểm lân cận -> nơi alpha giảm nhanh (biên phù điêu) sẽ sáng lên.
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

            // Số hướng / số nấc bán kính dùng để dò biên alpha thật quanh mỗi pixel.
            // Chi phí = FIRE_ANGLE_STEPS * FIRE_RADIUS_STEPS lần lấy mẫu texture / pixel.
            #define FIRE_ANGLE_STEPS 8
            #define FIRE_RADIUS_STEPS 4

            // Khoảng cách có dấu (xấp xỉ) từ uv tới viền alpha=0.5 gần nhất của _MainTex,
            // trong phạm vi tìm kiếm maxDist. Âm = đang ở bên trong phù điêu, dương = bên ngoài.
            // Nhờ dò trực tiếp trên alpha thật nên lửa ôm sát MỌI hình dạng, không chỉ hình học đơn giản.
            float SilhouetteDistance(float2 uv, float maxDist)
            {
                bool insideShape = tex2D(_MainTex, uv).a > 0.5;
                float best = maxDist;

                [unroll]
                for (int a = 0; a < FIRE_ANGLE_STEPS; a++)
                {
                    float ang = (a / (float)FIRE_ANGLE_STEPS) * 6.28318530718;
                    float2 dir = float2(cos(ang), sin(ang));

                    [unroll]
                    for (int r = 1; r <= FIRE_RADIUS_STEPS; r++)
                    {
                        float rad = maxDist * (r / (float)FIRE_RADIUS_STEPS);
                        // Chia lại theo aspect de vong do luon tron trong khong gian hien thi thuc te
                        float2 offset = float2(dir.x * rad / max(_FireAspect, 0.0001), dir.y * rad);
                        bool sampleInside = tex2D(_MainTex, uv + offset).a > 0.5;
                        if (sampleInside != insideShape)
                            best = min(best, rad);
                    }
                }

                return insideShape ? -best : best;
            }

            // Lửa procedural bám sát viền alpha thật của phù điêu.
            // Trả về (rgb, alpha) đã tính sẵn cường độ, chưa premultiply.
            fixed4 FireFrame(float2 uv, float t)
            {
                float2 p = uv - 0.5;
                p.x *= _FireAspect;

                float dist = SilhouetteDistance(uv, _FireSearchRadius);

                // Nhiễu cuộn lên trên theo thời gian để tạo cảm giác lửa bốc cháy
                float2 noiseUV = p * _FireNoiseScale + float2(0.0, -t * _FireRiseSpeed);
                float n  = FBM(noiseUV);
                float n2 = FBM(noiseUV * 1.7 + 5.2);

                // Lưỡi lửa vươn cao hơn ở phía trên (lửa bốc lên, không rơi xuống)
                float upMask = saturate(uv.y);
                float lick = _FireLickHeight * upMask * n;

                // Làm méo biên bằng nhiễu để lửa "liếm" ra ngoài một cách bất quy tắc
                float distJ = dist - (n - 0.5) * _FireTurbulence * 2.0 - lick;

                float edgeGrow = smoothstep(-_FireEdgeSoftness, 0.0, distJ);
                float outFade  = 1.0 - smoothstep(0.0, _FireThickness, distJ);
                float mask = saturate(edgeGrow) * saturate(outFade);

                float flicker = 0.75 + 0.25 * sin(t * _FireFlickerSpeed + n2 * 6.2831);
                float flameAlpha = saturate(mask * flicker) * _FireIntensity;

                float tGrad = saturate(distJ / max(_FireThickness, 0.0001));
                fixed3 col = lerp(_FireColorCore.rgb, _FireColorMid.rgb, saturate(tGrad * 2.0));
                col = lerp(col, _FireColorOuter.rgb, saturate(tGrad * 2.0 - 1.0));

                return fixed4(col * flameAlpha, saturate(flameAlpha));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;

                fixed4 tex = tex2D(_MainTex, uv) * i.color;
                float alpha = tex.a;

                // ---- 1) Tự phát sáng nhấp nháy (breathing glow) ----
                float glowT = 0.5 + 0.5 * sin(t * _GlowSpeed);
                float glowAmt = lerp(_GlowMin, _GlowMax, glowT);
                fixed3 baseCol = tex.rgb * glowAmt;

                // ---- 2) Viền hào quang nhấp nháy ----
                float rim = EdgeMask(uv, _EdgeSoftness) * _RimIntensity;
                float rimPulse = 1.0 + sin(t * _RimPulseSpeed) * _RimPulseAmount;
                fixed3 rimGlow = _RimColor.rgb * rim * rimPulse;

                // ---- 3) Tia sáng quét chéo ----
                float rad = radians(_ShineAngle);
                float2 dir = float2(cos(rad), sin(rad));
                float proj = dot(uv - 0.5, dir) + 0.5;
                float period = 1.0 + _ShineGap;
                float cycle = frac(t * _ShineSpeed / period) * period;
                float dist1 = abs(proj - cycle);
                float shineMask = smoothstep(_ShineWidth, 0.0, dist1) * alpha;
                fixed3 shine = _ShineColor.rgb * shineMask * _ShineIntensity;

                // ---- 4) Lửa cháy procedural bao quanh ----
                fixed4 fire = FireFrame(uv, t);

                fixed3 finalRGB = baseCol + rimGlow + shine + fire.rgb;
                float finalAlpha = saturate(max(alpha, fire.a));

                clip(finalAlpha - 0.001);
                return fixed4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
