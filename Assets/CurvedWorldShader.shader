Shader "Custom/CurvedWorldGround"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Curvature ("Curvature", Float) = 0.002
        _CurveStartDistance ("Curve Start Distance", Float) = 0
        _CurveAxisStrength ("X Axis Strength", Float) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Cull Back
        ZWrite On

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _Curvature;
            float _CurveStartDistance;
            float _CurveAxisStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 ApplyCurvedWorld(float3 worldPos)
            {
                float3 camPos = _WorldSpaceCameraPos;

                float zDistance = worldPos.z - camPos.z;
                float xDistance = worldPos.x - camPos.x;

                float curveZ = max(0, abs(zDistance) - _CurveStartDistance);
                float curveX = abs(xDistance) * _CurveAxisStrength;

                float curveAmount = (curveZ * curveZ + curveX * curveX) * _Curvature;

                worldPos.y -= curveAmount;

                return worldPos;
            }

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                worldPos = ApplyCurvedWorld(worldPos);

                o.pos = UnityWorldToClipPos(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * _Color;
            }

            ENDCG
        }
    }
}