Shader "Custom/UI/ItemTierFX1_Complete_NormalizedUV"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Default is correct for a standalone sprite.
        // When the sprite is packed into an atlas, set this from UIImageShaderUVBinder.
        [HideInInspector] _SpriteUVRect ("Sprite UV Rect", Vector) = (0,0,1,1)

        [Header(Gradient)]
        _BottomColor ("Bottom Color", Color) = (1,0.25,0.02,1)
        _TopColor ("Top Color", Color) = (1,0.75,0.08,1)
        _GradientStrength ("Gradient Strength", Range(0,1)) = 0
        _GradientPower ("Gradient Power", Range(0.1,8)) = 1

        [Header(Flow Shine)]
        [Toggle] _UseShine ("Use Shine", Float) = 1
        _ShineColor ("Shine Color", Color) = (1,0.8,0.25,1)
        _ShineIntensity ("Shine Intensity", Range(0,8)) = 1.5
        _ShineWidth ("Shine Width", Range(0.01,0.5)) = 0.12
        _ShineSoftness ("Shine Softness", Range(0.001,0.3)) = 0.06
        _ShineAngle ("Shine Angle", Range(-180,180)) = 25
        _ShineSpeed ("Shine Speed", Range(-5,5)) = 0.7
        _ShineRepeat ("Shine Repeat", Range(0.25,5)) = 1
        _ShinePhase ("Shine Phase", Range(0,1)) = 0

        [Header(Pulse Glow)]
        [Toggle] _UsePulse ("Use Pulse", Float) = 1
        _PulseColor ("Pulse Color", Color) = (1,0.25,0.02,1)
        _PulseIntensity ("Pulse Intensity", Range(0,8)) = 0.8
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 2
        _PulseMin ("Pulse Minimum", Range(0,1)) = 0.15

        [Header(Edge Glow)]
        [Toggle] _UseEdgeGlow ("Use Edge Glow", Float) = 0
        _EdgeColor ("Edge Color", Color) = (1,0.35,0.02,1)
        _EdgeIntensity ("Edge Intensity", Range(0,8)) = 1
        _EdgeWidth ("Edge Width", Range(0.25,6)) = 1.5

        [Header(Sprite Sparkle)]
        [Toggle] _UseSparkle ("Use Sparkle", Float) = 0
        _SparkleTex ("Sparkle Sprite", 2D) = "white" {}
        _SparkleColor ("Sparkle Color", Color) = (1,0.9,0.45,1)
        _SparkleIntensity ("Sparkle Intensity", Range(0,12)) = 2
        _SparkleScale ("Sparkle Grid Scale", Range(1,30)) = 6
        _SparkleSize ("Sparkle Size", Range(0.05,1)) = 0.45
        _SparkleSpeed ("Sparkle Speed", Range(0,10)) = 2
        _SparkleThreshold ("Spawn Threshold", Range(0,0.99)) = 0.72
        _SparkleRotationSpeed ("Rotation Speed", Range(-5,5)) = 0.3
        [Toggle] _SparkleMaskByAlpha ("Mask By Sprite Alpha", Float) = 1

        [Header(Pixel Squares)]
        [Toggle] _UsePixelSquares ("Use Pixel Squares", Float) = 0
        _PixelSquareColor ("Pixel Square Color", Color) = (1,0.65,0.2,1)
        _PixelSquareIntensity ("Pixel Square Intensity", Range(0,5)) = 1.2
        _PixelGridX ("Pixel Grid X", Range(2,20)) = 7
        _PixelGridY ("Pixel Grid Y", Range(2,20)) = 7
        _PixelSquareSize ("Pixel Square Size", Range(0.1,0.95)) = 0.68
        _PixelAreaMinX ("Pixel Area Min X", Range(0,1)) = 0.55
        _PixelAreaMaxY ("Pixel Area Max Y", Range(0,1)) = 0.42
        _PixelRandomness ("Pixel Randomness", Range(0,1)) = 0.35
        _PixelCornerRadius ("Pixel Corner Radius", Range(0.05,1.5)) = 0.65
        _PixelRevealStrength ("Pixel Reveal Strength", Range(0,5)) = 1
        [Toggle] _PixelLinkToShine ("Link To Shine", Float) = 1
        _PixelBaseVisibility ("Base Visibility", Range(0,1)) = 0.04
        _PixelShineBoost ("Shine Reveal Boost", Range(0.1,10)) = 3
        [Toggle] _PixelDebugAlwaysOn ("Debug Always On", Float) = 0

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
        Blend SrcAlpha OneMinusSrcAlpha
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
                float2 effectUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 effectUV      : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            sampler2D _SparkleTex;

            fixed4 _Color;
            float4 _SpriteUVRect;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _BottomColor;
            fixed4 _TopColor;
            float _GradientStrength;
            float _GradientPower;

            float _UseShine;
            fixed4 _ShineColor;
            float _ShineIntensity;
            float _ShineWidth;
            float _ShineSoftness;
            float _ShineAngle;
            float _ShineSpeed;
            float _ShineRepeat;
            float _ShinePhase;

            float _UsePulse;
            fixed4 _PulseColor;
            float _PulseIntensity;
            float _PulseSpeed;
            float _PulseMin;

            float _UseEdgeGlow;
            fixed4 _EdgeColor;
            float _EdgeIntensity;
            float _EdgeWidth;

            float _UseSparkle;
            fixed4 _SparkleColor;
            float _SparkleIntensity;
            float _SparkleScale;
            float _SparkleSize;
            float _SparkleSpeed;
            float _SparkleThreshold;
            float _SparkleRotationSpeed;
            float _SparkleMaskByAlpha;

            float _UsePixelSquares;
            fixed4 _PixelSquareColor;
            float _PixelSquareIntensity;
            float _PixelGridX;
            float _PixelGridY;
            float _PixelSquareSize;
            float _PixelAreaMinX;
            float _PixelAreaMaxY;
            float _PixelRandomness;
            float _PixelCornerRadius;
            float _PixelRevealStrength;
            float _PixelLinkToShine;
            float _PixelBaseVisibility;
            float _PixelShineBoost;
            float _PixelDebugAlwaysOn;

            v2f vert(appdata_t v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.effectUV = v.effectUV;
                o.color = v.color * _Color;

                return o;
            }

            float2 GetLocalSpriteUV(float2 atlasUV)
            {
                float2 rectSize = max(
                    _SpriteUVRect.zw,
                    float2(0.00001, 0.00001)
                );

                return saturate(
                    (atlasUV - _SpriteUVRect.xy) / rectSize
                );
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 RotateAroundCenter(float2 uv, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);

                uv -= 0.5;

                float2 result;
                result.x = uv.x * c - uv.y * s;
                result.y = uv.x * s + uv.y * c;

                return result + 0.5;
            }

            float SampleSparkle(float2 uv, float time)
            {
                float2 gridUV = uv * _SparkleScale;
                float2 cell = floor(gridUV);
                float2 localUV = frac(gridUV);

                float randomA = Hash21(cell);
                float randomB = Hash21(cell + 17.71);

                float spawnMask = step(_SparkleThreshold, randomA);

                float phase = frac(time * 0.35 + randomB);
                float fadeIn = smoothstep(0.0, 0.18, phase);
                float fadeOut = 1.0 - smoothstep(0.18, 0.55, phase);
                float pulse = fadeIn * fadeOut;

                float animatedSize = max(_SparkleSize * lerp(0.65, 1.0, pulse), 0.001);

                float2 spriteUV = (localUV - 0.5) / animatedSize + 0.5;
                spriteUV = RotateAroundCenter(
                    spriteUV,
                    time * _SparkleRotationSpeed + randomA * 6.2831853
                );

                float inside =
                    step(0.0, spriteUV.x) *
                    step(spriteUV.x, 1.0) *
                    step(0.0, spriteUV.y) *
                    step(spriteUV.y, 1.0);

                fixed4 sparkleSample = tex2D(_SparkleTex, spriteUV);

                return sparkleSample.a * inside * spawnMask * pulse;
            }


            float GetPixelSquarePattern(float2 uv)
            {
                float2 gridCount = float2(
                    max(_PixelGridX, 1.0),
                    max(_PixelGridY, 1.0)
                );

                float2 gridUV = uv * gridCount;
                float2 cell = floor(gridUV);
                float2 localUV = frac(gridUV);

                float2 centerDistance = abs(localUV - 0.5) * 2.0;

                float square = 1.0 - step(
                    _PixelSquareSize,
                    max(centerDistance.x, centerDistance.y)
                );

                float randomValue = Hash21(cell + 9.17);
                float randomMask = lerp(
                    1.0,
                    randomValue,
                    saturate(_PixelRandomness)
                );

                float rightMask = smoothstep(
                    _PixelAreaMinX,
                    1.0,
                    uv.x
                );

                float bottomMask = 1.0 - smoothstep(
                    0.0,
                    max(_PixelAreaMaxY, 0.001),
                    uv.y
                );

                float distanceToCorner = length(
                    uv - float2(1.0, 0.0)
                );

                float cornerFade = 1.0 - smoothstep(
                    0.12,
                    max(_PixelCornerRadius, 0.121),
                    distanceToCorner
                );

                return square *
                       randomMask *
                       rightMask *
                       bottomMask *
                       cornerFade;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;
                fixed4 col = tex * i.color;

                float2 effectUV = saturate(i.effectUV);

                float gradientY = pow(saturate(effectUV.y), _GradientPower);
                fixed3 gradient = lerp(_BottomColor.rgb, _TopColor.rgb, gradientY);
                col.rgb = lerp(col.rgb, col.rgb * gradient, _GradientStrength);

                float angle = radians(_ShineAngle);
                float2 shineDirection = float2(cos(angle), sin(angle));
                float projected = dot(effectUV - 0.5, shineDirection);
                float travel = frac(
                    projected * _ShineRepeat -
                    _Time.y * _ShineSpeed +
                    _ShinePhase
                );

                float distanceToBand = abs(travel - 0.5);
                float shine = 1.0 - smoothstep(
                    _ShineWidth,
                    _ShineWidth + _ShineSoftness,
                    distanceToBand
                );

                shine *= _UseShine * tex.a;
                col.rgb += _ShineColor.rgb * shine * _ShineIntensity;

                float pulse = lerp(
                    _PulseMin,
                    1.0,
                    sin(_Time.y * _PulseSpeed) * 0.5 + 0.5
                );

                col.rgb +=
                    _PulseColor.rgb *
                    (_UsePulse * _PulseIntensity * pulse * tex.a);

                float2 pixelOffset = _MainTex_TexelSize.xy * _EdgeWidth;

                float alphaLeft = tex2D(
                    _MainTex,
                    i.texcoord + float2(-pixelOffset.x, 0)
                ).a;

                float alphaRight = tex2D(
                    _MainTex,
                    i.texcoord + float2(pixelOffset.x, 0)
                ).a;

                float alphaDown = tex2D(
                    _MainTex,
                    i.texcoord + float2(0, -pixelOffset.y)
                ).a;

                float alphaUp = tex2D(
                    _MainTex,
                    i.texcoord + float2(0, pixelOffset.y)
                ).a;

                float outerEdge = saturate(
                    max(
                        max(alphaLeft, alphaRight),
                        max(alphaDown, alphaUp)
                    ) - tex.a
                );

                col.rgb +=
                    _EdgeColor.rgb *
                    outerEdge *
                    _EdgeIntensity *
                    _UseEdgeGlow;

                col.a = max(
                    col.a,
                    outerEdge * _EdgeColor.a * _UseEdgeGlow
                );

                float sparkle = SampleSparkle(
                    effectUV,
                    _Time.y * _SparkleSpeed
                );

                float alphaMask = lerp(
                    1.0,
                    tex.a,
                    saturate(_SparkleMaskByAlpha)
                );

                sparkle *= alphaMask * _UseSparkle;

                col.rgb +=
                    _SparkleColor.rgb *
                    sparkle *
                    _SparkleIntensity;

                col.a = max(
                    col.a,
                    sparkle * _SparkleColor.a
                );


                float pixelSquares = GetPixelSquarePattern(effectUV);

                // `shine` already contains _UseShine and tex.a.
                // Boost it so a narrow shine band can still reveal the squares clearly.
                float shineReveal = saturate(
                    shine * _PixelShineBoost
                );

                float reveal = lerp(
                    1.0,
                    max(_PixelBaseVisibility, shineReveal),
                    saturate(_PixelLinkToShine)
                );

                reveal = lerp(
                    reveal,
                    1.0,
                    saturate(_PixelDebugAlwaysOn)
                );

                pixelSquares *=
                    reveal *
                    _UsePixelSquares *
                    _PixelRevealStrength;

                col.rgb +=
                    _PixelSquareColor.rgb *
                    pixelSquares *
                    _PixelSquareIntensity *
                    tex.a;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(
                    i.worldPosition.xy,
                    _ClipRect
                );
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
