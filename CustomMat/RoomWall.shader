Shader "Custom/RoomWall"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (0.2, 0.2, 0.2, 1.0)
        _Color2 ("Color 2", Color) = (0.8, 0.8, 0.8, 1.0)
        _NoiseScale ("Noise Scale", Float) = 1.0
        _UseBandNoise ("Use Band Noise", Float) = 0.0
        _Seed ("Random Seed", Float) = 0.0
        _BandFrequencies ("Band Frequencies (cycles/m)", Vector) = (1.0, 2.4, 6.0, 0.0)
        _BandAmplitudes ("Band Amplitudes", Vector) = (0.6, 0.8, 0.5, 0.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Back
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
                float4 pos : SV_POSITION;
                float2 worldPos : TEXCOORD0;
            };

            float4 _Color1;
            float4 _Color2;
            float _NoiseScale;
            float _UseBandNoise;
            float _Seed;
            float4 _BandFrequencies;
            float4 _BandAmplitudes;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Get world position for isotropic spatial noise
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                // Use XY plane for vertical walls
                // This gives isotropic noise in world space units (meters)
                o.worldPos = worldPos.xy;

                return o;
            }

            // Hash function for pseudo-random numbers
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Perlin-like noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                // Smooth interpolation
                f = f * f * (3.0 - 2.0 * f);

                // Four corners of the cell
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                // Bilinear interpolation
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 1/f noise (pink noise) - sum of octaves with decreasing amplitude and increasing frequency
            float pinkNoise(float2 p)
            {
                float total = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                float maxValue = 0.0;

                // Sum 6 octaves of noise with 1/f amplitude falloff
                for (int i = 0; i < 6; i++)
                {
                    total += noise(p * frequency + _Seed) * amplitude;
                    maxValue += amplitude;

                    amplitude *= 0.5;  // 1/f amplitude falloff
                    frequency *= 2.0;   // Octave frequency doubling
                }

                // Normalize to [0, 1]
                return total / maxValue;
            }

            float bandLimitedNoise(float2 p)
            {
                float3 freqs = _BandFrequencies.xyz;
                float3 amps = _BandAmplitudes.xyz;

                float total = 0.0;
                float weightSum = 0.0;

                for (int idx = 0; idx < 3; idx++)
                {
                    float freq = freqs[idx];
                    float amp = amps[idx];
                    float scaledFreq = max(freq, 0.001);
                    float2 offset = float2(idx * 17.27 + _Seed, idx * 41.13 - _Seed);
                    float bandSample = noise(p * scaledFreq + offset);
                    total += bandSample * amp;
                    weightSum += amp;
                }

                return total / max(weightSum, 0.0001);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Use world space coordinates for isotropic noise
                float2 uv = i.worldPos * _NoiseScale;

                float noiseValue;
                if (_UseBandNoise > 0.5)
                {
                    noiseValue = bandLimitedNoise(uv);
                }
                else
                {
                    noiseValue = pinkNoise(uv);
                }

                // Interpolate between Color1 and Color2 based on noise
                fixed4 color = lerp(_Color1, _Color2, noiseValue);

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
