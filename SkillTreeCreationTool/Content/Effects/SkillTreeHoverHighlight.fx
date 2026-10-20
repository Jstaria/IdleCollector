#if OPENGL
#define SV_Target COLOR0
#define PS_SHADERMODEL ps_3_0
#else
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct VertexShaderOutput
{
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

sampler2D textureSampler : register(s0);
texture hoverMask;
sampler2D maskSampler : register(s1) = sampler_state
{
    Texture = <hoverMask>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
float2 texelSize;
float highlightStrength;
float blurRadius;

float BlurWeight(int offset)
{
    return offset == 0 ? 20.0 : abs(offset) == 1 ? 15.0 : abs(offset) == 2 ? 6.0 : 1.0;
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
    float2 uv = input.TextureCoordinates;
    float4 source = tex2D(textureSampler, uv);
    float mask = tex2D(maskSampler, uv).a;
    float blurredMask = 0.0;
    [unroll]
    for (int y = -3; y <= 3; y++)
    {
        [unroll]
        for (int x = -3; x <= 3; x++)
            blurredMask += tex2D(maskSampler, uv + float2(x, y) * texelSize * blurRadius * .75).a * BlurWeight(x) * BlurWeight(y);
    }
    float glow = saturate(blurredMask / 4096.0 - mask) * highlightStrength * .8;
    source.rgb = lerp(source.rgb, float3(1, 1, 1), glow);
    source.a = max(source.a, glow);
    return source * input.Color;
}

technique SkillTreeHoverHighlight
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
