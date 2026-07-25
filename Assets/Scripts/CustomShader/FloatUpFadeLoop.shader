// FloatUpFadeLoop.shader
// Dùng cho các sprite nhỏ bay lên rồi biến mất, lặp lại vô hạn (VD: nốt nhạc, bong bóng,
// hạt sáng, icon +1...). Toàn bộ animation chạy bằng _Time trong shader, KHÔNG cần script.
//
// Cách hoạt động:
//   - Một "chu kỳ" dài _CycleDuration giây. Trong 1 chu kỳ: sprite trồi lên theo trục Y
//     (object space), mờ dần vào ở đầu chu kỳ, mờ dần ra ở cuối chu kỳ, rồi lặp lại từ đầu.
//   - Nếu nhiều object dùng CHUNG 1 Material (tốt cho batching), mỗi object sẽ TỰ ĐỘNG
//     lệch pha với nhau nhờ hash từ vị trí world của chính nó -> không cần tạo nhiều
//     Material hay script riêng để random hoá. Có thể cộng thêm _PhaseOffset thủ công
//     nếu muốn ép lệch pha theo ý muốn (VD: đặt khác nhau cho từng biến thể nốt nhạc).
//
// YÊU CẦU: Pivot của sprite/RectTransform nên đặt ở giữa hoặc đáy hình để scale/rotate
// (nếu dùng) trông tự nhiên.
//
// Built-in Render Pipeline (CGPROGRAM). Nếu dự án dùng URP: đổi CGPROGRAM/ENDCG ->
// HLSLPROGRAM/ENDHLSL, include Core.hlsl, UnityObjectToClipPos -> TransformObjectToHClip,
// và thêm Tags { "RenderPipeline"="UniversalPipeline" }. Phần logic giữ nguyên.

Shader "Custom/FloatUpFadeLoop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (nốt nhạc...)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Vong lap Bay len Mo dan)]
        _CycleDuration ("Thời gian 1 chu kỳ (giây)", Range(0.2, 10)) = 2.2
        _RiseDistance ("Khoảng cách bay lên (object units)", Float) = 1.0
        _RiseCurvePower ("Độ cong đường bay (1=đều, nho hon 1=nhanh dan, lon hon 1=cham dan)", Range(0.2, 3)) = 0.8
        _PhaseOffset ("Lệch pha thủ công (0-1), cộng thêm vào lệch pha tự động", Range(0,1)) = 0

        [Header(Mo dan Fade)]
        _FadeInRatio ("Tỉ lệ thời gian mờ dần VÀO đầu chu kỳ", Range(0,0.5)) = 0.15
        _FadeOutRatio ("Tỉ lệ thời gian mờ dần RA cuối chu kỳ", Range(0,0.5)) = 0.35

        [Header(Chuyen dong phu Lac ngang Xoay nhe)]
        _HorizontalDrift ("Biên độ lắc ngang", Range(0,1)) = 0.15
        _DriftFrequency ("Tần số lắc ngang", Range(0,5)) = 1.5
        _RotateAmount ("Biên độ xoay lắc (độ)", Range(0,45)) = 10
        _RotateFrequency ("Tần số xoay lắc", Range(0,5)) = 1.2

        [Header(Kich thuoc theo vong doi)]
        _ScaleStart ("Tỉ lệ lúc bắt đầu chu kỳ", Range(0,2)) = 0.7
        _ScaleEnd ("Tỉ lệ lúc kết thúc chu kỳ", Range(0,2)) = 1.1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _CycleDuration, _RiseDistance, _RiseCurvePower, _PhaseOffset;
            float _FadeInRatio, _FadeOutRatio;
            float _HorizontalDrift, _DriftFrequency, _RotateAmount, _RotateFrequency;
            float _ScaleStart, _ScaleEnd;

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;

                // Lệch pha tự động theo vị trí world của object -> nhiều object dùng chung
                // 1 Material vẫn bay lệch nhau, không cần tạo nhiều Material/script.
                float3 worldPos = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
                float autoPhase = Hash21(worldPos.xy * 12.9898 + worldPos.zz * 78.233);
                float phase = frac(_PhaseOffset + autoPhase);

                float cycle = frac(_Time.y / max(_CycleDuration, 0.0001) + phase);

                // ---- Xoay lắc nhẹ quanh pivot ----
                float rotDeg = sin(cycle * 6.2831853 * _RotateFrequency + phase * 6.2831853) * _RotateAmount;
                float rad = radians(rotDeg);
                float s = sin(rad), c = cos(rad);
                float2 rotated = float2(
                    v.vertex.x * c - v.vertex.y * s,
                    v.vertex.x * s + v.vertex.y * c
                );

                // ---- Phóng to/nhỏ theo vòng đời ----
                float scale = lerp(_ScaleStart, _ScaleEnd, cycle);
                rotated *= scale;

                // ---- Bay lên + lắc ngang ----
                float rise = pow(cycle, _RiseCurvePower) * _RiseDistance;
                float drift = sin(cycle * 6.2831853 * _DriftFrequency + phase * 6.2831853) * _HorizontalDrift * cycle;

                v.vertex.xy = rotated + float2(drift, rise);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // ---- Mờ dần vào đầu chu kỳ, mờ dần ra cuối chu kỳ ----
                float fadeIn  = smoothstep(0.0, max(_FadeInRatio, 0.0001), cycle);
                float fadeOut = 1.0 - smoothstep(1.0 - max(_FadeOutRatio, 0.0001), 1.0, cycle);
                float alphaFade = saturate(fadeIn * fadeOut);

                o.color = v.color * _Color;
                o.color.a *= alphaFade;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                clip(col.a - 0.001);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
