Shader "Kiba/UI/LiquidGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [PerRendererData] _StencilComp ("Stencil Comparison", Float) = 8
        [PerRendererData] _Stencil ("Stencil ID", Float) = 0
        [PerRendererData] _StencilOp ("Stencil Operation", Float) = 0
        [PerRendererData] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [PerRendererData] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [PerRendererData] _ColorMask ("Color Mask", Float) = 15

        [PerRendererData] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [PerRendererData] _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)

        _GlassTint ("Glass Tint", Color) = (0.9,0.95,1.0,1.0)
        _Opacity ("Effect Strength", Range(0,1)) = 0.7
        _GlassAlpha ("Glass Alpha", Range(0,1)) = 0.6
        _EdgeAlpha ("Edge Alpha", Range(0,1)) = 0.6
        _IndexOfRefraction ("Index Of Refraction", Range(1.0, 1.4)) = 1.05

        _CornerRadius ("Corner Radius", Range(0,0.5)) = 0.2
        _EdgeSoftness ("Edge Softness", Range(0.001,0.2)) = 0.02
        _BorderThickness ("Border Thickness", Range(0.01,0.5)) = 0.12

        _BlurRadius ("Blur Radius", Range(0,4)) = 1.5
        _HighlightIntensity ("Highlight Intensity", Range(0,4)) = 2.0
        _HighlightWidth ("Highlight Width", Range(0.001,0.3)) = 0.08
        _GlassBrightness ("Glass Brightness", Range(0,2)) = 1.1

        _MarginPixels ("Margin Pixels", Range(0,-10)) = -1
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

        GrabPass { "_GrabTex" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            Fail [_StencilOp]
            ZFail [_StencilOp]
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
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 grabPos : TEXCOORD2;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            sampler2D _GrabTex;
            float4 _GrabTex_TexelSize;

            float4 _ClipRect;
            float _UseUIAlphaClip;
            float4 _GlassTint;
            float _Opacity;
            float _GlassAlpha;
            float _EdgeAlpha;
            float _IndexOfRefraction;
            float _CornerRadius;
            float _EdgeSoftness;
            float _BorderThickness;

            float _BlurRadius;
            float _HighlightIntensity;
            float _HighlightWidth;
            float _GlassBrightness;
            float _MarginPixels;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                #ifdef UNITY_UI_CLIP_RECT
                    if (UnityGet2DClipping(i.worldPosition.xy, _ClipRect) == 0)
                        discard;
                #endif

                float4 texCol = tex2D(_MainTex, i.uv) * i.color;

                float du_dx = abs(ddx(i.uv.x));
                float dv_dy = abs(ddy(i.uv.y));
                float aspect = 1.0;
                if (du_dx > 1e-6 && dv_dy > 1e-6)
                    aspect = dv_dy / du_dx;

                float marginUx = _MarginPixels * du_dx;
                float marginUy = _MarginPixels * dv_dy;
                marginUx = min(marginUx, 0.25);
                marginUy = min(marginUy, 0.25);

                float2 shapeUV;
                shapeUV.x = lerp(marginUx, 1.0 - marginUx, i.uv.x);
                shapeUV.y = lerp(marginUy, 1.0 - marginUy, i.uv.y);

                float2 p = float2((shapeUV.x - 0.5) * aspect, shapeUV.y - 0.5);
                float2 halfSize = float2(aspect, 1.0) * 0.5;

                float maxRadius = min(halfSize.x, halfSize.y);
                float rScale = saturate(_CornerRadius * 2.0);
                float r = rScale * maxRadius;

                float2 rect = halfSize - r;
                float2 d = abs(p) - rect;
                float outsideDist = length(max(d, 0.0));
                float insideDist = min(max(d.x, d.y), 0.0);
                float dist = outsideDist + insideDist - r;

                float shape = saturate(1.0 - smoothstep(0.0, _EdgeSoftness, dist));

                float borderWidth = max(_BorderThickness * maxRadius, 1e-4);
                float edgeFactor = saturate(1.0 - abs(dist) / borderWidth);

                float iorAmount = (_IndexOfRefraction - 1.0) * 50.0;
                float refractStrength = iorAmount * edgeFactor;

                float2 centerDir = normalize((shapeUV - 0.5) + 1e-5);
                float2 offset = centerDir * refractStrength;

                float4 grabOrig = tex2Dproj(_GrabTex, i.grabPos);

                float4 grabBase = i.grabPos;
                grabBase.xy += offset * _GrabTex_TexelSize.xy * grabBase.w;

                float2 blurStep = _GrabTex_TexelSize.xy * _BlurRadius * grabBase.w / 2;

                float4 bg = tex2Dproj(_GrabTex, grabBase);
                bg += tex2Dproj(_GrabTex, grabBase + float4( blurStep.x, 0, 0, 0));
                bg += tex2Dproj(_GrabTex, grabBase + float4(-blurStep.x, 0, 0, 0));
                bg += tex2Dproj(_GrabTex, grabBase + float4(0,  blurStep.y, 0, 0));
                bg += tex2Dproj(_GrabTex, grabBase + float4(0, -blurStep.y, 0, 0));
                bg *= 0.2;

                float3 glassColor = bg.rgb * _GlassTint.rgb * _GlassBrightness;

                float2 grad2 = float2(ddx(dist), ddy(dist));
                float3 normal = normalize(float3(grad2, 0.5));
                float3 lightDir = normalize(float3(-0.4, 0.9, 1.0));
                float spec = pow(saturate(dot(normal, lightDir)), 16.0);

                float highlightWidth = max(_HighlightWidth * maxRadius, 1e-4);
                float edgeMask = 1.0 - saturate(abs(dist) / highlightWidth);
                edgeMask = saturate(edgeMask);
                edgeMask *= edgeMask;
                float highlight = spec * edgeMask * _HighlightIntensity * shape;

                glassColor += highlight;

                float3 baseBg = grabOrig.rgb;
                float3 effectBg = glassColor;
                float effectStrength = _Opacity;
                float3 resultRgb = lerp(baseBg, effectBg, effectStrength);

                float glassAlpha = _GlassAlpha;
                float surfaceAlpha = shape * texCol.a * glassAlpha;

                float4 finalColor = float4(resultRgb, 1);
                finalColor.rgb *= finalColor.a;

                #ifdef UNITY_UI_ALPHACLIP
                    if (_UseUIAlphaClip > 0.5)
                        clip(finalColor.a - 0.001);
                #endif

                return finalColor + (edgeMask * _EdgeAlpha) + shape * surfaceAlpha;
            }
            ENDCG
        }
    }
}
