Shader "Custom/ChapterStageBg1"
{
    Properties
    {
        _MainTex   ("Storm Sky Background", 2D)    = "white" {}
        _Color     ("Tint",                 Color) = (1, 1, 1, 1)
        _SpeedX    ("Wind Speed X",         Float) = 0.014
        _SpeedY    ("Wind Speed Y",         Float) = -0.006
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            float     _SpeedX;
            float     _SpeedY;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                // Dich chuyen UV theo huong gio bao (cheo xuong phai)
                float2 scrollUV = i.uv + float2(_SpeedX, _SpeedY) * _Time.y;

                // Giu trong gia tri (0.02 ~ 0.98) de khong thay bien anh
                scrollUV = clamp(scrollUV, 0.02, 0.98);

                fixed4 col = tex2D(_MainTex, scrollUV);
                return col * _Color;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
