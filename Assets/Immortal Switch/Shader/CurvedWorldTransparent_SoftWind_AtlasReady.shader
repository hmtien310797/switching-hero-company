Shader "Custom/CurvedWorldHorizonTransparentSoftWindVegetation"
{
    Properties
    {
        [Header(Base Layer)]
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Base Tint", Color) = (1,1,1,1)

        [Header(Vegetation Overlay)]
        [Toggle] _UseVegetation ("Use Vegetation", Float) = 0
        _VegetationTex ("Tree Grass Texture", 2D) = "black" {}
        _VegetationColor ("Vegetation Tint", Color) = (1,1,1,1)
        _VegetationOpacity ("Vegetation Opacity", Range(0,1)) = 1
        _VegetationScale ("Vegetation Scale XY", Vector) = (1,1,0,0)
        _VegetationOffset ("Vegetation Offset XY", Vector) = (0,0,0,0)

        [Header(Vegetation UV Wind)]
        [Toggle] _UseVegetationWind ("Use Vegetation Wind", Float) = 1
        _VegetationWindStrength ("Vegetation Wind Strength", Range(0,0.1)) = 0.01
        _VegetationWindSpeed ("Vegetation Wind Speed", Range(0,10)) = 1
        _VegetationWindFrequency ("Vegetation Wind Frequency", Range(0,20)) = 4
        _VegetationBendStart ("Vegetation Bend Start", Range(0,1)) = 0.05
        _VegetationBendPower ("Vegetation Bend Power", Range(0.1,8)) = 2

        [Header(Curved World)]
        _HorizontalCurvature ("Horizontal Curvature", Float) = 0.002
        _ForwardCurvature ("Forward Curvature", Float) = 0.0003
        _CurveStartDistance ("Curve Start Distance", Float) = 0
        _HorizontalStartDistance ("Horizontal Start Distance", Float) = 0
        _CurveOffsetY ("Curve Offset Y", Float) = 0

        [Header(Mesh Soft Wind)]
        [Toggle] _UseWind ("Use Mesh Wind", Float) = 0
        _WindStrength ("Wind Strength", Range(0, 0.5)) = 0.025
        _WindSpeed ("Wind Speed", Range(0, 10)) = 1.0
        _WindFrequency ("Wind Frequency", Range(0, 10)) = 1.2
        _WindDetailStrength ("Wind Detail Strength", Range(0, 0.2)) = 0.006
        _WindDetailSpeed ("Wind Detail Speed", Range(0, 10)) = 2.1

        [Header(Mesh Wind Bend Mask)]
        _BendStart ("Bend Start", Range(0, 1)) = 0.05
        _BendPower ("Bend Power", Range(0.1, 8)) = 2.0
        _WindPhaseOffset ("Wind Phase Offset", Float) = 0

        [Header(Transparency)]
        [Toggle] _UseAlphaClip ("Use Alpha Clip", Float) = 0
        _AlphaClip ("Alpha Clip", Range(0, 1)) = 0.01
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

            sampler2D _VegetationTex;
            fixed4 _VegetationColor;
            float _UseVegetation;
            float _VegetationOpacity;
            float4 _VegetationScale;
            float4 _VegetationOffset;

            float _UseVegetationWind;
            float _VegetationWindStrength;
            float _VegetationWindSpeed;
            float _VegetationWindFrequency;
            float _VegetationBendStart;
            float _VegetationBendPower;

            float _HorizontalCurvature;
            float _ForwardCurvature;
            float _CurveStartDistance;
            float _HorizontalStartDistance;
            float _CurveOffsetY;

            float _UseWind;
            float _WindStrength;
            float _WindSpeed;
            float _WindFrequency;
            float _WindDetailStrength;
            float _WindDetailSpeed;
            float _BendStart;
            float _BendPower;
            float _WindPhaseOffset;

            float _UseAlphaClip;
            float _AlphaClip;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;          // UV atlas để sample sprite
                float2 localUV : TEXCOORD1;     // UV local 0..1 để làm mask rung
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 rawUV : TEXCOORD1;
                fixed4 color : COLOR;
            };

            float3 ApplySoftWind(float3 worldPos, float2 rawUV)
            {
                if (_UseWind < 0.5)
                {
                    return worldPos;
                }

                float bendMask = saturate(
                    (rawUV.y - _BendStart) /
                    max(1.0 - _BendStart, 0.0001)
                );

                bendMask = pow(bendMask, _BendPower);

                float objectPhase =
                    unity_ObjectToWorld._m03 * 0.71 +
                    unity_ObjectToWorld._m23 * 0.37 +
                    _WindPhaseOffset;

                float mainWind = sin(
                    _Time.y * _WindSpeed +
                    worldPos.x * _WindFrequency +
                    worldPos.z * (_WindFrequency * 0.65) +
                    objectPhase
                );

                float detailWind = sin(
                    _Time.y * _WindDetailSpeed +
                    worldPos.x * 2.17 +
                    worldPos.z * 1.31 +
                    objectPhase * 1.73 +
                    rawUV.y * 3.0
                );

                float sway =
                    mainWind * _WindStrength +
                    detailWind * _WindDetailStrength;

                worldPos.x += sway * bendMask;
                return worldPos;
            }

            float3 ApplyCurvedWorld(float3 worldPos)
            {
                float3 camPos = _WorldSpaceCameraPos;

                float xDistance = abs(worldPos.x - camPos.x);
                float zDistance = abs(worldPos.z - camPos.z);

                xDistance = max(0, xDistance - _HorizontalStartDistance);
                zDistance = max(0, zDistance - _CurveStartDistance);

                worldPos.y -= xDistance * xDistance * _HorizontalCurvature;
                worldPos.y -= zDistance * zDistance * _ForwardCurvature;
                worldPos.y += _CurveOffsetY;

                return worldPos;
            }

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                worldPos = ApplySoftWind(worldPos, v.localUV);
                worldPos = ApplyCurvedWorld(worldPos);

                o.pos = UnityWorldToClipPos(worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.rawUV = v.localUV;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 AlphaOver(fixed4 background, fixed4 foreground)
            {
                fixed outAlpha = foreground.a + background.a * (1.0 - foreground.a);
                fixed3 outRgb = foreground.rgb * foreground.a +
                                background.rgb * background.a * (1.0 - foreground.a);

                outRgb /= max(outAlpha, 0.0001);
                return fixed4(outRgb, outAlpha);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = tex2D(_MainTex, i.uv) * i.color;
                fixed4 finalColor = baseColor;

                if (_UseVegetation > 0.5)
                {
                    // Scale quanh tâm UV rồi mới offset để dễ bố trí sprite.
                    float2 vegetationUV =
                        (i.rawUV - 0.5) / max(_VegetationScale.xy, float2(0.0001, 0.0001)) +
                        0.5 - _VegetationOffset.xy;

                    if (_UseVegetationWind > 0.5)
                    {
                        float bendMask = saturate(
                            (vegetationUV.y - _VegetationBendStart) /
                            max(1.0 - _VegetationBendStart, 0.0001)
                        );

                        bendMask = pow(bendMask, _VegetationBendPower);

                        float wind = sin(
                            _Time.y * _VegetationWindSpeed +
                            vegetationUV.y * _VegetationWindFrequency +
                            unity_ObjectToWorld._m03 * 0.73
                        );

                        vegetationUV.x -= wind * _VegetationWindStrength * bendMask;
                    }

                    // Ngoài vùng 0..1 thì không render vegetation.
                    float inside =
                        step(0.0, vegetationUV.x) *
                        step(vegetationUV.x, 1.0) *
                        step(0.0, vegetationUV.y) *
                        step(vegetationUV.y, 1.0);

                    fixed4 vegetation = tex2D(_VegetationTex, vegetationUV);
                    vegetation *= _VegetationColor;
                    vegetation.a *= _VegetationOpacity * inside;

                    finalColor = AlphaOver(baseColor, vegetation);
                }

                if (_UseAlphaClip > 0.5)
                {
                    clip(finalColor.a - _AlphaClip);
                }

                return finalColor;
            }

            ENDCG
        }
    }
}
