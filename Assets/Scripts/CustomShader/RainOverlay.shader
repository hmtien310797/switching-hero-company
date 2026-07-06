Shader "Custom/RainOverlay"
{
    Properties
    {
        _MainTex     ("Rain Texture (white bg)", 2D)           = "white" {}
        _Color       ("Rain Color",               Color)        = (0.75, 0.85, 1.0, 1.0)

        // Lop mua 1 - gan, to, nhanh
        _SpeedX      ("Layer 1 Speed X",          Float)        = -0.12
        _SpeedY      ("Layer 1 Speed Y",          Float)        = 0.65
        _Opacity1    ("Layer 1 Opacity",          Range(0, 1))  = 1.0

        // Lop mua 2 - xa, nho, cham
        _Speed2X     ("Layer 2 Speed X",          Float)        = -0.06
        _Speed2Y     ("Layer 2 Speed Y",          Float)        = 0.38
        _Offset2     ("Layer 2 UV Offset X",      Range(0, 1))  = 0.37
        _Scale2      ("Layer 2 Scale",            Range(0.5, 2)) = 1.4
        _Opacity2    ("Layer 2 Opacity",          Range(0, 1))  = 0.5

        _Intensity   ("Overall Intensity",        Range(0, 2))  = 1.0

        // Neu mua roi nguoc: bat cai nay len
        [Toggle] _FlipY ("Flip Rain Direction (Y)",  Float)        = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        // Additive: vung trang (sau khi invert = den) + 0 = trong suot hoan toan
        // Vach mua (sau khi invert = sang) + nen = sang dep
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
            float _FlipY;

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

            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y;

                // Pulse nhe: mua co luc day luc nhat
                float pulse = 0.88 + 0.12 * sin(t * 1.5);

                float dirY = _FlipY > 0.5 ? -1.0 : 1.0;

                // --- Lop 1: mua chinh ---
                float2 uv1   = frac(i.uv + float2(_SpeedX * t, dirY * _SpeedY * t));
                fixed3 col1  = tex2D(_MainTex, uv1).rgb;
                // Invert: trang->den (vo hieu), mau->sang (hien thi)
                fixed3 rain1 = (1.0 - col1) * _Color.rgb * _Opacity1;

                // --- Lop 2: mua nen ---
                float2 uv2   = frac((i.uv / _Scale2) + float2(_Offset2 + _Speed2X * t, dirY * _Speed2Y * t));
                fixed3 col2  = tex2D(_MainTex, uv2).rgb;
                fixed3 rain2 = (1.0 - col2) * _Color.rgb * _Opacity2;

                fixed3 result = (rain1 + rain2) * pulse * _Intensity;

                return fixed4(result, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
