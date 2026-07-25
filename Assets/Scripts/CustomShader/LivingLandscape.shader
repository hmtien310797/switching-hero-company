// LivingLandscape.shader
// Dùng cho 1 ảnh phong cảnh nền TĨNH (núi, thung lũng, trăng, đảo bay...) đã vẽ sẵn,
// CHƯA tách lớp. Shader này làm cho ảnh "sống" bằng cách làm méo UV + chồng hiệu ứng
// thủ tục (procedural) ngay trên chính ảnh gốc, không cần thêm texture phụ nào:
//   1) Mây trôi: làm méo UV nhẹ ở vùng trời bằng nhiễu cuộn theo thời gian.
//   2) Sương/mù trong thung lũng: dải sương mờ cuộn ngang gần đường chân trời (procedural,
//      không đọc ảnh).
//   3) Trăng phát sáng nhấp nháy: quầng sáng pulse tại 1 điểm neo (đặt trùng vị trí trăng).
//   4) Sao lấp lánh: chỉ hiện ở vùng trời tối (không đè lên mây sáng), theo kiểu lưới hash.
//   5) Đảo bay bồng bềnh: làm méo UV cục bộ quanh 1 điểm neo để đảo/áng mây trông lơ lửng.
//
// CÁCH DÙNG: Gán ảnh gốc vào _MainTex. Mở Scene view, chỉnh _HorizonY cho khớp đường núi,
// _MoonPos/_IslandPos cho khớp vị trí trăng và đảo bay trong ảnh của bạn (xem trực tiếp
// bằng mắt, kéo Range trong Inspector).
//
// Built-in Render Pipeline (CGPROGRAM). Shader unlit dạng này vẫn chạy được trong URP
// (dự án đang dùng URP - PC_RPAsset) vì không phụ thuộc lighting pipeline. Nếu muốn chuẩn
// URP 100%: đổi CGPROGRAM/ENDCG -> HLSLPROGRAM/ENDHLSL, include Core.hlsl,
// UnityObjectToClipPos -> TransformObjectToHClip, thêm Tags { "RenderPipeline"="UniversalPipeline" }.

