Shader "Custom/ForgeBackgroundEffect"
{
    Properties
    {
        _MainTex ("Background", 2D) = "white" {}
        _Color   ("Tint", Color)     = (1,1,1,1)

        [Header(Fire Hue Flicker doc mau tren chinh anh goc)]
        _FireHue             ("Fire Hue (0-1)",          Range(0,1))   = 0.07
        _FireHueRange        ("Fire Hue Range",          Range(0,0.3)) = 0.08
        _FireMinSat          ("Fire Min Saturation",     Range(0,1))   = 0.35
        _FireMinVal          ("Fire Min Brightness",     Range(0,1))   = 0.35
        _FireFlickerSpeed    ("Flicker Speed",           Float)        = 9.0
        _FireFlickerStrength ("Flicker Strength",        Range(0,1))   = 0.3

        [Header(Window Light Pulse doc mau tren chinh anh goc)]
        _WindowHue             ("Window Hue (0-1)",       Range(0,1))   = 0.56
        _WindowHueRange        ("Window Hue Range",       Range(0,0.3)) = 0.08
        _WindowMinSat          ("Window Min Saturation",  Range(0,1))   = 0.25
        _WindowMinVal          ("Window Min Brightness",  Range(0,1))   = 0.35
        _WindowPulseSpeed      ("Pulse Speed",            Float)        = 2.2
        _WindowPulseStrength   ("Pulse Strength",         Range(0,1))   = 0.25

        [Header(Magic Ribbon Glow doc mau tren chinh anh goc)]
        _RibbonHue           ("Ribbon Hue (0-1, wrap gan 0/1)", Range(0,1)) = 0.98
        _RibbonHueRange      ("Ribbon Hue Range",        Range(0,0.3)) = 0.05
        _RibbonMinSat        ("Ribbon Min Saturation",   Range(0,1))   = 0.4
        _RibbonMinVal        ("Ribbon Min Brightness",   Range(0,1))   = 0.4
        _RibbonFlowSpeed     ("Flow Speed",              Float)        = 1.6
        _RibbonGlowStrength  ("Glow Strength",           Range(0,2))   = 0.6
        _RibbonGlowColor     ("Glow Color",              Color)        = (1.0, 0.25, 0.3, 1)

        [Header(Light Shaft Procedural Khong Doc Anh)]
        _ShaftX         ("Shaft Origin X",   Range(0,1)) = 0.82
        _ShaftY         ("Shaft Origin Y",   Range(0,1)) = 0.55
        _ShaftColor     ("Shaft Color",      Color)      = (0.6, 0.85, 1.0, 1)
        _ShaftIntensity ("Shaft Intensity",  Range(0,1)) = 0.18
        _ShaftCount     ("Shaft Count",      Range(3,20))= 8
        _ShaftSpeed     ("Shaft Rotate Speed", Float)    = 0.05
        _ShaftFalloff   ("Shaft Falloff",    Range(0.5,10)) = 3.2
        _ShaftAspect    ("Aspect W/H",       Float)      = 1.0

        [Header(Floating Embers Procedural Khong Doc Anh)]
        _EmberOriginX  ("Ember Origin X",   Range(0,1)) = 0.34
        _EmberOriginY  ("Ember Origin Y",   Range(0,1)) = 0.42
        _EmberSpreadX  ("Ember Spread X",   Range(0,1)) = 0.12
        _EmberColor    ("Ember Color",      Color)      = (1.0, 0.55, 0.15, 1)
        _EmberDensity  ("Ember Density",    Float)       = 26.0
        _EmberSize     ("Ember Size",       Float)       = 20.0
        _EmberRiseSpeed("Ember Rise Speed", Float)       = 0.06
        _EmberBright   ("Ember Brightness", Float)       = 1.2
        _EmberMaxY     ("Ember Max Rise Y", Range(0,1))  = 0.75

        [Header(Heat Haze Cuc Bo Tai Lo Lua Procedural Khong Doc Anh)]
        _HeatCenterX  ("Heat Center X",  Range(0,1))    = 0.34
        _HeatCenterY  ("Heat Center Y",  Range(0,1))    = 0.38
        _HeatRadius   ("Heat Radius",    Range(0,0.5))  = 0.16
        _HeatStrength ("Heat Distortion Strength", Range(0,0.05)) = 0.006
        _HeatSpeed    ("Heat Speed",     Float)         = 3.0
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

            float  _FireHue, _FireHueRange, _FireMinSat, _FireMinVal, _FireFlickerSpeed, _FireFlickerStrength;
            float  _WindowHue, _WindowHueRange, _WindowMinSat, _WindowMinVal, _WindowPulseSpeed, _WindowPulseStrength;
            float  _RibbonHue, _RibbonHueRange, _RibbonMinSat, _RibbonMinVal, _RibbonFlowSpeed, _RibbonGlowStrength;
            fixed4 _RibbonGlowColor;

            float  _ShaftX, _ShaftY, _ShaftIntensity, _ShaftCount, _ShaftSpeed, _ShaftFalloff, _ShaftAspect;
            fixed4 _ShaftColor;

            float  _EmberOriginX, _EmberOriginY, _EmberSpreadX, _EmberDensity, _EmberSize, _EmberRiseSpeed, _EmberBright, _EmberMaxY;
            fixed4 _EmberColor;

            float _HeatCenterX, _HeatCenterY, _HeatRadius, _HeatStrength, _HeatSpeed;

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

            // ---- RGB -> HSV (cong thuc chuan) ----
            float3 RGBtoHSV(float3 c) {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float  d = q.x - min(q.w, q.y);
                float  e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            // Khoang cach hue theo vong tron (hue wrap 0..1)
            float HueDist(float h, float target) {
                float d = abs(h - target);
                return min(d, 1.0 - d);
            }

            float Hash(float2 p)  { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float2 Hash2(float2 p){ return float2(Hash(p), Hash(p + float2(31.7, 17.3))); }

            // 1 hat ember bay len tu vi tri lo lua
            float3 EmberCell(float2 cell, float2 uv, float t) {
                float2 h      = Hash2(cell);
                float  tOff   = Hash(cell + float2(5.1, 3.7)) * 12.0;
                float  spanY  = max(0.001, _EmberMaxY - _EmberOriginY);

                float2 pos;
                pos.x = _EmberOriginX + (h.x - 0.5) * _EmberSpreadX;
                pos.y = _EmberOriginY + frac(h.y - (t + tOff) * _EmberRiseSpeed) * spanY;

                float2 d       = (uv - pos) * _EmberDensity;
                float  dist    = length(d);
                float  twinkle = sin(t * 2.2 + h.x * 6.2832) * 0.3 + 0.7;
                float  fadeTop = 1.0 - smoothstep(_EmberOriginY + spanY * 0.7, _EmberMaxY, uv.y);
                float  glow    = exp(-dist * _EmberSize) * twinkle * _EmberBright * fadeTop;

                return _EmberColor.rgb * glow;
            }

            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y;

                // ---- 1. Heat haze cuc bo quanh lo lua (lam lech UV truoc khi sample) ----
                float2 heatCenter = float2(_HeatCenterX, _HeatCenterY);
                float  heatDist   = length(i.uv - heatCenter);
                float  heatMask   = 1.0 - smoothstep(_HeatRadius * 0.5, _HeatRadius, heatDist);
                float2 heatOffset = float2(
                    sin(t * _HeatSpeed        + i.uv.y * 18.0),
                    cos(t * _HeatSpeed * 0.8  + i.uv.x * 14.0)
                ) * _HeatStrength * heatMask;

                float2 uv  = i.uv + heatOffset;
                fixed4 col = tex2D(_MainTex, uv) * _Color;

                // ---- 2. Phan tich mau ngay tren pixel cua anh goc (khong can tach layer) ----
                float3 hsv = RGBtoHSV(col.rgb);
                float  hue = hsv.x, sat = hsv.y, val = hsv.z;

                // Fire flicker: chi anh huong vung mau cam/do đủ sat/val (khoi lua trong lo)
                float fireMask = (1.0 - smoothstep(_FireHueRange * 0.5, _FireHueRange, HueDist(hue, _FireHue)))
                               * step(_FireMinSat, sat) * step(_FireMinVal, val);
                float fireFlicker = sin(t * _FireFlickerSpeed) * 0.40
                                  + sin(t * _FireFlickerSpeed * 2.3 + 1.1) * 0.25
                                  + sin(t * _FireFlickerSpeed * 0.5 + 2.6) * 0.35;
                fireFlicker = fireFlicker * 0.5 + 0.5;
                col.rgb *= lerp(1.0, lerp(1.0 - _FireFlickerStrength, 1.0 + _FireFlickerStrength, fireFlicker), fireMask);

                // Window pulse: chi anh huong vung mau xanh lam đủ sat/val (anh sang cua so)
                float windowMask = (1.0 - smoothstep(_WindowHueRange * 0.5, _WindowHueRange, HueDist(hue, _WindowHue)))
                                 * step(_WindowMinSat, sat) * step(_WindowMinVal, val);
                float windowPulse = sin(t * _WindowPulseSpeed) * 0.5 + 0.5;
                col.rgb *= lerp(1.0, lerp(1.0 - _WindowPulseStrength, 1.0 + _WindowPulseStrength, windowPulse), windowMask);

                // Ribbon glow: chi anh huong vung mau do/hong dam đủ sat/val (vet nang luong)
                float ribbonMask = (1.0 - smoothstep(_RibbonHueRange * 0.5, _RibbonHueRange, HueDist(hue, _RibbonHue)))
                                 * step(_RibbonMinSat, sat) * step(_RibbonMinVal, val);
                float flowPhase = (uv.x - uv.y) * 6.0 - t * _RibbonFlowSpeed;
                float ribbonFlow = sin(flowPhase) * 0.5 + 0.5;
                col.rgb += _RibbonGlowColor.rgb * ribbonFlow * _RibbonGlowStrength * ribbonMask;

                // ---- 3. Light shaft procedural, khong phu thuoc noi dung anh ----
                float2 toShaft  = uv - float2(_ShaftX, _ShaftY);
                toShaft.x      *= _ShaftAspect;
                float shaftDist = length(toShaft);
                float angle     = atan2(toShaft.y, toShaft.x);
                float shaftPat  = pow(max(0.0, sin(angle * _ShaftCount + t * _ShaftSpeed)), 2.5);
                float shaftFall = exp(-shaftDist * _ShaftFalloff);
                col.rgb += _ShaftColor.rgb * shaftPat * shaftFall * _ShaftIntensity;

                // ---- 4. Floating embers procedural, khong phu thuoc noi dung anh ----
                float2 baseCell = floor(uv * _EmberDensity);
                float3 embers = float3(0,0,0);
                embers += EmberCell(baseCell + float2(-1,-1), uv, t);
                embers += EmberCell(baseCell + float2( 0,-1), uv, t);
                embers += EmberCell(baseCell + float2( 1,-1), uv, t);
                embers += EmberCell(baseCell + float2(-1, 0), uv, t);
                embers += EmberCell(baseCell + float2( 0, 0), uv, t);
                embers += EmberCell(baseCell + float2( 1, 0), uv, t);
                embers += EmberCell(baseCell + float2(-1, 1), uv, t);
                embers += EmberCell(baseCell + float2( 0, 1), uv, t);
                embers += EmberCell(baseCell + float2( 1, 1), uv, t);
                col.rgb += embers;

                return col;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
