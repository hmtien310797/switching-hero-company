Shader "Custom/CloudFlyOverlay"
{
    Properties
    {
        _MainTex     ("Cloud Texture (white bg)", 2D)          = "white" {}
        _Color       ("Cloud Color",               Color)       = (0.85, 0.88, 1.0, 1.0)

        // Lop may 1 - gan, to, nhanh
        _SpeedX      ("Layer 1 Speed X",           Float)       = 0.04
        _SpeedY      ("Layer 1 Speed Y",           Float)       = 0.0
        _Opacity1    ("Layer 1 Opacity",           Range(0, 1)) = 1.0

        // Lop may 2 - xa, nho, cham
        _Speed2X     ("Layer 2 Speed X",           Float)       = 0.022
        _Speed2Y     ("Layer 2 Speed Y",           Float)       = 0.0
        _Offset2     ("Layer 2 UV Offset X",       Range(0, 1)) = 0.45
        _Scale2      ("Layer 2 Scale",             Range(0.5, 2)) = 1.3
        _Opacity2    ("Layer 2 Opacity",           Range(0, 1)) = 0.55

        _Intensity   ("Overall Intensity",         Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        // Additive: trang->den->vo hieu, may co mau->sang->hien len nen
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            float _SpeedX;
            float _SpeedY;
            float _Opacity1;

            float _Speed2X;
            float _Speed2Y;
            float _Offset2;
            float _Scale2;
            float _Opacity2;

            float _Intensity;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Fade bien de an duong seam khi tile
            float EdgeFade(float2 fracUV, float edgeW) {
                float ex = smoothstep(0.0, edgeW, fracUV.x) * smoothstep(0.0, edgeW, 1.0 - fracUV.x);
                float ey = smoothstep(0.0, edgeW, fracUV.y) * smoothstep(0.0, edgeW, 1.0 - fracUV.y);
                return ex * ey;
            }

            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y;

                // --- Lop 1: may chinh ---
                float2 raw1  = i.uv + float2(_SpeedX * t, _SpeedY * t);
                float2 uv1   = frac(raw1);
                fixed3 col1  = tex2D(_MainTex, uv1).rgb;
                fixed3 cld1  = (1.0 - col1) * _Color.rgb * _Opacity1 * EdgeFade(uv1, 0.12);

                // --- Lop 2: may nen ---
                float2 raw2  = (i.uv / _Scale2) + float2(_Offset2 + _Speed2X * t, _Speed2Y * t);
                float2 uv2   = frac(raw2);
                fixed3 col2  = tex2D(_MainTex, uv2).rgb;
                fixed3 cld2  = (1.0 - col2) * _Color.rgb * _Opacity2 * EdgeFade(uv2, 0.12);

                fixed3 result = (cld1 + cld2) * _Intensity;

                return fixed4(result, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
