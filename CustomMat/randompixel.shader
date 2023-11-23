Shader "Custom/RandomPixelShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Speed", Range(0,5)) = 1.0
        _GridResolutionX ("Grid Resolution X", Range(1, 512)) = 64
        _GridResolutionY ("Grid Resolution Y", Range(1, 512)) = 64
    }

    SubShader
    {
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Speed;
            float _GridResolutionX;
            float _GridResolutionY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float random (float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 _GridResolution = float2(_GridResolutionX, _GridResolutionY);
                float2 quantizedUV = floor(i.uv * _GridResolution) / _GridResolution;
                float noise = random(quantizedUV + _Time.yy * _Speed);
                return half4(float(0.5+0.1*noise), float(0.5+0.1*noise), float(0.5+0.1*noise), 1.);
            }
            ENDCG
        }
    }
}
