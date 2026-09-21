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

sampler2D textureSampler;
float iTime;
float4 shockwaves[8];
int shockwaveCount;

float4 MainPS(VertexShaderOutput input) : SV_Target
{
    float2 uv = input.TextureCoordinates;

    for (int i = 0; i < 8; i++)
    {
        if (i >= shockwaveCount)
            break;

        float age = iTime - shockwaves[i].z;
        float progress = saturate(age / shockwaves[i].w);
        float2 offset = uv - shockwaves[i].xy;
        float distance = length(offset);
        float ringRadius = progress * 0.35;
        float ring = 1.0 - smoothstep(0.0, 0.035, abs(distance - ringRadius));
        float strength = ring * (1.0 - progress) * 0.018;
        uv += normalize(offset + 0.00001) * strength;
    }

    return tex2D(textureSampler, uv) * input.Color;
}

technique SkillTreeShockwave
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
