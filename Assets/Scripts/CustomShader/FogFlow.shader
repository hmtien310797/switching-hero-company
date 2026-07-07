Shader "Custom/FogFlow" {
Properties {
    _MainTex      ("Fog Texture",     2D)          = "white" {}
    _Color        ("Fog Color",       Color)       = (1, 1, 1, 1)
    _Opacity      ("Opacity",         Range(0, 1)) = 0.5
    _SpeedX1      ("Layer 1 Speed X", Range(-5, 5)) = 1.0
    _SpeedY1      ("Layer 1 Speed Y", Range(-5, 5)) = 0.5
    _SpeedX2      ("Layer 2 Speed X", Range(-5, 5)) = -0.5
    _SpeedY2      ("Layer 2 Speed Y", Range(-5, 5)) = 0.3
    _Tiling2      ("Layer 2 Tiling",  Range(0.1, 5)) = 1.5
    _EdgeSoftness ("Edge Softness",   Range(0, 0.5)) = 0.15
    [Toggle] _DebugUV ("Debug UV Motion",  Float) = 0
}

SubShader {
    Tags {
        "Queue"           = "Transparent"
        "RenderType"      = "Transparent"
        "IgnoreProjector" = "True"
    }
    Blend SrcAlpha OneMinusSrcAlpha
    ZWrite Off
    Cull Off

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4    _MainTex_ST;   // Unity tự fill tiling/offset từ texture inspector
        fixed4    _Color;
        float     _Opacity;
        float     _SpeedX1, _SpeedY1;
        float     _SpeedX2, _SpeedY2;
        float     _Tiling2;
        float     _EdgeSoftness;
        float     _DebugUV;

        struct appdata {
            float4 vertex : POSITION;
            float2 uv     : TEXCOORD0;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float2 uv1    : TEXCOORD0;
            float2 uv2    : TEXCOORD1;
            float2 rawUV  : TEXCOORD2;
        };

        v2f vert(appdata v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);

            // TRANSFORM_TEX dùng tiling/offset chuẩn của Unity
            float2 baseUV = TRANSFORM_TEX(v.uv, _MainTex);

            // Layer 1: scroll theo _Time.y
            o.uv1 = baseUV + float2(_Time.y * _SpeedX1, _Time.y * _SpeedY1);

            // Layer 2: tiling nhân thêm + scroll ngược chiều
            o.uv2 = baseUV * _Tiling2 + float2(_Time.y * _SpeedX2, _Time.y * _SpeedY2);

            o.rawUV = v.uv;
            return o;
        }

        fixed4 frag(v2f i) : SV_Target {
            fixed4 fog1 = tex2D(_MainTex, i.uv1);
            fixed4 fog2 = tex2D(_MainTex, i.uv2);

            float luma1 = dot(fog1.rgb, float3(0.299, 0.587, 0.114));
            float luma2 = dot(fog2.rgb, float3(0.299, 0.587, 0.114));
            float fogDensity = luma1 * luma2;

            // Soft edge 4 cạnh
            float2 uv = i.rawUV;
            float edgeX = smoothstep(0.0, _EdgeSoftness, uv.x)
                        * smoothstep(0.0, _EdgeSoftness, 1.0 - uv.x);
            float edgeY = smoothstep(0.0, _EdgeSoftness, uv.y)
                        * smoothstep(0.0, _EdgeSoftness, 1.0 - uv.y);

            // Debug: hiện UV scroll để confirm shader đang chạy
            // Bật "Debug UV Motion" trong Inspector → thấy màu chuyển động = shader OK
            if (_DebugUV > 0.5)
            {
                float2 scrollUV = frac(i.uv1);
                return fixed4(scrollUV.x, scrollUV.y, 0, 1);
            }

            fixed4 col = _Color;
            col.a = fogDensity * _Opacity * edgeX * edgeY;

            return col;
        }
        ENDCG
    }
}
Fallback Off
}
