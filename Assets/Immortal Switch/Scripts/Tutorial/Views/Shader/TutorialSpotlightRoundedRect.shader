Shader "UI/TutorialSpotlightRoundedRect"
{
    Properties
    {
        _OverlayColor ("Overlay Color", Color) = (0, 0, 0, 1)
        _DimAlpha ("Dim Alpha", Range(0, 1)) = 0.6
        _HoleAlpha ("Hole Alpha", Range(0, 1)) = 0

        _HoleCenter ("Hole Center", Vector) = (0.5, 0.5, 0, 0)
        _HoleSize ("Hole Size", Vector) = (0.25, 0.15, 0, 0)

        _CanvasSize ("Canvas Size", Vector) = (1080, 1920, 0, 0)
        _CornerRadius ("Corner Radius PX", Float) = 24
        _Softness ("Softness PX", Float) = 8
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _OverlayColor;
            float _DimAlpha;
            float _HoleAlpha;

            float2 _HoleCenter;
            float2 _HoleSize;
            float2 _CanvasSize;

            float _CornerRadius;
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float RoundedRectSDF(float2 p, float2 halfSize, float radius)
            {
                radius = min(radius, min(halfSize.x, halfSize.y));

                float2 q = abs(p) - (halfSize - radius);

                return length(max(q, 0.0))
                    + min(max(q.x, q.y), 0.0)
                    - radius;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 pixelPos = i.uv * _CanvasSize;

                float2 holeCenterPx = _HoleCenter * _CanvasSize;
                float2 holeSizePx = _HoleSize * _CanvasSize;

                float2 p = pixelPos - holeCenterPx;
                float2 halfSize = holeSizePx * 0.5;

                float radius = min(_CornerRadius, min(halfSize.x, halfSize.y));
                float softness = max(_Softness, 0.0001);

                float dist = RoundedRectSDF(p, halfSize, radius);

                // trong hole = 0, ngoài hole = 1
                float mask = smoothstep(0.0, softness, dist);

                fixed4 col = _OverlayColor * i.color;
                col.a = lerp(_HoleAlpha, _DimAlpha, mask);

                return col;
            }
            ENDCG
        }
    }
}