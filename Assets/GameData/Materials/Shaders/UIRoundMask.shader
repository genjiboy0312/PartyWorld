Shader "UI/UIRoundMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _UIRound_Size ("Size", Vector) = (100,100,0,0)
        _UIRound_Radius ("Radius", Float) = 24
        _UIRound_Feather ("Feather", Float) = 1
        _UIRound_FadeDir ("Fade Dir", Float) = 0
        _UIRound_FadeStrength ("Fade Strength", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;
            float4 _UIRound_Size;
            float _UIRound_Radius;
            float _UIRound_Feather;
            float _UIRound_FadeDir;
            float _UIRound_FadeStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float2 uvRect : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.uvRect = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                float2 size = max(_UIRound_Size.xy, 0.0001);
                float2 halfSize = size * 0.5;
                float radius = min(_UIRound_Radius, min(halfSize.x, halfSize.y));

                float2 p = (i.uvRect - 0.5) * size;
                float2 d = abs(p) - (halfSize - radius);
                float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;

                float aa = max(_UIRound_Feather, 0.0001);
                float alphaMask = 1.0 - smoothstep(-aa, aa, dist);
                col.a *= alphaMask;

                float fadeStrength = saturate(_UIRound_FadeStrength);
                if (fadeStrength > 0.0)
                {
                    float dir = _UIRound_FadeDir;
                    float fadeAlpha = 1.0;

                    if (dir < 1.5 && dir > 0.5)           // Left: left pivot, fade to right
                        fadeAlpha = lerp(1.0, 1.0 - fadeStrength, i.uvRect.x);
                    else if (dir < 2.5 && dir > 1.5)      // Right: right pivot, fade to left
                        fadeAlpha = lerp(1.0 - fadeStrength, 1.0, i.uvRect.x);
                    else if (dir < 3.5 && dir > 2.5)      // Top: top pivot, fade to bottom
                        fadeAlpha = lerp(1.0 - fadeStrength, 1.0, i.uvRect.y);
                    else if (dir < 4.5 && dir > 3.5)      // Bottom: bottom pivot, fade to top
                        fadeAlpha = lerp(1.0, 1.0 - fadeStrength, i.uvRect.y);

                    col.a *= saturate(fadeAlpha);
                }

                clip(col.a - 0.001);
                return col;
            }
            ENDCG
        }
    }
}
