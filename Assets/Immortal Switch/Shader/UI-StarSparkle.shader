Shader "Custom/UI-StarSparkle" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)

		[Header(Star Sparkle)]
		_StarColor ("Star Color", Color) = (1, 1, 1, 1)
		_StarDensity ("Star Density (grid cells)", Range(4, 60)) = 20
		_StarSize ("Star Size", Range(0.02, 0.5)) = 0.12
		_StarBrightness ("Star Brightness", Range(0, 3)) = 1.2
		_TwinkleSpeed ("Twinkle Speed", Range(0, 10)) = 2.0

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
	}

	SubShader {
		Tags {
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Stencil {
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

		Pass {
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			struct appdata_t {
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex        : SV_POSITION;
				fixed4 color         : COLOR;
				float2 texcoord      : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			fixed4 _Color;
			fixed4 _StarColor;
			float _StarDensity;
			float _StarSize;
			float _StarBrightness;
			float _TwinkleSpeed;
			float4 _ClipRect;
			float4 _MainTex_ST;

			v2f vert (appdata_t v) {
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.worldPosition = v.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				OUT.color = v.color * _Color;
				return OUT;
			}

			// cheap 2D hash, returns pseudo-random value in [0,1)
			float hash12 (float2 p) {
				return frac(sin(dot(p, float2(41.3, 289.1))) * 43758.5453123);
			}
			float2 hash22 (float2 p) {
				return float2(hash12(p), hash12(p + 19.19));
			}

			fixed4 frag (v2f IN) : SV_Target {
				fixed4 color = tex2D(_MainTex, IN.texcoord) * IN.color;

				float2 uv = IN.texcoord;
				float2 grid = uv * _StarDensity;
				float2 cell = floor(grid);
				float2 f = frac(grid);

				float2 starPos = hash22(cell);
				float dist = length(f - starPos);

				float size = _StarSize * (0.4 + 0.6 * hash12(cell + 5.2));
				float star = smoothstep(size, 0.0, dist);

				float twinklePhase = hash12(cell + 11.7) * 6.2831853;
				float twinkle = 0.5 + 0.5 * sin(_Time.y * _TwinkleSpeed + twinklePhase);

				float starMask = star * twinkle * _StarBrightness;

				color.rgb += _StarColor.rgb * starMask;
				color.a = saturate(color.a + starMask * _StarColor.a);

				#ifdef UNITY_UI_CLIP_RECT
				color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
			ENDCG
		}
	}
}
