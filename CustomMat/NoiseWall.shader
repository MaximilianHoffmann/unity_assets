Shader "Custom/NoiseWall"
{
    Properties
    {
        _TimeStep("Time Step", Float) = 1.0
        _GridSize("Grid Size", Float) = 1.0 // Property for grid size
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
                float localY : TEXCOORD1; // Added for local y-coordinate
            };

            float _TimeStep;
            float _GridSize; // Variable for grid size

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Calculate angle around y-axis
                float angle = atan2(v.vertex.z, v.vertex.x);
                o.angle = angle;

                // Pass the local y-coordinate
                o.localY = v.vertex.y; 

                return o;
            }

            float rand(float2 p) 
            {
                p = 50.0 * frac(p * 0.3183099 + float2(sin(p.x), cos(p.y)));
                return  frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453123);
            }


            fixed4 frag (v2f i) : SV_Target
            {
                // Determine color based on angle and time
                int segment = int(floor((i.angle + 3.14159) / (3.14159 / (1.0/_GridSize)))); // 20 segments for 360 degrees
                float timeScaled = _Time.y / _TimeStep;

                // Calculate vertical segment based on local y-coordinate and grid size
                int verticalSegment = int(floor((i.localY +1)/ _GridSize));

                // Combine angular and vertical segment information
                float combinedSegments = float(0.00001*segment + 0.001*verticalSegment);//+0.001*timeScaled);
                float randomValue=rand(combinedSegments);
                fixed4 color = fixed4(randomValue, randomValue, randomValue, 1.0);
                // float colorValue = (fmod(combinedSegments, 2.0) == 0.0) ? 1.0 : 0.0;

                // fixed4 color = fixed4(colorValue, colorValue, colorValue, 1.0);
                return color;
            }
            ENDCG
        }
    }
}


