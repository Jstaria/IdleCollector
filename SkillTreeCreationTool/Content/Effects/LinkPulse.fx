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
float pulseDelay;

float4 MainPS(VertexShaderOutput input) : SV_Target
{
    const float pulseTravelTime = 3.0;
    const float pulseCycleTime = 6.0;

    float localTime = fmod(iTime - pulseDelay, pulseCycleTime);
    float pulsePosition = frac(localTime / pulseTravelTime);
    float isTraveling = step(localTime, pulseTravelTime);

    float alongLine = abs(input.TextureCoordinates.x - pulsePosition);

    float waveCenter = 0.5 +
        sin(input.TextureCoordinates.x * 28.0 - iTime * 4.0) * 0.12;

    float acrossLine = abs(input.TextureCoordinates.y - waveCenter);

    // Fade the pulse in at the beginning and out at the end
    float pulseFadeIn = smoothstep(0.0, 0.5, localTime);
    float pulseFadeOut = 1.0 - smoothstep(2.5, 3.0, localTime);
    float pulseFade = pulseFadeIn * pulseFadeOut;

    float pulse = exp2(-11.0 * alongLine) *
                  exp2(-11.0 * acrossLine) *
                  isTraveling *
                  pulseFade;

    float centerDistance = abs(input.TextureCoordinates.y - 0.5) * 2.0;

    float sideFade = exp2(-6.0 * centerDistance);
    float centerGlow = exp2(-14.0 * centerDistance);

    float endFade = sin(input.TextureCoordinates.x * 3.141593);

    float brightness = (0.18 + pulse * 1.35) *
                       (0.4 + 1.35 * centerGlow);

    return input.Color * brightness * sideFade * endFade;
}

technique LinkPulse
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
