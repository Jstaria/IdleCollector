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
    float alongLine = input.TextureCoordinates.x;
    float centerDistance = abs(input.TextureCoordinates.y - 0.5) * 2.0;
    float core = exp2(-18.0 * centerDistance);
    float falloff = exp2(-5.0 * centerDistance);
    float endFade = smoothstep(0.0, 0.12, alongLine) *
        smoothstep(0.0, 0.12, 1.0 - alongLine);
    float shimmer = 0.88 + 0.12 * sin(iTime * 2.0 + pulseDelay + alongLine * 12.0);
    float brightness = (0.22 + 0.72 * core) * falloff * endFade * shimmer;

    return input.Color * brightness;
}

technique ConstellationLink
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
