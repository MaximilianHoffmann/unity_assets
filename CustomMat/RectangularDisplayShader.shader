Shader "Custom/RectangularDisplayShader"
{
    Properties
    {
        _Color1 ("Color1", Color) = (1, 1, 1, 1)
        _Color2 ("Color2", Color) = (0, 0, 0, 1)
        _VerticalArmThickness ("Vertical Arm Thickness", Range(0, 0.5)) = 0.05
        _HorizontalArmThickness ("Horizontal Arm Thickness", Range(0, 0.5)) = 0.05
        _VerticalArmLength ("Vertical Arm Length", Range(0, 1)) = 0.5
        _HorizontalArmLength ("Horizontal Arm Length", Range(0, 1)) = 0.5
        _Rotation ("Rotation", Range(0, 360)) = 0
        _GratingDensity ("Grating Density", Range(0, 100)) = 20
        [MaterialToggle] _SwitchOnGrating ( "Switch On Grating", Float ) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha // Enable transparency
        Cull Off // Turning off culling

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

            float4 _Color1;
            float4 _Color2;
            float _VerticalArmThickness;
            float _HorizontalArmThickness;
            float _VerticalArmLength;
            float _HorizontalArmLength;
            float _Rotation;
            float _GratingDensity;
            float _SwitchOnGrating;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half2 uv = i.uv - half2(0.5, 0.5); // Centering the UV coordinates

                // Rotate the UV coordinates if necessary
                float radians = _Rotation * 3.14159 / 180.0;
                half2 rotatedUV;
                rotatedUV.x = uv.x * cos(radians) - uv.y * sin(radians);
                rotatedUV.y = uv.x * sin(radians) + uv.y * cos(radians);
                uv = rotatedUV + half2(0.5, 0.5); // Re-centering after rotation

                half4 color = _Color1;

                // Check if grating should be displayed
                if (_SwitchOnGrating > 0.5) // A threshold greater than 0.5 indicates the grating is on
                {
                    // Calculate 1D grating effect along the horizontal axis
                    half gratingEffect = sin(uv.x * _GratingDensity * 3.14159 * 2);
                    color = lerp(_Color1, _Color2, step(0.0, gratingEffect)); // Lerp between main and plus color based on grating effect
                }
                else
                {
                    // Draw cross
                    if ((uv.x > 0.5 - _HorizontalArmThickness && uv.x < 0.5 + _HorizontalArmThickness && 
                         uv.y > 0.5 - _HorizontalArmLength * 0.5 && uv.y < 0.5 + _HorizontalArmLength * 0.5) || 
                        (uv.y > 0.5 - _VerticalArmThickness && uv.y < 0.5 + _VerticalArmThickness &&
                         uv.x > 0.5 - _VerticalArmLength * 0.5 && uv.x < 0.5 + _VerticalArmLength * 0.5))
                    {
                        color = _Color2;
                    }
                }

                return color;
            }
            ENDCG
        }
    }
}
