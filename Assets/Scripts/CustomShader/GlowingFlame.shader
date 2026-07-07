Shader "Custom/GlowingFlame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Shake Settings)]
        _ShakeSpeed ("Tốc độ rung", Float) = 15.0
        _ShakeX ("Cường độ X (Ngang)", Float) = 0.02
        _ShakeY ("Cường độ Y (Dọc)", Float) = 0.01
        
        [Header(Height Control)]
        _HeightThreshold ("Vị trí bắt đầu rung (0-1)", Range(0, 1)) = 0.5
        _EdgeSoftness ("Độ mượt vùng chuyển", Range(0.01, 0.5)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "CanUseSpriteAtlas"="True" 
        }

        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _ShakeSpeed, _ShakeX, _ShakeY, _HeightThreshold, _EdgeSoftness;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // Tính toán trọng số dựa trên độ cao UV (y từ 0 đến 1)
                // uv.y = 0 là đáy, uv.y = 1 là đỉnh
                float weight = smoothstep(_HeightThreshold, _HeightThreshold + _EdgeSoftness, uv.y);

                // Tính toán độ lệch (Offset) theo thời gian
                // Sử dụng hàm sin/cos khác nhau cho X và Y để chuyển động tự nhiên
                float offsetX = sin(_Time.y * _ShakeSpeed) * _ShakeX * weight;
                float offsetY = cos(_Time.y * _ShakeSpeed * 1.1) * _ShakeY * weight;

                // Áp dụng độ lệch vào UV trước khi lấy màu từ Texture
                float2 displacedUV = uv + float2(offsetX, offsetY);
                
                fixed4 c = tex2D(_MainTex, displacedUV) * IN.color;
                c.rgb *= c.a; // Alpha Premultiply cho Sprite
                
                return c;
            }
            ENDCG
        }
    }
}