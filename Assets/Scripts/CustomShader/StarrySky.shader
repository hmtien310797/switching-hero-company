Shader "Custom/StarrySky"
{
    Properties
    {
        _MainTex        ("Background Texture", 2D)         = "white" {}
        _Color          ("Tint",               Color)      = (1,1,1,1)
        _GridSize       ("Grid Density",       Float)      = 45.0
        _StarThreshold  ("Star Threshold",     Range(0,1)) = 0.78
        _StarSize       ("Star Size",          Float)      = 14.0
        _StarBright     ("Star Brightness",    Float)      = 2.5
        _TwinkleSpeed   ("Twinkle Speed",      Float)      = 2.0
        _TwinkleMin     ("Twinkle Min",        Range(0,1)) = 0.05
        _StarColorWarm  ("Warm Color",         Color)      = (1.0, 0.95, 0.8, 1)
        _StarColorCool  ("Cool Color",         Color)      = (0.75, 0.85, 1.0, 1)
        _StarColorRare  ("Rare Color",         Color)      = (1.0, 0.6,  0.9, 1)
        _Aspect         ("Aspect Ratio W/H",   Float)      = 0.5
        _StarRegionY    ("Star Region Bottom", Range(0,1)) = 0.67
        _StarFade       ("Star Fade Width",    Range(0,0.3)) = 0.10
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            float4 _Color;
            float  _GridSize, _StarThreshold, _StarSize, _StarBright;
            float  _TwinkleSpeed, _TwinkleMin;
            float4 _StarColorWarm, _StarColorCool, _StarColorRare;
            float  _Aspect, _StarRegionY, _StarFade;

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            float2 Hash2(float2 p)
            {
                float2 q = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(q) * 43758.5453);
            }
            float3 StarColor(float cv)
            {
                return cv < 0.5
                    ? lerp(_StarColorWarm.rgb, _StarColorCool.rgb, cv * 2.0)
                    : lerp(_StarColorCool.rgb, _StarColorRare.rgb, (cv - 0.5) * 2.0);
            }

            // Tính đóng góp của 1 cell vào pixel tại uv (small star)
            float3 SmallStarCell(float2 cell, float2 uv, float t)
            {
                float hasS = step(_StarThreshold, Hash(cell + float2(3.7, 3.7)));
                // Vị trí sao trong world UV
                float2 starUV = (cell + Hash2(cell) * 0.7 + 0.15) / _GridSize;
                // Khoảng cách thực trong world UV, có hiệu chỉnh aspect
                float2 d = (uv - starUV) * float2(_Aspect, 1.0) * _GridSize;
                float dist = length(d);
                float phase = Hash(cell * 3.17) * 6.2832;
                float speed = Hash(cell * 5.73) * 2.0 + 0.5;
                float tw = lerp(_TwinkleMin, 1.0, sin(t * _TwinkleSpeed * speed + phase) * 0.5 + 0.5);
                float glow = exp(-dist * _StarSize) * tw * _StarBright * hasS;
                return StarColor(Hash(cell + float2(5.0, 5.0))) * glow;
            }

Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv    = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Mask: sao chỉ hiện ở 1/3 trên (uv.y = 1 là đỉnh)
                float mask = smoothstep(_StarRegionY - _StarFade, _StarRegionY, uv.y);

                float3 stars = float3(0.0, 0.0, 0.0);

                // ── Small stars: check 3x3 cells xung quanh ───────────────
                float2 baseCell = floor(uv * _GridSize);
                stars += SmallStarCell(baseCell + float2(-1,-1), uv, t);
                stars += SmallStarCell(baseCell + float2( 0,-1), uv, t);
                stars += SmallStarCell(baseCell + float2( 1,-1), uv, t);
                stars += SmallStarCell(baseCell + float2(-1, 0), uv, t);
                stars += SmallStarCell(baseCell + float2( 0, 0), uv, t);
                stars += SmallStarCell(baseCell + float2( 1, 0), uv, t);
                stars += SmallStarCell(baseCell + float2(-1, 1), uv, t);
                stars += SmallStarCell(baseCell + float2( 0, 1), uv, t);
                stars += SmallStarCell(baseCell + float2( 1, 1), uv, t);

col.rgb += stars * mask;
                return col * IN.color;
            }
            ENDHLSL
        }
    }
}
