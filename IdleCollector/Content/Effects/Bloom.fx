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
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};


// SpriteBatch automatically supplies the source texture.
sampler2D textureSampler;

// Original unmodified scene.
// Used only during the final BLOOM composite.
sampler2D originalTexture;


// ============================================================
// PARAMETERS
// ============================================================

float bloomThreshold;
float bloomStrength;
float3 bloomTint;
float2 blurTexelSize;
int blurKernelRadius;
float2 downsampleTexelSize;
// Fixed-size arrays keep the per-pixel hue comparisons predictable. For a larger
// palette, use a lookup texture rather than increasing uniform-array work here.
float3 bloomExcludedColors[4];
int bloomExcludedColorCount;
float bloomExclusionTolerance;
float bloomExclusionSoftness;
float bloomExclusionStrength;
float3 bloomBoostColors[4];
int bloomBoostColorCount;
float bloomBoostTolerance;
float bloomBoostSoftness;
float bloomBoostStrength;


// ============================================================
// EXTRACT
// ============================================================
//
// Keeps only pixels whose luminance reaches bloomThreshold.
//
// Below threshold:
//     RGB = 0
//
// At/above threshold:
//     RGB = original
//
// ============================================================

float GetBloomColorExclusionMask(float3 color)
{
    float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
    float3 chroma = color - luminance;
    float chromaLength = length(chroma);

    if (chromaLength < 0.01)
        return 1.0;

    float3 normalizedChroma = chroma / chromaLength;
    float exclusion = 0.0;

    for (int i = 0; i < 4; i++)
    {
        if (i < bloomExcludedColorCount)
        {
            float3 excludedColor = bloomExcludedColors[i];
            float excludedLuminance = dot(excludedColor, float3(0.2126, 0.7152, 0.0722));
            float3 excludedChroma = excludedColor - excludedLuminance;
            float excludedChromaLength = length(excludedChroma);

            if (excludedChromaLength < 0.01)
                continue;

            float similarity = dot(normalizedChroma, excludedChroma / excludedChromaLength);

            exclusion = max(exclusion, smoothstep(
                bloomExclusionTolerance - bloomExclusionSoftness,
                bloomExclusionTolerance + bloomExclusionSoftness,
                similarity));
        }
    }

    return lerp(1.0, bloomExclusionStrength, exclusion);
}


float GetBloomColorBoostMultiplier(float3 color)
{
    float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
    float3 chroma = color - luminance;
    float chromaLength = length(chroma);

    if (chromaLength < 0.01)
        return 1.0;

    float3 normalizedChroma = chroma / chromaLength;
    float boost = 0.0;

    for (int i = 0; i < 4; i++)
    {
        if (i < bloomBoostColorCount)
        {
            float3 boostColor = bloomBoostColors[i];
            float boostLuminance = dot(boostColor, float3(0.2126, 0.7152, 0.0722));
            float3 boostChroma = boostColor - boostLuminance;
            float boostChromaLength = length(boostChroma);

            if (boostChromaLength < 0.01)
                continue;

            float similarity = dot(normalizedChroma, boostChroma / boostChromaLength);

            boost = max(boost, smoothstep(
                bloomBoostTolerance - bloomBoostSoftness,
                bloomBoostTolerance + bloomBoostSoftness,
                similarity));
        }
    }

    return lerp(1.0, bloomBoostStrength, boost);
}


float4 ExtractPS(VertexShaderOutput input) : COLOR
{
    float4 color =
        tex2D(
            textureSampler,
            input.TextureCoordinates
        ) * input.Color;

    float luminance =
        dot(
            color.rgb,
            float3(
                0.2126,
                0.7152,
                0.0722
            )
        );

    float brightnessMask =
        step(
            bloomThreshold,
            luminance
    );
    float colorMask = GetBloomColorExclusionMask(color.rgb);
    float colorBoost = GetBloomColorBoostMultiplier(color.rgb);

    return float4(
        color.rgb * brightnessMask * colorMask * colorBoost,
        1.0
    );
}


float4 BlurPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    const int maxKernelRadius = 4;
    int radius = min(max(blurKernelRadius, 0), maxKernelRadius);
    float sigma = max(0.5, radius * 0.5);
    float3 color = 0.0;
    float totalWeight = 0.0;

    for (int row = -maxKernelRadius; row <= maxKernelRadius; row++)
    {
        for (int column = -maxKernelRadius; column <= maxKernelRadius; column++)
        {
            if (abs(row) <= radius && abs(column) <= radius)
            {
                float2 offset = float2(column, row);
                float weight = exp(-dot(offset, offset) / (2.0 * sigma * sigma));

                color += tex2D(textureSampler, uv + offset * blurTexelSize).rgb * weight;
                totalWeight += weight;
            }
        }
    }

    return float4(color / totalWeight, 1.0);
}


float KarisWeight(float3 color)
{
    float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
    return rcp(1.0 + luminance);
}


float4 DownsamplePS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float2 x = float2(downsampleTexelSize.x, 0.0);
    float2 y = float2(0.0, downsampleTexelSize.y);
    float3 color = 0.0;
    float weight = 0.0;

    float3 sample = tex2D(textureSampler, uv).rgb;
    float sampleWeight = 0.125 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv - x - y).rgb;
    sampleWeight = 0.125 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv + x - y).rgb;
    sampleWeight = 0.125 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv - x + y).rgb;
    sampleWeight = 0.125 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv + x + y).rgb;
    sampleWeight = 0.125 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv - 2.0 * x - 2.0 * y).rgb;
    sampleWeight = 0.03125 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv + 2.0 * x - 2.0 * y).rgb;
    sampleWeight = 0.03125 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv - 2.0 * x + 2.0 * y).rgb;
    sampleWeight = 0.03125 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv + 2.0 * x + 2.0 * y).rgb;
    sampleWeight = 0.03125 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv - 2.0 * x).rgb;
    sampleWeight = 0.0625 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv + 2.0 * x).rgb;
    sampleWeight = 0.0625 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv - 2.0 * y).rgb;
    sampleWeight = 0.0625 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    sample = tex2D(textureSampler, uv + 2.0 * y).rgb;
    sampleWeight = 0.0625 * KarisWeight(sample);
    color += sample * sampleWeight;
    weight += sampleWeight;

    return float4(color / max(weight, 0.0001), 1.0);
}


// ============================================================
// BLOOM COMPOSITE
// ============================================================
//
// textureSampler = blurred bloom
// originalTexture = original scene
//
// Final:
//     original + bloom
//
// ============================================================

float4 BloomPS(VertexShaderOutput input) : COLOR
{
    float3 original =
        tex2D(originalTexture, input.TextureCoordinates).rgb;

    float3 bloom =
        tex2D(textureSampler, input.TextureCoordinates).rgb;

    float3 combined =
        original +
        bloom * bloomTint * bloomStrength;

    return float4(combined, 1.0);
}


// ============================================================
// TECHNIQUES
// ============================================================
//
// IMPORTANT:
// Each technique has exactly ONE pass.
//
// C# selects:
//     effect.Techniques["Extract"]
//     effect.Techniques["Bloom"]
//
// ============================================================

technique Extract
{
    pass
    {
        PixelShader =
            compile PS_SHADERMODEL ExtractPS();
    }
}
technique Blur
{
    pass
    {
        PixelShader =
            compile PS_SHADERMODEL BlurPS();
    }
}
technique Downsample
{
    pass
    {
        PixelShader =
            compile PS_SHADERMODEL DownsamplePS();
    }
}
technique Bloom
{
    pass
    {
        PixelShader =
            compile PS_SHADERMODEL BloomPS();
    }
}
