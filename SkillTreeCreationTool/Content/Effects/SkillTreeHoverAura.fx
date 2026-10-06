#if OPENGL
#define SV_POSITION POSITION
#define SV_Target COLOR0
#define PS_SHADERMODEL ps_3_0
#else
#define SV_POSITION SV_POSITION
#define SV_Target SV_Target
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct VertexShaderOutput
{
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float iTime;
float4 ringTimeOffsets;

float Ring(float distance, float radius, float wave, float coreWidth, float hazeWidth)
{
    float ringDistance = abs(distance - (radius + wave));
    float core = exp2(-coreWidth * ringDistance);
    float haze = exp2(-hazeWidth * ringDistance);
    return core + haze * 0.5;
}

float WrappedDistance(float a, float b)
{
    float distance = abs(a - b);
    return min(distance, 1.0 - distance);
}

float TravelingPulse(float circlePosition, float pulsePosition, float coreWidth, float fadeWidth)
{
    float distance = WrappedDistance(circlePosition, pulsePosition);
    return 1.0 - smoothstep(coreWidth, coreWidth + fadeWidth, distance);
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
    float2 position = input.TextureCoordinates - 0.5;
    float angle = atan2(position.y, position.x);
    float distance = length(position);
    float timeA = iTime * 0.55 + ringTimeOffsets.x;
    float timeB = iTime * 0.55 + ringTimeOffsets.y;
    float timeC = iTime * 0.55 + ringTimeOffsets.z;
    float timeD = iTime * 0.55 + ringTimeOffsets.w;

    float sharedRadius = 0.37;
    float waveA = sin(angle * 5.0 - timeA * 0.8) * 0.012 +
        sin(angle * 11.0 + timeA * 1.5) * 0.007;
    float waveB = sin(angle * 8.0 + timeB * 1.7 + 2.1) * 0.032 +
        sin(angle * 16.0 - timeB * 0.55 + 0.8) * 0.018;
    float waveC = sin(angle * 17.0 - timeC * 2.6 + 4.2) * 0.085 +
        sin(angle * 29.0 + timeC * 1.7) * 0.050 +
        sin(angle * 43.0 - timeC * 1.1) * 0.025;
    float waveD = sin(angle * 21.0 + timeD * 3.4 + 1.1) * 0.240 +
        sin(angle * 37.0 - timeD * 2.1 + 3.7) * 0.135 +
        sin(angle * 59.0 + timeD * 1.3) * 0.070 +
        sin(angle * 83.0 - timeD * 2.8) * 0.030;

    float ringA = Ring(distance, sharedRadius, waveA, 220.0, 75.0);
    float ringB = Ring(distance, sharedRadius, waveB, 240.0, 82.0);
    float ringC = Ring(distance, sharedRadius, waveC, 650.0, 200.0);
    float ringD = Ring(distance, sharedRadius, waveD, 720.0, 220.0);
    float circlePosition = frac((angle + 3.141593) / 6.283185);
    float travelingA = TravelingPulse(circlePosition, frac(iTime * 0.16 + ringTimeOffsets.x), 0.025, 0.11);
    float travelingB = TravelingPulse(circlePosition, frac(iTime * 0.22 + ringTimeOffsets.y + 0.35), 0.020, 0.09);
    float pulseA = 0.22 + travelingA * 0.78;
    float pulseB = 0.18 + travelingB * 0.82;
    float wispBreakup = saturate(sin(angle * 9.0 - timeC * 2.0) * 0.5 + 0.5);
    float pulseC = (0.03 + 0.22 * (sin(angle * 3.0 - timeC * 1.4 + 3.6) * 0.5 + 0.5)) * wispBreakup;
    float pulseD = (0.02 + 0.14 * (sin(angle * 7.0 + timeD * 2.2) * 0.5 + 0.5)) *
        saturate(sin(angle * 12.0 - timeD * 2.8) * 0.5 + 0.5);
    float alpha = saturate((ringA * pulseA * 0.38) + (ringB * pulseB * 0.45) +
        (ringC * pulseC * 0.20) + (ringD * pulseD * 0.14));
    float brightness = saturate(ringA * pulseA * 0.50 + ringB * pulseB * 0.65 +
        ringC * pulseC * 0.12 + ringD * pulseD * 0.08);

    return float4(input.Color.rgb * brightness, input.Color.a * alpha);
}

technique SkillTreeHoverAura
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
