// HeroAuraGlowBackdrop.shader
// Dùng cho các lớp GLOW/BLOB mờ phía SAU nhân vật (VD: các ảnh blob tím/hồng/xanh navy
// bạn đã upload - đã được làm mờ sẵn trong Photoshop).
// Hiệu ứng: quầng sáng nhấp nháy (pulse) + xoay chậm, blend cộng (additive) để tạo ánh sáng thật.
//
// Layer order gợi ý trong Canvas/Scene (từ xa tới gần camera):
//   [Background xa]  ->  [HeroAuraGlowBackdrop (blob màu)]  ->  [HeroCharacterFX (nhân vật)]

Shader "Hero/AuraGlowBackdrop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Glow Blob (PNG đã blur sẵn)", 2D) = "white" {}
        _Color ("Màu quầng sáng (theo nhân vật)", Color) = (0.7, 0.6, 1, 1)

        [Header(Pulse)]
        _PulseSpeed ("Tốc độ nhấp nháy", Range(0,5)) = 1.2
        _PulseMinAlpha ("Độ mờ thấp nhất", Range(0,1)) = 0.55
        _PulseMaxAlpha ("Độ mờ cao nhất", Range(0,2)) = 1.15

        // Chuyển động (xoay/phong to nho)
        [Header(Motion)]
        _RotateSpeed ("Tốc độ xoay chậm (độ/giây)", Range(-60,60)) = 5
        _ScalePulseAmount ("Biên độ phình to/nhỏ", Range(0,0.3)) = 0.05

        _Intensity ("Cường độ tổng", Range(0,3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One // additive: quầng sáng cộng dồn ánh sáng, không che nhân vật phía trước

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            fixed4 _Color;
            float _PulseSpeed, _PulseMinAlpha, _PulseMaxAlpha;
            float _RotateSpeed, _ScalePulseAmount, _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Xoay + phóng to nhẹ quanh tâm UV (0.5, 0.5)
            float2 RotateScaleUV(float2 uv, float degrees, float scale)
            {
                float rad = radians(degrees);
                float2 centered = (uv - 0.5) * scale;
                float s = sin(rad), c = cos(rad);
                float2x2 m = float2x2(c, -s, s, c);
                return mul(m, centered) + 0.5;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                float pulseAlpha = lerp(_PulseMinAlpha, _PulseMaxAlpha, t);
                float scale = 1.0 - _ScalePulseAmount * t; // scale < 1 => zoom in nhẹ khi t tăng

                float ang = _Time.y * _RotateSpeed;
                float2 uv = RotateScaleUV(i.uv, ang, scale);

                fixed4 tex = tex2D(_MainTex, uv);

                fixed3 col = tex.rgb * _Color.rgb * _Intensity * pulseAlpha;
                float a = tex.a * pulseAlpha * _Color.a;

                // premultiplied alpha vì blend mode là additive (One One)
                return fixed4(col * a, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
