Shader "Custom/CurvedWorldHorizonTransparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _HorizontalCurvature ("Horizontal Curvature", Float) = 0.002
        _ForwardCurvature ("Forward Curvature", Float) = 0.0003

        _CurveStartDistance ("Curve Start Distance", Float) = 0
        _HorizontalStartDistance ("Horizontal Start Distance", Float) = 0

        _CurveOffsetY ("Curve Offset Y", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _HorizontalCurvature;
            float _ForwardCurvature;

            float _CurveStartDistance;
            float _HorizontalStartDistance;

            float _CurveOffsetY;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            float3 ApplyCurvedWorld(float3 worldPos)
            {
                float3 camPos = _WorldSpaceCameraPos;

                float xDistance = abs(worldPos.x - camPos.x);
                float zDistance = abs(worldPos.z - camPos.z);

                xDistance = max(0, xDistance - _HorizontalStartDistance);
                zDistance = max(0, zDistance - _CurveStartDistance);

                float horizontalCurve = xDistance * xDistance * _HorizontalCurvature;
                float forwardCurve = zDistance * zDistance * _ForwardCurvature;

                worldPos.y -= horizontalCurve;
                worldPos.y -= forwardCurve;
                worldPos.y += _CurveOffsetY;

                return worldPos;
            }

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                worldPos = ApplyCurvedWorld(worldPos);

                o.pos = UnityWorldToClipPos(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * i.color;
            }

            ENDCG
        }
    }
}