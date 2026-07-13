Shader "Custom/UI/FireFlipbook"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Sprite", 2D) = "white" {}
        _FireTex ("Fire Flipbook", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Flipbook)]
        _Columns ("Columns", Float) = 5
        _Rows ("Rows", Float) = 2
        _FrameCount ("Frame Count", Float) = 10
        _FPS ("FPS", Float) = 12

        [Header(Color)]
        _FireLowColor ("Fire Low Color", Color) = (1,0.25,0.05,1)
        _FireHighColor ("Fire High Color", Color) = (1,0.95,0.75,1)
        _Intensity ("Intensity", Range(0,10)) = 1.5

        [Header(Time)]
        [Toggle] _UseSharedTime ("Use Shared Time", Float) = 1
        [HideInInspector] _SharedFXTime ("Shared FX Time", Float) = 0

        [Header(Distortion)]
        [Toggle] _UseDistortion ("Use Distortion", Float) = 0
        _DistortionTex ("Distortion Noise", 2D) = "gray" {}
        _DistortionStrength ("Distortion Strength", Range(0,0.1)) = 0.01
        _DistortionSpeed ("Distortion Speed", Range(-5,5)) = 0.2

        [Header(Mask)]
        [Toggle] _MaskByMainTexAlpha ("Mask By MainTex Alpha", Float) = 0

        [Header(UI)]
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
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _FireTex;
            sampler2D _DistortionTex;

            float4 _MainTex_ST;
            float4 _FireTex_ST;

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _Columns;
            float _Rows;
            float _FrameCount;
            float _FPS;

            fixed4 _FireLowColor;
            fixed4 _FireHighColor;
            float _Intensity;

            float _UseSharedTime;
            float _SharedFXTime;

            float _UseDistortion;
            float _DistortionStrength;
            float _DistortionSpeed;

            float _MaskByMainTexAlpha;

            v2f vert(appdata_t v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;

                return o;
            }

            float2 GetFlipbookUV(float2 baseUV, float frameIndex)
            {
                float safeColumns = max(_Columns, 1.0);
                float safeRows = max(_Rows, 1.0);

                float2 cellSize = float2(1.0 / safeColumns, 1.0 / safeRows);

                float col = fmod(frameIndex, safeColumns);
                float rowFromTop = floor(frameIndex / safeColumns);
                float row = (safeRows - 1.0) - rowFromTop;

                float2 uv = baseUV * cellSize + float2(col, row) * cellSize;
                return uv;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 maskTex = tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;

                float timeValue = lerp(_Time.y, _SharedFXTime, saturate(_UseSharedTime));

                float safeFrameCount = max(_FrameCount, 1.0);
                float frameFloat = floor(timeValue * _FPS);
                float frameIndex = fmod(frameFloat, safeFrameCount);

                float2 baseUV = i.texcoord;

                if (_UseDistortion > 0.5)
                {
                    float2 noiseUV = baseUV * 1.5;
                    noiseUV.y += timeValue * _DistortionSpeed;

                    float distortion = tex2D(_DistortionTex, noiseUV).r - 0.5;
                    baseUV.x += distortion * _DistortionStrength;
                }

                float2 flipbookUV = GetFlipbookUV(baseUV, frameIndex);
                fixed4 fireTex = tex2D(_FireTex, flipbookUV);

                float fireGradient = saturate(i.texcoord.y);
                fixed3 fireTint = lerp(_FireLowColor.rgb, _FireHighColor.rgb, fireGradient);

                float maskAlpha = lerp(1.0, maskTex.a, saturate(_MaskByMainTexAlpha));

                fixed4 col;
                col.rgb = fireTex.rgb * fireTint * _Intensity * fireTex.a * maskAlpha * i.color.rgb;
                col.a = fireTex.a * maskAlpha * i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                col.rgb *= col.a;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}