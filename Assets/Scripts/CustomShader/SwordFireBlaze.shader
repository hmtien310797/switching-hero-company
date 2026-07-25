// SwordFireBlaze.shader
// Làm LƯỠI KIẾM rực cháy: lưỡi kiếm nóng đỏ/cam/trắng theo nhịp nhấp nháy, và lửa liếm
// ra ngoài dọc theo rìa lưỡi kiếm, chạy dọc theo chiều dài kiếm (từ chuôi ra mũi hoặc
// ngược lại tuỳ _FlowAngle). Nhân vật là 1 sprite phẳng chưa tách lớp, nên lưỡi kiếm được
// đánh dấu bằng 1 ẢNH MASK RIÊNG (_BladeMask, cùng UV với _MainTex) để chính xác 100% theo
// đúng hình dạng lưỡi kiếm, không đụng tới tay, áo, áo choàng...
//
// CÁCH TẠO MASK (_BladeMask):
//   Mở ảnh nhân vật, tạo layer mới cùng kích thước, tô TRẮNG dọc theo lưỡi kiếm (có thể
//   để mờ dần về phía chuôi kiếm nếu muốn lửa chỉ tập trung ở phần lưỡi), ĐEN ở phần còn
//   lại. Xuất PNG grayscale, kênh R được đọc làm cường độ. Import Texture Type = Default,
//   sRGB tắt, Wrap Mode = Clamp.
//
// YÊU CẦU VỀ TEXTURE/QUAD: Cần viền TRONG SUỐT đủ rộng quanh mũi kiếm (padding) để lửa có
// chỗ liếm ra ngoài rìa lưỡi kiếm. Sprite Editor: Mesh Type = Full Rect.
//
// Chỉnh _FlowAngle cho khớp góc nghiêng thật của lưỡi kiếm trong ảnh (xem Scene view).
//
// Built-in Render Pipeline (CGPROGRAM). Vẫn chạy được trong URP vì là shader unlit
// không phụ thuộc lighting pipeline (dự án đang dùng URP - PC_RPAsset).