Shader "Custom/LivingLandscape"
{
    Properties
    {
        _MainTex ("Ảnh phong cảnh gốc", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Aspect ("Tỉ lệ khung hình W/H (cho các điểm neo tròn đều)", Float) = 1.777

        [Header(Duong chan troi Phan vung troi thung lung)]
        _HorizonY ("Vị trí đường chân trời (0 đáy, 1 đỉnh)", Range(0,1)) = 0.45
        _HorizonSoftness ("Độ mượt chuyển vùng", Range(0.01,0.3)) = 0.12

        [Header(May troi Lam meo UV vung troi)]
        _CloudNoiseScale ("Tỉ lệ nhiễu mây", Range(0.5,10)) = 2.2
        _CloudSpeedX ("Tốc độ trôi ngang", Float) = 0.015
        _CloudSpeedY ("Tốc độ trôi dọc", Float) = 0.004
        _CloudWarpStrength ("Cường độ méo UV", Range(0,0.05)) = 0.012

        [Header(Suong mu thung lung Procedural khong doc anh)]
        _MistColor ("Màu sương", Color) = (0.85, 0.9, 1.0, 1)
        _MistBandCenter ("Tâm dải sương (0-1 theo Y)", Range(0,1)) = 0.42
        _MistBandWidth ("Bề rộng dải sương", Range(0.02,0.6)) = 0.28
        _MistNoiseScale ("Tỉ lệ nhiễu sương", Range(1,20)) = 6.0
        _MistScrollSpeed ("Tốc độ cuộn sương", Float) = 0.03
        _MistOpacity ("Độ đậm sương", Range(0,1)) = 0.35

        [Header(Trang phat sang Diem neo pulse)]
        _MoonPos ("Vị trí trăng (UV 0-1)", Vector) = (0.11, 0.95, 0, 0)
        _MoonGlowColor ("Màu quầng sáng trăng", Color) = (1.0, 0.98, 0.9, 1)
        _MoonGlowFalloff ("Độ tắt dần quầng sáng", Range(2,60)) = 18
        _MoonGlowIntensity ("Độ sáng quầng", Range(0,2)) = 0.55
        _MoonPulseSpeed ("Tốc độ nhấp nháy", Range(0,5)) = 0.8

        [Header(Sao lap lanh Chi hien o vung troi toi)]
        _StarGridSize ("Mật độ lưới sao", Float) = 55
        _StarThreshold ("Ngưỡng xuất hiện sao (0-1, cao = it sao)", Range(0,1)) = 0.85
        _StarSize ("Kích thước điểm sao", Float) = 26
        _StarBright ("Độ sáng sao", Float) = 1.4
        _StarTwinkleSpeed ("Tốc độ lấp lánh", Float) = 2.0
        _StarBrightnessLimit ("Độ sáng nền tối đa để sao hiện", Range(0,1)) = 0.45

        [Header(Dao bay bong benh Lam meo UV cuc bo)]
        _IslandPos ("Vị trí đảo bay (UV 0-1)", Vector) = (0.03, 0.85, 0, 0)
        _IslandRadius ("Bán kính vùng ảnh hưởng", Range(0.02,0.5)) = 0.14
        _IslandBobAmount ("Biên độ bồng bềnh", Range(0,0.03)) = 0.006
        _IslandBobSpeed ("Tốc độ bồng bềnh", Range(0,3)) = 0.7
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
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

            float _HorizonY, _HorizonSoftness;

            float _CloudNoiseScale, _CloudSpeedX, _CloudSpeedY, _CloudWarpStrength;

            fixed4 _MistColor;
            float _MistBandCenter, _MistBandWidth, _MistNoiseScale, _MistScrollSpeed, _MistOpacity;

            float4 _MoonPos;
            fixed4 _MoonGlowColor;
            float _MoonGlowFalloff, _MoonGlowIntensity, _MoonPulseSpeed;

            float _StarGridSize, _StarThreshold, _StarSize, _StarBright, _StarTwinkleSpeed, _StarBrightnessLimit;

            float4 _IslandPos;
            float _IslandRadius, _IslandBobAmount, _IslandBobSpeed;

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

            // 1 sao trong 1 cell lưới, twinkle riêng theo hash
            float StarCell(float2 cell, float2 uv, float t)
            {
                float hasStar = step(_StarThreshold, Hash21(cell + float2(3.7, 3.7)));
                float2 jitter = float2(Hash21(cell), Hash21(cell + float2(9.1, 4.3)));
                float2 starUV = (cell + jitter * 0.8 + 0.1) / _StarGridSize;

                float2 d = (uv - starUV) * float2(_Aspect, 1.0) * _StarGridSize;
                float dist = length(d);

                float phase = Hash21(cell * 3.17) * 6.2832;
                float speed = Hash21(cell * 5.73) * 1.5 + 0.5;
                float twinkle = 0.15 + 0.85 * (sin(t * _StarTwinkleSpeed * speed + phase) * 0.5 + 0.5);

                return exp(-dist * (100.0 / max(_StarSize, 1.0))) * twinkle * _StarBright * hasStar;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                // 1 = vùng trời, 0 = vùng thung lũng
                float skyMask = smoothstep(_HorizonY - _HorizonSoftness, _HorizonY + _HorizonSoftness, uv.y);

                // ---- 1) Mây trôi: làm méo UV nhẹ ở vùng trời ----
                float2 cloudNoiseUV = uv * _CloudNoiseScale + float2(t * _CloudSpeedX, t * _CloudSpeedY);
                float2 cloudWarp = (float2(FBM(cloudNoiseUV), FBM(cloudNoiseUV + 9.2)) - 0.5)
                                    * _CloudWarpStrength * skyMask;

                // ---- 5) Đảo bay bồng bềnh: làm méo UV cục bộ quanh điểm neo ----
                float2 toIsland = (uv - _IslandPos.xy) * float2(_Aspect, 1.0);
                float islandMask = 1.0 - smoothstep(0.0, _IslandRadius, length(toIsland));
                float2 islandWarp = float2(0.0, sin(t * _IslandBobSpeed) * _IslandBobAmount) * islandMask;

                float2 warpedUV = uv + cloudWarp + islandWarp;
                fixed4 col = tex2D(_MainTex, warpedUV) * i.color;

                // ---- 2) Sương mù thung lũng: dải sương procedural cuộn ngang ----
                float mistBand = 1.0 - smoothstep(_MistBandWidth * 0.5, _MistBandWidth, abs(uv.y - _MistBandCenter));
                float2 mistNoiseUV = uv * _MistNoiseScale + float2(t * _MistScrollSpeed, 0.0);
                float mistN = FBM(mistNoiseUV);
                float mistMask = mistBand * (1.0 - skyMask * 0.85) * mistN;
                col.rgb += _MistColor.rgb * mistMask * _MistOpacity;

                // ---- 3) Trăng phát sáng nhấp nháy ----
                float2 toMoon = (uv - _MoonPos.xy) * float2(_Aspect, 1.0);
                float moonDist = length(toMoon);
                float moonPulse = 0.7 + 0.3 * sin(t * _MoonPulseSpeed);
                float moonGlow = exp(-moonDist * _MoonGlowFalloff) * _MoonGlowIntensity * moonPulse;
                col.rgb += _MoonGlowColor.rgb * moonGlow;

                // ---- 4) Sao lấp lánh: chỉ hiện ở vùng trời tối ----
                float luminance = dot(col.rgb, float3(0.333, 0.333, 0.334));
                float darkSkyMask = skyMask * (1.0 - smoothstep(_StarBrightnessLimit, _StarBrightnessLimit + 0.15, luminance));

                float2 baseCell = floor(uv * _StarGridSize);
                float stars = 0.0;
                stars += StarCell(baseCell + float2(-1,-1), uv, t);
                stars += StarCell(baseCell + float2( 0,-1), uv, t);
                stars += StarCell(baseCell + float2( 1,-1), uv, t);
                stars += StarCell(baseCell + float2(-1, 0), uv, t);
                stars += StarCell(baseCell + float2( 0, 0), uv, t);
                stars += StarCell(baseCell + float2( 1, 0), uv, t);
                stars += StarCell(baseCell + float2(-1, 1), uv, t);
                stars += StarCell(baseCell + float2( 0, 1), uv, t);
                stars += StarCell(baseCell + float2( 1, 1), uv, t);
                col.rgb += stars * darkSkyMask;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
