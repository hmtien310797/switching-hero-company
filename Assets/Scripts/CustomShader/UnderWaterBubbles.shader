Shader "Custom/UnderwaterBubbles"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BubbleColor ("Bubble Color", Color) = (0.8, 0.9, 1.0, 0.5) // Màu cơ bản (xanh nhạt)
        _RimColor ("Rim Color (Iridescence)", Color) = (1.0, 0.5, 0.8, 1.0) // Màu ánh sắc ở viền
        _Speed ("Rising Speed", Float) = 0.5 // Tốc độ nổi lên
        _WobbleScale ("Wobble Scale", Float) = 0.1 // Độ lắc lư qua lại
        _WobbleSpeed ("Wobble Speed", Float) = 2.0 // Tốc độ lắc lư
        _SpawnRate ("Spawn Rate (Density)", Float) = 10.0 // Mật độ bong bóng
        _BubbleSize ("Base Bubble Size", Float) = 0.05 // Kích thước cơ bản
        _RimPower ("Rim Power", Float) = 3.0 // Độ đậm của viền Fresnel
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        // Bật Alpha Blending để nhìn xuyên qua bong bóng
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL; // Cần Normal cho hiệu ứng Fresnel
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BubbleColor;
            float4 _RimColor;
            float _Speed;
            float _WobbleScale;
            float _WobbleSpeed;
            float _SpawnRate;
            float _BubbleSize;
            float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            // Hàm tạo số ngẫu nhiên đơn giản dựa trên UV
            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            // Hàm tạo nhiễu Perlin 2D
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(dot(hash22(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                                 dot(hash22(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                            lerp(dot(hash22(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                                 dot(hash22(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
            }

            // Hàm SDF vẽ hình tròn (rỗng giữa)
            float sdCircle(float2 p, float r, float thickness)
            {
                float d = length(p) - r;
                // Làm cho hình tròn rỗng bên trong (Ring effect)
                return abs(d) - thickness;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Tính toán UV động (chuyển động nổi lên và lắc lư)
                float2 movingUV = i.uv;
                movingUV.y += _Time.y * _Speed; // Chuyển động lên trên
                
                // Thêm độ lắc lư qua lại dựa trên Sin và Noise
                float wobble = sin(_Time.y * _WobbleSpeed + i.uv.y * 10.0) * _WobbleScale;
                movingUV.x += wobble * noise(movingUV * 5.0);

                // 2. Tạo nhiều ô grid để sinh bong bóng
                float2 gridUV = movingUV * _SpawnRate;
                float2 i_grid = floor(gridUV); // ID của ô grid
                float2 f_grid = frac(gridUV) - 0.5; // UV bên trong mỗi ô (đưa tâm về 0.0)

                // 3. Tạo biến thể cho từng bong bóng (kích thước, vị trí ngẫu nhiên trong ô)
                float2 rand = hash22(i_grid); // Số ngẫu nhiên từ -1 đến 1
                float sizeVar = _BubbleSize * (1.0 + rand.x * 0.5); // Kích thước biến thiên +-50%
                f_grid += rand * 0.3; // Dịch chuyển vị trí ngẫu nhiên trong ô

                // 4. Vẽ hình tròn bong bóng (Ring)
                float thickness = 0.001; // Độ dày viền bong bóng
                float d = sdCircle(f_grid, sizeVar, thickness);
                
                // Làm mềm cạnh (Antialiasing)
                float bubbleMask = smoothstep(0.01, 0.0, d);

                // 5. Thêm hiệu ứng Fresnel (Ánh sắc ở viền)
                // Dựa trên góc nhìn của camera so với mặt pháp tuyến
                float3 normal = normalize(i.normal);
                float3 viewDir = normalize(i.viewDir);
                float fresnel = saturate(dot(normal, viewDir));
                fresnel = pow(1.0 - fresnel, _RimPower); // Đẩy độ đậm về phía viền

                // 6. Kết hợp màu sắc
                // Màu cơ bản của màng bong bóng
                float4 col = _BubbleColor;
                
                // Thêm màu ánh sắc Fresnel vào viền
                col.rgb = lerp(col.rgb, _RimColor.rgb, fresnel * _RimColor.a);
                
                // Áp dụng mask hình tròn và Alpha
                col.a *= bubbleMask;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}