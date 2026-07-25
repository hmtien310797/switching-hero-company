// Ho tro "Soft Maskable" cua package com.coffee.softmask-for-ugui: shader phai tu khai
// bao ho tro (khong tu dong hoat dong voi shader tuy bien). Da them theo dung huong dan
// chinh thuc cua package (cach "Hybrid" - sua truc tiep tren shader goc):
//   - Them hau to " (SoftMaskable)" vao ten shader.
//   - Include SoftMask.cginc + 2 dong #pragma shader_feature.
//   - Nhan alpha cuoi cung voi SoftMask(...) truoc khi return.
// Sau khi save, bat toggle "Soft Maskable" tren Image/RawImage dung material nay se hoat
// dong binh thuong. Neu Unity bao khong tim thay shader trong dropdown, chon lai shader
// "Custom/StarrySkyUI (SoftMaskable)" cho material starskyUI.mat (van la file shader nay,
// chi doi ten hien thi, khong anh huong reference vi Unity luu theo GUID).
Shader "Custom/StarrySkyUI (SoftMaskable)"
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
        _StarRegionTopY("Star Region Top Y",   Range(0,1))    = 1.0
        _StarFade      ("Star Fade Width",     Range(0,0.3))  = 0.10

        [Header(UI Mask Ho tro chuan Mask RectMask2D cua Unity UI)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            // Ho tro Soft Mask (bat buoc theo package com.coffee.softmask-for-ugui)
            #include "Packages/com.coffee.softmask-for-ugui/Shaders/SoftMask.cginc"
            #pragma shader_feature _ SOFTMASK_EDITOR
            #pragma shader_feature_local _ SOFTMASKABLE

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
            float  _StarRegionTopY;
            float  _StarFade;

            float4 _ClipRect;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
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
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float  t  = _Time.y;

                // Anh nen
                fixed4 col = tex2D(_MainTex, uv);

                // Mask: sao chi hien trong dai tu StarRegionY (duoi) den StarRegionTopY (tren)
                float mask = smoothstep(_StarRegionY, _StarRegionY + _StarFade, uv.y)
                           * (1.0 - smoothstep(_StarRegionTopY - _StarFade, _StarRegionTopY, uv.y));

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

                fixed4 finalColor = col * _Color;
                finalColor.a *= SoftMask(i.vertex, i.worldPosition, finalColor.a);

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
