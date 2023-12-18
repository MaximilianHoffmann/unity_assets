Shader "Custom/HexagonalGrid"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GridColor ("Grid Color", Color) = (1,1,1,1)
        _GridSize ("Grid Size", Float) = 10.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles
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
            float4 _MainTex_ST;
            float4 _GridColor;
            float _GridSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            float hexagonalPattern(float2 uv)
            {
                uv *= _GridSize;
                float3 hex = float3(1.0, 2.0 / sqrt(3.0), 0.5);
                float2x3 uvTransform = float2x3(hex.x, hex.y, -hex.z, hex.z, -hex.z, hex.z);
                float3 grid = frac(mul(uv.xyxy, uvTransform));
                grid = min(grid, 1.0 - grid);
                return min(min(grid.x + grid.y, grid.y + grid.z), grid.z + grid.x);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float gridValue = hexagonalPattern(i.uv);
                float4 color = tex2D(_MainTex, i.uv);
                color.rgb = lerp(color.rgb, _GridColor.rgb, gridValue);
                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
