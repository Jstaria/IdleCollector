#if OPENGL
#define SV_POSITION POSITION
#define PS_SHADERMODEL ps_3_0
#else
#define SV_POSITION SV_POSITION
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

sampler2D textureSampler;
sampler2D blurMaskTexture;

float2 texelSize;
float maskCutoff;
int maxBlurRadius;

float4 MaskedBlurPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float4 original = tex2D(textureSampler, uv) * input.Color;
    float maskValue = dot(tex2D(blurMaskTexture, uv).rgb, float3(0.2126, 0.7152, 0.0722));

    if (maskValue >= maskCutoff)
        return original;

    float blurAmount = saturate((maskCutoff - maskValue) / max(maskCutoff, 0.0001));
    const int shaderMaxRadius = 8;
    int radius = clamp((int)ceil(blurAmount * min(maxBlurRadius, shaderMaxRadius)), 1, shaderMaxRadius);
    float sigma = max(0.5, radius * 0.5);
    float3 blurredColor = 0.0;
    float totalWeight = 0.0;

    for (int row = -shaderMaxRadius; row <= shaderMaxRadius; row++)
    {
        for (int column = -shaderMaxRadius; column <= shaderMaxRadius; column++)
        {
            if (abs(row) <= radius && abs(column) <= radius)
            {
                float2 offset = float2(column, row);
                float weight = exp(-dot(offset, offset) / (2.0 * sigma * sigma));
                blurredColor += tex2D(textureSampler, uv + offset * texelSize).rgb * weight;
                totalWeight += weight;
            }
        }
    }

    float3 result = lerp(original.rgb, blurredColor / totalWeight, blurAmount);
    return float4(result, original.a);
}

technique MaskedBlur
{
    pass
    {
        PixelShader = compile PS_SHADERMODEL MaskedBlurPS();
    }
}
