Shader "Custom/Grating"
{
    Properties
    {
        _TimeStep("Time Step", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float angle : TEXCOORD0;
            };

            float _TimeStep;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Calculate angle around y-axis
                float angle = atan2(v.vertex.z, v.vertex.x);
                o.angle = angle;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Determine color based on angle and time
                int segment = int(floor((i.angle + 3.14159) / (3.14159 / 120))); // 18 segments for 360 degrees
                float timeScaled = _Time.y / _TimeStep;
                float colorValue = (segment%2);//frac(sin(segment * 1234.5678 + timeScaled) * 43758.5453);
                fixed4 color = fixed4(colorValue, colorValue, colorValue, 1.0);
                return color;
            }
            ENDCG
        }
    }
}
