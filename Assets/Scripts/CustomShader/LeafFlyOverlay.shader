Shader "Custom/LeafFlyOverlay"
{
    Properties
    {
        _MainTex    ("Leaf Texture (black bg)", 2D)             = "black" {}
        _Color      ("Tint",                    Color)          = (1, 1, 1, 1)

        // So luong la bay (toi da 12)
        _LeafCount  ("Leaf Count",              Range(1, 12))   = 8

        // Kich thuoc moi la tren man hinh (UV space)
        _LeafSize   ("Leaf Size",               Range(0.02, 0.4)) = 0.18


        _SpeedBase  ("Base Speed",              Float)          = 0.06
        _SpeedVar   ("Speed Variation",         Float)          = 0.04
        _SwayAmt    ("Sway Amount",             Range(0, 0.05)) = 0.018

        _Intensity  ("Intensity",               Range(0, 3))    = 1.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            float     _LeafCount;
            float     _LeafSize;
            float     _TexSection;
            float     _SpeedBase;
            float     _SpeedVar;
            float     _SwayAmt;
            float     _Intensity;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            float Hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Dong gop cua 1 la vao pixel hien tai
            fixed3 LeafContrib(float2 uv, float id, float t) {
                float2 seed   = float2(id * 1.618, id * 2.414);

                float startX  = Hash(seed);
                float startY  = Hash(seed + float2(1.1, 0));
                float speed   = _SpeedBase + Hash(seed + float2(2.2, 0)) * _SpeedVar;
                float tOff    = Hash(seed + float2(3.3, 0)) * 20.0;
                float swayPh  = Hash(seed + float2(4.4, 0)) * 6.2832;
                float swaySpd = 0.6 + Hash(seed + float2(5.5, 0));

                float tt = t + tOff;
                float px = frac(startX + speed * tt + sin(tt * swaySpd + swayPh) * _SwayAmt);
                float py = frac(startY + speed * 0.45 * tt);

                // Toa do pixel tuong doi so voi tam la nay
                float2 local = (uv - float2(px, py)) / _LeafSize;

                // Mask: chi hien trong vung vuong cua la
                float inB = step(abs(local.x), 0.5) * step(abs(local.y), 0.5);

                // Texture chi co 1 la → sample toan bo texture
                float2 texUV = clamp(local + 0.5, 0.0, 1.0);

                // Dung alpha cua texture de mask (bo qua vung trong suot)
                fixed4 s = tex2Dlod(_MainTex, float4(texUV, 0, 0));
                return s.rgb * s.a * inB;
            }

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float  t   = _Time.y;
                float2 uv  = i.uv;

                fixed3 result = fixed3(0, 0, 0);

                // 12 la, dung bien step de tat/bat theo _LeafCount
                result += LeafContrib(uv,  0.0, t) * step(1.0,  _LeafCount);
                result += LeafContrib(uv,  1.0, t) * step(2.0,  _LeafCount);
                result += LeafContrib(uv,  2.0, t) * step(3.0,  _LeafCount);
                result += LeafContrib(uv,  3.0, t) * step(4.0,  _LeafCount);
                result += LeafContrib(uv,  4.0, t) * step(5.0,  _LeafCount);
                result += LeafContrib(uv,  5.0, t) * step(6.0,  _LeafCount);
                result += LeafContrib(uv,  6.0, t) * step(7.0,  _LeafCount);
                result += LeafContrib(uv,  7.0, t) * step(8.0,  _LeafCount);
                result += LeafContrib(uv,  8.0, t) * step(9.0,  _LeafCount);
                result += LeafContrib(uv,  9.0, t) * step(10.0, _LeafCount);
                result += LeafContrib(uv, 10.0, t) * step(11.0, _LeafCount);
                result += LeafContrib(uv, 11.0, t) * step(12.0, _LeafCount);

                return fixed4(result * _Color.rgb * _Intensity, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
