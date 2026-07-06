Shader "Custom/ChapterStageBg4"
{
    Properties
    {
        _MainTex   ("Sky Background", 2D)    = "white" {}
        _Color     ("Tint",           Color) = (1, 1, 1, 1)
        _SpeedX    ("Cloud Speed X",  Float) = 0.012
        _SpeedY    ("Cloud Speed Y",  Float) = 0.0

        [Header(God Rays)]
        _SunX         ("Sun Position X",    Range(0, 1))    = 0.5
        _SunY         ("Sun Position Y",    Range(0, 1))    = 0.82
        _RayColor     ("Ray Color",         Color)          = (1.0, 0.88, 0.55, 1)
        _RayIntensity ("Ray Intensity",     Range(0, 1))    = 0.28
        _RayCount     ("Ray Count",         Range(3, 20))   = 10.0
        _RaySpeed     ("Ray Rotate Speed",  Float)          = 0.06
        _RayFalloff   ("Ray Falloff",       Range(0.5, 10)) = 2.8
        _RayAspect    ("Aspect W/H",        Float)          = 0.56

        [Header(Floating Dust)]
        _DustColor    ("Dust Color",        Color)          = (1.0, 0.92, 0.62, 1)
        _DustDensity  ("Dust Density",      Float)          = 38.0
        _DustSize     ("Dust Size",         Float)          = 22.0
        _DustSpeed    ("Dust Rise Speed",   Float)          = 0.025
        _DustBright   ("Dust Brightness",   Float)          = 1.3
        _DustMaxY     ("Dust Max Y",        Range(0, 1))    = 0.62
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
            float     _SpeedX;
            float     _SpeedY;

            float     _SunX;
            float     _SunY;
            fixed4    _RayColor;
            float     _RayIntensity;
            float     _RayCount;
            float     _RaySpeed;
            float     _RayFalloff;
            float     _RayAspect;

            fixed4    _DustColor;
            float     _DustDensity;
            float     _DustSize;
            float     _DustSpeed;
            float     _DustBright;
            float     _DustMaxY;

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

            float2 Hash2(float2 p) {
                return float2(Hash(p), Hash(p + float2(31.7, 17.3)));
            }

            // Dong gop cua 1 hat bui trong cell luoi
            float3 DustCell(float2 cell, float2 uv, float t) {
                float2 h    = Hash2(cell);
                float  tOff = Hash(cell + float2(5.1, 3.7)) * 12.0;

                float2 base = (cell + h * 0.8 + 0.1) / _DustDensity;
                float  py   = frac(base.y - (t + tOff) * _DustSpeed);
                float2 pos  = float2(base.x, py);

                float2 d       = (uv - pos) * float2(_RayAspect, 1.0) * _DustDensity;
                float  dist    = length(d);
                float  twinkle = sin(t * 1.8 + h.x * 6.2832) * 0.3 + 0.7;
                float  glow    = exp(-dist * _DustSize) * twinkle * _DustBright;

                // Chi hien trong vung may phia duoi
                float  mask = 1.0 - smoothstep(_DustMaxY - 0.08, _DustMaxY, uv.y);
                return _DustColor.rgb * glow * mask;
            }

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float  t  = _Time.y;
                float2 uv = i.uv;

                // --- Nen troi (dung yen) ---
                fixed4 col = tex2D(_MainTex, uv) * _Color;

                // --- God Rays ---
                float2 toSun = uv - float2(_SunX, _SunY);
                toSun.x     *= _RayAspect;
                float  sunDist = length(toSun);
                float  angle   = atan2(toSun.y, toSun.x);

                float  rayPat  = sin(angle * _RayCount + t * _RaySpeed);
                       rayPat  = pow(max(0.0, rayPat), 2.5);
                float  falloff = exp(-sunDist * _RayFalloff);
                col.rgb       += _RayColor.rgb * rayPat * falloff * _RayIntensity;

                // --- Floating Dust (3x3 cell check) ---
                float3 dust     = float3(0, 0, 0);
                float2 baseCell = floor(uv * _DustDensity);

                dust += DustCell(baseCell + float2(-1,-1), uv, t);
                dust += DustCell(baseCell + float2( 0,-1), uv, t);
                dust += DustCell(baseCell + float2( 1,-1), uv, t);
                dust += DustCell(baseCell + float2(-1, 0), uv, t);
                dust += DustCell(baseCell + float2( 0, 0), uv, t);
                dust += DustCell(baseCell + float2( 1, 0), uv, t);
                dust += DustCell(baseCell + float2(-1, 1), uv, t);
                dust += DustCell(baseCell + float2( 0, 1), uv, t);
                dust += DustCell(baseCell + float2( 1, 1), uv, t);

                col.rgb += dust;

                return col;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
