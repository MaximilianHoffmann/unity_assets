Shader "Custom/RoomExitShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color1 ("Grating Color 1", Color) = (1,1,1,1)
        _Color2 ("Grating Color 2", Color) = (0,0,0,1)
        _Color3 ("Grating Color 3", Color) = (0,0,0,1)
        _AngularSize1 ("Size Of Zone1", Range(0,1)) = 0.5
        _AngularSize2 ("Size Of Zone2", Range(0,1)) = 0.5
        _AngularDistance ("Distance Zones", Range(0,1)) = 0.5
        _Offset ("Angular Offset", Range(0,1)) = 0.
        [MaterialToggle] _SwitchOnGrating1 ( "Switch On Grating1", Float ) = 0
        [MaterialToggle] _SwitchOnGrating2 ( "Switch On Grating2", Float ) = 0
        _GratingFrequency1 ("Grating Frequency1", Range(1, 50)) = 10
        _GratingFrequency2 ("Grating Frequency2", Range(1, 50)) = 10
        _GratingOrientation1 ("Grating Orientation1", Range(0, 90)) = 0
        _GratingOrientation2 ("Grating Orientation2", Range(0, 90)) = 0
        _Aspect ("Cylinder Aspect", Range(0.001, 100)) = 1
        }

    SubShader {
        LOD 100

        Pass {
            Name "Green"
            Tags {"RenderType"="Transparent" "Queue" = "Transparent"}
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Properties declaration
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float _Ratio;
            float _Offset;
            float _SwitchOnGrating1;
            float _SwitchOnGrating2;
            float _GratingFrequency1;
            float _GratingFrequency2;
            float _GratingOrientation1;
            float _GratingOrientation2;
            float _AngularSize1;
            float _AngularSize2;
            float _AngularDistance;
            float _MaxDistance;
            float _Aspect;
         

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 localPos : TEXCOORD0; // Changed to localPos
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz; // Passing the local position
                return o;
            }

            half4 frag (v2f i) : SV_Target {

                //if ((_AngularSize1 + _AngularSize2) > 1)
                //{
                //    _AngularSize2=1-_AngularSize1;
                //}
                half4 gratingColor;
                float gratingValue;

                // Map the fragment's local position to a cylindrical coordinate

                
                float angle = atan2(i.localPos.z, i.localPos.x) / (2.0 * 3.14159265) + 0.5; // Changed to localPos
                



                // Calculate rotation in radians
                float rotationRad1 = _GratingOrientation1 * 3.14159265 / 180;
                float rotationRad2 = _GratingOrientation2 * 3.14159265 / 180;
                // Apply 2D rotation matrix
                
                
                float rotatedAngle1 = cos(rotationRad1) * angle - sin(rotationRad1) * i.localPos.y * _Aspect /8;
                float rotatedAngle2 = cos(rotationRad2) * angle - sin(rotationRad2) * i.localPos.y * _Aspect/ 8;
               

                // Now use the rotated angle for the grating calculation, you may need to convert back to [0,1] range if necessary
                float gratingCoord1 = fmod(rotatedAngle1 , 1.0);
                float gratingCoord2 = fmod(rotatedAngle2 , 1.0);


                // Calculate the centers of the two zones
                float centerZone1 = fmod(0.5 + _Offset,1.0); // Let's assume Zone 1 is centered at 0.5
                float centerZone2 = fmod(centerZone1 + _AngularDistance, 1.0); // Wrap around if necessary

                // Determine if the current angle is within Zone 1
                float halfSize1 = _AngularSize1 * 0.5;
                bool inZone1 = angle >= (centerZone1 - halfSize1) && angle <= (centerZone1 + halfSize1);

                // Determine if the current angle is within Zone 2
                float halfSize2 = _AngularSize2 * 0.5;
                bool inZone2 = angle >= (centerZone2 - halfSize2) && angle <= (centerZone2 + halfSize2);

                // Account for wrapping on both zones
                inZone1 = inZone1 || (angle >= (centerZone1 - halfSize1 + 1.0)) || (angle <= (centerZone1 + halfSize1 - 1.0));
                inZone2 = inZone2 || (angle >= (centerZone2 - halfSize2 + 1.0)) || (angle <= (centerZone2 + halfSize2 - 1.0));

                float uv_x= angle;
                gratingColor = _Color3;

                if (inZone1) {
                    if (_SwitchOnGrating1 > 0.5) {
                 
                            gratingValue = frac(gratingCoord1* _GratingFrequency1);
                            gratingColor = lerp(_Color1, _Color2, gratingValue > 0.5 ? 1 : 0);
                        }
                    else {
                        gratingColor = _Color1;
                    }
                            }

                if (inZone2) {   
                    if (_SwitchOnGrating2>0.5) {
                    
                            gratingValue = frac(gratingCoord2* _GratingFrequency2);
                            gratingColor = lerp(_Color1, _Color2, gratingValue > 0.5 ? 1 : 0);
                            
                        }
                        else {
                          gratingColor = _Color3;
                        }
                            }

                   
    
                
                return gratingColor;
            }
            ENDCG
        }
    }
}
