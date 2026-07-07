Shader "Custom/StarrySkyUI"
{
    Properties
    {
        _MainTex       ("Background Texture",  2D)            = "white" {}
        _Color         ("Tint",                Color)         = (1,1,1,1)
        _GridSize      ("Grid Density",        Float)         = 45.0
        _StarThreshold ("Star Threshold",      Range(0,1))    = 0.78
        _StarSize      ("Star Size",           Float)         = 14.0
        _StarBright    ("Star Brightness",     Float)         = 2.5
        _TwinkleSpeed  ("Twinkle Speed",       Float)         = 2.0
        _TwinkleMin    ("Twinkle Min",         Range(0,1))    = 0.05
        _StarColorWarm ("Warm Star Color",     Color)         = (1.0, 0.95, 0.8,  1)
        _StarColorCool ("Cool Star Color",     Color)         = (0.75, 0.85, 1.0, 1)
        _StarColorRare ("Rare Star Color",     Color)         = (1.0,  0.6,  0.9, 1)
        _Aspect        ("Aspect Ratio (W/H)",  Float)         = 0.5
        _StarRegionY   ("Star Region Bottom Y",Range(0,1))   = 0.0
        _StarFade      ("Star Fade Width",     Range(0,0.3))  = 0.10
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
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

            float  _GridSize;
            float  _StarThreshold;
            float  _StarSize;
            float  _StarBright;
            float  _TwinkleSpeed;
            float  _TwinkleMin;
            float4 _StarColorWarm;
            float4 _StarColorCool;
            float4 _StarColorRare;
            float  _Aspect;
            float  _StarRegionY;
            float  _StarFade;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            // Ham hash ngau nhien tu toa do o luoi
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 Hash2(float2 p)
            {
                float2 q = float2(dot(p, float2(127.1, 311.7)),
                                  dot(p, float2(269.5, 183.3)));
                return frac(sin(q) * 43758.5453);
            }

            // Mau sao theo gia tri ngau nhien
            float3 StarColor(float cv)
            {
                if (cv < 0.5)
                    return lerp(_StarColorWarm.rgb, _StarColorCool.rgb, cv * 2.0);
                else
                    return lerp(_StarColorCool.rgb, _StarColorRare.rgb, (cv - 0.5) * 2.0);
            }

            // Dong gop sang cua 1 o luoi vao pixel hien tai
            float3 StarCell(float2 cell, float2 uv, float t)
            {
                float hasStar = step(_StarThreshold, Hash(cell + float2(3.7, 3.7)));

                // Vi tri sao trong UV space
                float2 starUV = (cell + Hash2(cell) * 0.7 + 0.15) / _GridSize;

                // Khoang cach co hieu chinh aspect
                float2 d    = (uv - starUV) * float2(_Aspect, 1.0) * _GridSize;
                float  dist = length(d);

                // Nhap nhay
                float phase = Hash(cell * 3.17) * 6.2832;
                float spd   = Hash(cell * 5.73) * 2.0 + 0.5;
                float tw    = lerp(_TwinkleMin, 1.0,
                              sin(t * _TwinkleSpeed * spd + phase) * 0.5 + 0.5);

                float glow  = exp(-dist * _StarSize) * tw * _StarBright * hasStar;
                return StarColor(Hash(cell + float2(5.0, 5.0))) * glow;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float  t  = _Time.y;

                // Anh nen
                fixed4 col = tex2D(_MainTex, uv);

                // Mask: sao chi hien tu StarRegionY tro len
                float mask = smoothstep(_StarRegionY, _StarRegionY + _StarFade, uv.y);

                // Kiem tra 3x3 o luoi xung quanh pixel
                float3 stars    = float3(0, 0, 0);
                float2 baseCell = floor(uv * _GridSize);

                stars += StarCell(baseCell + float2(-1,-1), uv, t);
                stars += StarCell(baseCell + float2( 0,-1), uv, t);
                stars += StarCell(baseCell + float2( 1,-1), uv, t);
                stars += StarCell(baseCell + float2(-1, 0), uv, t);
                stars += StarCell(baseCell + float2( 0, 0), uv, t);
                stars += StarCell(baseCell + float2( 1, 0), uv, t);
                stars += StarCell(baseCell + float2(-1, 1), uv, t);
                stars += StarCell(baseCell + float2( 0, 1), uv, t);
                stars += StarCell(baseCell + float2( 1, 1), uv, t);

                col.rgb += stars * mask;
                return col * _Color;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
