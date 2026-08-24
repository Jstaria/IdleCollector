#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct VertexShaderOutput
{
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

// Bound automatically by SpriteBatch. It is the source texture for each pass.
sampler2D textureSampler;

// Set this to the unmodified scene texture before applying the BLOOM pass.
sampler2D originalTexture;

float bloomThreshold;
float bloomStrength;
float3 bloomTint;

float4 ExtractPS(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(textureSampler, input.TextureCoordinates) * input.Color;
    float brightness = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
    float bloomMask = step(bloomThreshold, brightness);

    return float4(color.rgb * bloomMask, color.a * bloomMask);
}

float4 BloomPS(VertexShaderOutput input) : COLOR
{
    float4 bloom = tex2D(textureSampler, input.TextureCoordinates);
    float4 original = tex2D(originalTexture, input.TextureCoordinates) * input.Color;

    return float4(original.rgb + bloom.rgb * bloomTint * bloomStrength, original.a);
}

technique SpriteDrawing
{
    pass EXTRACT
    {
        PixelShader = compile PS_SHADERMODEL ExtractPS();
    }

    pass BLOOM
    {
        PixelShader = compile PS_SHADERMODEL BloomPS();
    }
};
