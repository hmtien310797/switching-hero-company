Shader "Custom/Spine-Skeleton-RimGlow" {
	Properties {
		[NoScaleOffset] _MainTex ("Main Texture", 2D) = "black" {}
		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0

		[Header(Rim Glow)]
		_RimColor ("Rim Color", Color) = (0.4, 0.9, 1.0, 1.0)
		_RimWidth ("Rim Width (texels)", Range(0.5, 8)) = 2.0
		_RimIntensity ("Rim Intensity", Range(0, 5)) = 1.5
		[Toggle(_RIM_PULSE_ON)] _RimPulse ("Pulse", Int) = 1
		_RimPulseSpeed ("Pulse Speed", Range(0, 10)) = 2.0
	}

	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

		Fog { Mode Off }
		Cull Off
		ZWrite Off
		Blend One OneMinusSrcAlpha
		Lighting Off

		Pass {
			Name "Normal"

			CGPROGRAM
			#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
			#pragma shader_feature _ _RIM_PULSE_ON
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "../Addressable/SharedAsset/CGIncludes/Spine-Common.cginc"

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			fixed4 _RimColor;
			float _RimWidth;
			float _RimIntensity;
			float _RimPulseSpeed;

			struct VertexInput {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 vertexColor : COLOR;
			};

			struct VertexOutput {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 vertexColor : COLOR;
			};

			VertexOutput vert (VertexInput v) {
				VertexOutput o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.vertexColor = PMAGammaToTargetSpace(v.vertexColor);
				return o;
			}

			float4 frag (VertexOutput i) : SV_Target {
				float4 texColor = tex2D(_MainTex, i.uv);

				#if defined(_STRAIGHT_ALPHA_INPUT)
				texColor.rgb *= texColor.a;
				#endif

				float4 result = texColor * i.vertexColor;

				// Edge glow: this pixel is opaque-ish but sits next to more-transparent
				// neighbours, i.e. it's on the silhouette border of the sprite.
				float centerAlpha = texColor.a;
				float2 texel = _MainTex_TexelSize.xy * _RimWidth;

				float edge = 0;
				edge += saturate(centerAlpha - tex2D(_MainTex, i.uv + float2( texel.x, 0)).a);
				edge += saturate(centerAlpha - tex2D(_MainTex, i.uv + float2(-texel.x, 0)).a);
				edge += saturate(centerAlpha - tex2D(_MainTex, i.uv + float2(0,  texel.y)).a);
				edge += saturate(centerAlpha - tex2D(_MainTex, i.uv + float2(0, -texel.y)).a);
				edge = saturate(edge);

				float pulse = 1.0;
				#if defined(_RIM_PULSE_ON)
				pulse = 0.5 + 0.5 * sin(_Time.y * _RimPulseSpeed);
				#endif

				float rimMask = edge * centerAlpha * _RimIntensity * pulse;

				result.rgb += _RimColor.rgb * rimMask;
				result.a = saturate(result.a + rimMask * _RimColor.a);

				return result;
			}
			ENDCG
		}
	}
}
