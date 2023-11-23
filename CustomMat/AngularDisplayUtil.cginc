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
float _GratingFrequency;
float _GratingSpeed;
float _CylinderRadius;
float4 _Color1;
float4 _Color2;
float _CapTheshold;
float _Ratio;




       