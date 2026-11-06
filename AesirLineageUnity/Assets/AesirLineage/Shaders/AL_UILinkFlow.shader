// Star map links: a soft line (texture U runs across the width) with light flowing along it.
// TEXCOORD1.x is the distance along the link in canvas units and TEXCOORD1.y how much it flows
// (0 for links not taken), filled in by LinkGraphic; the canvas must pass TexCoord1.
Shader "AesirLineage/UI Link Flow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Falloff Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Spacing ("Pulse Spacing", Float) = 90
        _Speed ("Pulse Speed", Float) = 70
        _Sharpness ("Pulse Sharpness", Float) = 6
        _Boost ("Pulse Boost", Float) = 1.6

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

        Stencil
        {
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
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 flow : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 flow : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Spacing;
            float _Speed;
            float _Sharpness;
            float _Boost;
            float4 _ClipRect;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.flow = v.flow;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;
                // Pulses travel outward from the root along every taken link.
                float phase = (i.flow.x - _Time.y * _Speed) / max(_Spacing, 1.0);
                float pulse = pow(saturate(0.5 + 0.5 * sin(phase * 6.2831853)), _Sharpness);
                c.rgb *= 1.0 + i.flow.y * pulse * _Boost;
                c.a *= 1.0 + i.flow.y * pulse * 0.5;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
