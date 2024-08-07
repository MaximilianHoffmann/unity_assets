Shader "Custom/GratingTriangle"
{
    Properties
    {
        // Zone 1
        _BarFrequency1 ("Bar Frequency Zone 1", Range(1, 100)) = 9
        _BarWidth1 ("Bar Width Zone 1", Range(0.01, 1)) = 0.5
        _BarColor1 ("Bar Color Zone 1", Color) = (0,0,1,1)
        _BackgroundColor1 ("Background Color Zone 1", Color) = (0.5,0.5,0.5,1)
        _EffectStart1 ("Effect Start Zone 1", Range(0, 1)) = 0.0
        _EffectEnd1 ("Effect End Zone 1", Range(0, 1)) = 0.5
        _GratingRotation1 ("Grating Rotation Zone 1", Range(0, 360)) = 0.0

        // Zone 2
        _BarFrequency2 ("Bar Frequency Zone 2", Range(1, 100)) = 9
        _BarWidth2 ("Bar Width Zone 2", Range(0.01, 1)) = 0.5
        _BarColor2 ("Bar Color Zone 2", Color) = (0,0,1,1)
        _BackgroundColor2 ("Background Color Zone 2", Color) = (0.5,0.5,0.5,1)
        _EffectStart2 ("Effect Start Zone 2", Range(0, 1)) = 0.5
        _EffectEnd2 ("Effect End Zone 2", Range(0, 1)) = 0.75
        _GratingRotation2 ("Grating Rotation Zone 2", Range(0, 360)) = 45.0

        // Zone 3
        _BarFrequency3 ("Bar Frequency Zone 3", Range(1, 100)) = 9
        _BarWidth3 ("Bar Width Zone 3", Range(0.01, 1)) = 0.5
        _BarColor3 ("Bar Color Zone 3", Color) = (0,0,1,1)
        _BackgroundColor3 ("Background Color Zone 3", Color) = (0.5,0.5,0.5,1)
        _EffectStart3 ("Effect Start Zone 3", Range(0, 1)) = 0.75
        _EffectEnd3 ("Effect End Zone 3", Range(0, 1)) = 1.0
        _GratingRotation3 ("Grating Rotation Zone 3", Range(0, 360)) = 90.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Define properties for three zones
            float _BarFrequency1;
            float _BarWidth1;
            float4 _BarColor1;
            float4 _BackgroundColor1;
            float _EffectStart1;
            float _EffectEnd1;
            float _GratingRotation1;

            float _BarFrequency2;
            float _BarWidth2;
            float4 _BarColor2;
            float4 _BackgroundColor2;
            float _EffectStart2;
            float _EffectEnd2;
            float _GratingRotation2;

            float _BarFrequency3;
            float _BarWidth3;
            float4 _BarColor3;
            float4 _BackgroundColor3;
            float _EffectStart3;
            float _EffectEnd3;
            float _GratingRotation3;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float4 uv : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Convert rotation from degrees to radians
                float radians1 = _GratingRotation1 * 3.14159265359 / 180.0;
                float radians2 = _GratingRotation2 * 3.14159265359 / 180.0;
                float radians3 = _GratingRotation3 * 3.14159265359 / 180.0;

                bool withinZone1 = i.uv.y >= _EffectStart1 && i.uv.y <= _EffectEnd1;
                bool withinZone2 = i.uv.y >= _EffectStart2 && i.uv.y <= _EffectEnd2;
                bool withinZone3 = i.uv.y >= _EffectStart3 && i.uv.y <= _EffectEnd3;

                // Calculate the rotated UV coordinates for each zone
                float2 uv_centered = i.uv - 0.5;

                // Zone 1
                float2 uv_rotated1;
                uv_rotated1.x = uv_centered.x * cos(radians1) - uv_centered.y * sin(radians1);
                uv_rotated1.y = uv_centered.x * sin(radians1) + uv_centered.y * cos(radians1);
                uv_rotated1 += 0.5;

                // Zone 2
                float2 uv_rotated2;
                uv_rotated2.x = uv_centered.x * cos(radians2) - uv_centered.y * sin(radians2);
                uv_rotated2.y = uv_centered.x * sin(radians2) + uv_centered.y * cos(radians2);
                uv_rotated2 += 0.5;

                // Zone 3
                float2 uv_rotated3;
                uv_rotated3.x = uv_centered.x * cos(radians3) - uv_centered.y * sin(radians3);
                uv_rotated3.y = uv_centered.x * sin(radians3) + uv_centered.y * cos(radians3);
                uv_rotated3 += 0.5;

                // Adjust for UV wrapping (0 to 1 range)
                uv_rotated1.x = frac(uv_rotated1.x);
                uv_rotated2.x = frac(uv_rotated2.x);
                uv_rotated3.x = frac(uv_rotated3.x);

                // Calculate bar patterns for each zone
                float barPattern1 = withinZone1 ? abs(frac(uv_rotated1.x * _BarFrequency1) - 0.5) / (0.5 * _BarWidth1) : 0.0;
                float barPattern2 = withinZone2 ? abs(frac(uv_rotated2.x * _BarFrequency2) - 0.5) / (0.5 * _BarWidth2) : 0.0;
                float barPattern3 = withinZone3 ? abs(frac(uv_rotated3.x * _BarFrequency3) - 0.5) / (0.5 * _BarWidth3) : 0.0;

                // Determine color based on the zone
                half4 finalColor;
                if (withinZone1 && !withinZone2 && !withinZone3)
                {
                    finalColor = lerp(_BackgroundColor1, _BarColor1, step(barPattern1, 1.0));
                }
                else if (withinZone2 && !withinZone3)
                {
                    finalColor = lerp(_BackgroundColor2, _BarColor2, step(barPattern2, 1.0));
                }
                else if (withinZone3)
                {
                    finalColor = lerp(_BackgroundColor3, _BarColor3, step(barPattern3, 1.0));
                }
                else
                {
                    finalColor = _BackgroundColor1; // Default to the background color of Zone 1
                }

                // Multiply by texture color
                finalColor.rgb *= tex2D(_MainTex, i.uv).rgb;

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}