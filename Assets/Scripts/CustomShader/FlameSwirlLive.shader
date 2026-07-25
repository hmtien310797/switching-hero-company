// FlameSwirlLive.shader
// Dùng cho các texture VFX lửa/năng lượng đã VẼ SẴN (vòng xoáy, hào quang cháy...) như
// ảnh vòng lửa xoáy hồng-cam-vàng. Ảnh đã có hình lửa rồi nên KHÔNG vẽ thêm lửa mới, chỉ
// "thổi sức sống" vào chính ảnh đó bằng 3 lớp chuyển động:
//   1) Làm méo UV bằng nhiễu cuộn theo thời gian (lửa trông dao động/lung linh thay vì
//      đứng yên như ảnh tĩnh).
//   2) Nhấp nháy độ sáng + độ mờ theo nhịp (thở lửa).
//   3) Nhún nhích cả khối lửa qua lại (nhiều tần số sin lệch pha, không đều như gió tạt).
//
// BLEND MODE: mặc định dùng alpha blend thường (SrcAlpha OneMinusSrcAlpha), hợp khi ảnh có
// nền trong suốt thật. Nếu dùng làm hiệu ứng bùng nổ/cộng sáng đè lên nền tối, đổi dòng
// "Blend SrcAlpha OneMinusSrcAlpha" thành "Blend One One" (additive) để rực hơn.
//
// Built-in Render Pipeline (CGPROGRAM). Vẫn chạy được trong URP vì là shader unlit
// không phụ thuộc lighting pipeline (dự án đang dùng URP - PC_RPAsset).

Shader "Custom/FlameSwirlLive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Vòng lửa xoáy (đã vẽ sẵn)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Meo UV Lua dao dong lung linh)]
        _DistortScale ("Tỉ lệ nhiễu méo UV", Range(1,20)) = 6
        _DistortSpeed ("Tốc độ cuộn nhiễu", Range(0,5)) = 1.8
        _DistortStrength ("Cường độ méo UV", Range(0,0.08)) = 0.02

        [Header(Nhap nhay Do sang va do mo)]
        _PulseSpeed ("Tốc độ nhấp nháy", Range(0,5)) = 2.5
        _PulseMinBright ("Độ sáng thấp nhất", Range(0,2)) = 0.85
        _PulseMaxBright ("Độ sáng cao nhất", Range(0,2)) = 1.5
        _PulseMinAlpha ("Độ mờ thấp nhất", Range(0,1)) = 0.75
        _PulseMaxAlpha ("Độ mờ cao nhất", Range(0,1.5)) = 1.1

        [Header(Nhun nhich Lac ca khoi lua qua lai)]
        _JiggleAmount ("Biên độ nhún nhích", Range(0,0.1)) = 0
        _JiggleSpeed ("Tốc độ nhún nhích", Range(0,5)) = 1.8
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "PreviewType"="Plane" }
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

            float _DistortScale, _DistortSpeed, _DistortStrength;
            float _PulseSpeed, _PulseMinBright, _PulseMaxBright, _PulseMinAlpha, _PulseMaxAlpha;
            float _JiggleAmount, _JiggleSpeed;

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

            v2f vert (appdata v)
            {
                v2f o;

                // Nhún nhích cả khối lửa qua lại: cộng nhiều tần số sin lệch pha để
                // chuyển động không đều đặn, giống lửa bị gió tạt qua lại.
                float t = _Time.y;
                float jiggleX = sin(t * _JiggleSpeed) * 0.6
                              + sin(t * _JiggleSpeed * 2.3 + 1.7) * 0.4;
                float jiggleY = sin(t * _JiggleSpeed * 1.7 + 0.5) * 0.5
                              + sin(t * _JiggleSpeed * 3.1 + 2.4) * 0.3;
                v.vertex.xy += float2(jiggleX, jiggleY) * _JiggleAmount;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;

                float2 uv = i.uv;

                // Méo UV bằng nhiễu cuộn theo thời gian -> lửa dao động lung linh
                float2 noiseUV = uv * _DistortScale + float2(t * _DistortSpeed, -t * _DistortSpeed * 0.7);
                float2 warp = float2(FBM(noiseUV), FBM(noiseUV + 5.3)) - 0.5;
                uv += warp * _DistortStrength;

                fixed4 tex = tex2D(_MainTex, uv) * i.color;

                float pulseT = 0.5 + 0.5 * sin(t * _PulseSpeed);
                float bright = lerp(_PulseMinBright, _PulseMaxBright, pulseT);
                float alphaMul = lerp(_PulseMinAlpha, _PulseMaxAlpha, pulseT);

                fixed3 finalRGB = tex.rgb * bright;
                float finalAlpha = saturate(tex.a * alphaMul);

                clip(finalAlpha - 0.001);
                return fixed4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