Shader "Custom/SwordFireBlaze"
{
    Properties
    {
        [PerRendererData] _MainTex ("Nhân vật (RGBA, cắt nền)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Aspect ("Tỉ lệ khung hình W/H", Float) = 0.85

        [Header(Blade Mask Anh danh dau luoi kiem kenh R)]
        [NoScaleOffset] _BladeMask ("Mask lưỡi kiếm (trắng = lưỡi kiếm)", 2D) = "black" {}

        [Header(Huong doc luoi kiem De lua chay doc theo kiem)]
        _FlowAngle ("Góc dọc lưỡi kiếm (độ, 0=sang phải, 90=lên trên)", Range(0,360)) = 135
        _FlowSpeed ("Tốc độ lửa chạy dọc lưỡi kiếm", Range(0,5)) = 1.4

        [Header(Lua tren luoi Mau va do sang)]
        _BladeHotColor ("Màu lõi nóng trên lưỡi", Color) = (1, 0.95, 0.75, 1)
        _BladeMidColor ("Màu cam giữa", Color) = (1, 0.45, 0.1, 1)
        _BladeGlowIntensity ("Độ sáng lửa trên lưỡi", Range(0,3)) = 1.2
        _BladeFlickerSpeed ("Tốc độ nhấp nháy trên lưỡi", Range(0,10)) = 6

        [Header(Lua liem ra ngoai vien luoi kiem)]
        _FireSearchRadius ("Tầm dò biên (UV, chứa cả độ dày lửa)", Range(0.01,0.3)) = 0.07
        _FireThickness ("Độ dày lớp lửa liếm ra", Range(0.005,0.15)) = 0.04
        _FireEdgeSoftness ("Độ mượt mép trong (sát lưỡi kiếm)", Range(0.001,0.05)) = 0.012
        _FireColorCore ("Màu lõi lửa liếm", Color) = (1, 0.9, 0.5, 1)
        _FireColorOuter ("Màu ngoài (tan dần)", Color) = (0.8, 0.08, 0.02, 0)
        _FireIntensity ("Độ sáng lửa liếm", Range(0,4)) = 1.6
        _FireLickToTip ("Độ vươn dài về phía mũi kiếm", Range(0,0.5)) = 0.18

        [Header(Nhieu Chi tiet ngon lua)]
        _FireNoiseScale ("Tỉ lệ nhiễu", Range(1,80)) = 30
        _FireTurbulence ("Độ méo biên", Range(0,0.3)) = 0.12
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
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
            float _Aspect;

            sampler2D _BladeMask;

            float _FlowAngle, _FlowSpeed;

            fixed4 _BladeHotColor, _BladeMidColor;
            float _BladeGlowIntensity, _BladeFlickerSpeed;

            float _FireSearchRadius, _FireThickness, _FireEdgeSoftness;
            fixed4 _FireColorCore, _FireColorOuter;
            float _FireIntensity, _FireLickToTip;

            float _FireNoiseScale, _FireTurbulence;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // ---- Noise: hash -> value noise -> fbm ----
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

            #define FIRE_ANGLE_STEPS 8
            #define FIRE_RADIUS_STEPS 4

            // Khoảng cách có dấu (xấp xỉ) từ uv tới viền _BladeMask=0.5 gần nhất, trong
            // phạm vi maxDist. Âm = bên trong lưỡi kiếm, dương = bên ngoài.
            float MaskSilhouetteDistance(float2 uv, float maxDist)
            {
                bool insideBlade = tex2D(_BladeMask, uv).r > 0.5;
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
                        float2 offset = float2(dir.x * rad / max(_Aspect, 0.0001), dir.y * rad);
                        bool sampleInside = tex2D(_BladeMask, uv + offset).r > 0.5;
                        if (sampleInside != insideBlade)
                            best = min(best, rad);
                    }
                }

                return insideBlade ? -best : best;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                fixed4 tex = tex2D(_MainTex, uv) * i.color;
                float alpha = tex.a;
                float maskV = tex2D(_BladeMask, uv).r;

                // ---- Hệ toạ độ dọc theo lưỡi kiếm ----
                float rad = radians(_FlowAngle);
                float2 alongDir = float2(cos(rad), sin(rad));
                float2 acrossDir = float2(-alongDir.y, alongDir.x);

                float2 p = uv - 0.5;
                p.x *= _Aspect;
                float alongCoord = dot(p, alongDir);
                float acrossCoord = dot(p, acrossDir);

                // Nhiễu chạy dọc theo chiều dài kiếm (cuộn theo thời gian)
                float2 flowUV = float2(alongCoord, acrossCoord) * _FireNoiseScale + float2(-t * _FlowSpeed, 0.0);
                float n  = FBM(flowUV);
                float n2 = FBM(flowUV * 1.8 + 3.1);

                float flicker = 0.8 + 0.2 * sin(t * _BladeFlickerSpeed + n2 * 6.2831);

                // ---- 1) Lửa nóng ngay trên lưỡi kiếm ----
                float glowT = saturate(maskV * (0.6 + 0.4 * n) * flicker);
                fixed3 hotColor = lerp(_BladeMidColor.rgb, _BladeHotColor.rgb, saturate(n * 1.3));
                fixed3 baseCol = tex.rgb + hotColor * glowT * _BladeGlowIntensity;

                // ---- 2) Lửa liếm ra ngoài rìa lưỡi kiếm ----
                float dist = MaskSilhouetteDistance(uv, _FireSearchRadius);

                // Vươn dài hơn về phía mũi kiếm (toạ độ "along" dương = hướng theo _FlowAngle)
                float tipBias = saturate(alongCoord * 2.0 + 0.5);
                float lick = _FireLickToTip * tipBias * n;

                float distJ = dist - (n - 0.5) * _FireTurbulence * 2.0 - lick;

                float edgeGrow = smoothstep(-_FireEdgeSoftness, 0.0, distJ);
                float outFade  = 1.0 - smoothstep(0.0, _FireThickness, distJ);
                float fireMask = saturate(edgeGrow) * saturate(outFade);

                float flameAlpha = saturate(fireMask * flicker) * _FireIntensity;

                float tGrad = saturate(distJ / max(_FireThickness, 0.0001));
                fixed3 fireRGB = lerp(_FireColorCore.rgb, _FireColorOuter.rgb, tGrad) * flameAlpha;

                fixed3 finalRGB = baseCol + fireRGB;
                float finalAlpha = saturate(max(alpha, flameAlpha));

                clip(finalAlpha - 0.001);
                return fixed4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
