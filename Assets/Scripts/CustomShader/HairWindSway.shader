// HairWindSway.shader
// Chỉ làm TÓC của nhân vật bay nhẹ theo gió, phần còn lại (mặt, tay, áo) đứng yên.
// Nhân vật là 1 sprite phẳng chưa tách lớp, nên vùng tóc được đánh dấu bằng 1 ẢNH MASK
// RIÊNG (_HairMask, cùng kích thước/UV với _MainTex) thay vì đoán bằng hình học ellipse
// -> khớp chính xác 100% theo đúng hình dạng tóc dù xoăn/nhọn phức tạp thế nào.
//
// CÁCH TẠO MASK (_HairMask):
//   Mở ảnh nhân vật trong Photoshop/GIMP, tạo 1 layer mới cùng kích thước, tô TRẮNG lên
//   toàn bộ vùng tóc, ĐEN ở phần còn lại. Muốn ngọn tóc lay nhiều hơn gốc tóc thì tô
//   GRADIENT: trắng ở ngọn tóc -> xám/đen dần về phía chân tóc (da đầu). Xuất ra PNG
//   grayscale, kênh R sẽ được đọc làm cường độ lay. Import với Texture Type = Default,
//   sRGB tắt (mask không phải màu), Wrap Mode = Clamp.
//
// CÁCH DÙNG: Gán material dùng shader này cho SpriteRenderer/Image của nhân vật, gán ảnh
// gốc vào _MainTex và ảnh mask vừa vẽ vào _HairMask.
//
// Built-in Render Pipeline (CGPROGRAM). Vẫn chạy được trong URP vì là shader unlit
// không phụ thuộc lighting pipeline (dự án đang dùng URP - PC_RPAsset).

Shader "Custom/HairWindSway"
{
    Properties
    {
        [PerRendererData] _MainTex ("Nhân vật (RGBA, cắt nền)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Hair Mask Anh danh dau vung toc kenh R)]
        [NoScaleOffset] _HairMask ("Mask tóc (trắng = lay mạnh, đen = giữ nguyên)", 2D) = "black" {}
        _MaskInfluence ("Cường độ ảnh hưởng tổng của mask", Range(0,2)) = 1.0

        [Header(Chuyen dong bay Nhieu tan so cho tu nhien)]
        _SwaySpeed1 ("Tốc độ lay 1", Range(0,5)) = 1.4
        _SwayAmount1 ("Biên độ lay 1", Range(0,0.1)) = 0.02
        _SwayFreqX1 ("Tần số theo chiều ngang 1", Range(0,20)) = 6

        _SwaySpeed2 ("Tốc độ lay 2 (chi tiết nhỏ)", Range(0,8)) = 3.1
        _SwayAmount2 ("Biên độ lay 2", Range(0,0.05)) = 0.008
        _SwayFreqX2 ("Tần số theo chiều ngang 2", Range(0,30)) = 13

        _VerticalFlutter ("Biên độ phất phơ theo chiều dọc", Range(0,0.03)) = 0.006
        _FlutterSpeed ("Tốc độ phất phơ dọc", Range(0,6)) = 2.4
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
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

            sampler2D _HairMask;
            float _MaskInfluence;

            float _SwaySpeed1, _SwayAmount1, _SwayFreqX1;
            float _SwaySpeed2, _SwayAmount2, _SwayFreqX2;
            float _VerticalFlutter, _FlutterSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                // ---- Vùng tóc: đọc trực tiếp từ ảnh mask do artist vẽ (kênh R) ----
                // Trắng = lay mạnh, đen = giữ nguyên. Gradient trắng->đen từ ngọn về gốc
                // trong chính ảnh mask sẽ tự cho hiệu ứng "chân tóc cứng, ngọn tóc mềm".
                float weight = tex2D(_HairMask, uv).r * _MaskInfluence;

                // ---- Lay theo nhiều tần số để trông tự nhiên như từng lọn tóc ----
                float sway1 = sin(t * _SwaySpeed1 + uv.x * _SwayFreqX1) * _SwayAmount1;
                float sway2 = sin(t * _SwaySpeed2 + uv.x * _SwayFreqX2 + 1.7) * _SwayAmount2;
                float flutterY = sin(t * _FlutterSpeed + uv.x * 9.0 + 2.3) * _VerticalFlutter;

                float2 offset = float2(sway1 + sway2, flutterY) * weight;

                fixed4 col = tex2D(_MainTex, uv + offset) * i.color;
                clip(col.a - 0.001);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
