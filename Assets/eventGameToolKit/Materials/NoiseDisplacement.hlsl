#ifndef NOISE_DISPLACEMENT_INCLUDED
#define NOISE_DISPLACEMENT_INCLUDED

// Shared displacement function for all passes.
// Requires these to be declared in the CBUFFER and as textures before including:
//   float4 _DisplacementTiling
//   float4 _DisplacementScrollSpeed
//   float  _DisplacementStrength
//   float  _DisplacementOffset
//   TEXTURE2D(_DisplacementMap) + SAMPLER(sampler_DisplacementMap)
//
// Only executes when _DISPLACEMENT_ON keyword is enabled.
// When the keyword is off, this compiles to nothing — zero cost.

float3 ApplyNoiseDisplacement(float3 positionOS, float3 normalOS, float2 uv)
{
    #ifdef _DISPLACEMENT_ON
        float2 dispUV = uv * _DisplacementTiling.xy + _DisplacementTiling.zw;
        dispUV += _Time.y * _DisplacementScrollSpeed.xy;

        float noise = SAMPLE_TEXTURE2D_LOD(
            _DisplacementMap, sampler_DisplacementMap, dispUV, 0).r;

        float displacement = (noise + _DisplacementOffset) * _DisplacementStrength;
        positionOS += normalOS * displacement;
    #endif

    return positionOS;
}

#endif // NOISE_DISPLACEMENT_INCLUDED
