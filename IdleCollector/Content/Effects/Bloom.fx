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

float2 blurOffset;
float sampleCount;


// Must match Bloom.cs.
#define MAX_BLUR_RADIUS 8


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

    float mask =
        step(
            bloomThreshold,
            luminance
        );

    return float4(
        color.rgb * mask,
        1.0
    );
}


// ============================================================
// BLUR
// ============================================================
//
// Single-pass box blur.
//
// sampleCount controls the radius:
//
// 0 = center pixel
// 1 = -1 to +1
// 2 = -2 to +2
// ...
// 8 = -8 to +8
//
// ============================================================

float4 BlurPS(VertexShaderOutput input) : COLOR
{
    float2 uv =
        input.TextureCoordinates;

    float3 total =
        float3(
            0,
            0,
            0
        );

    float count = 0;


    for (int x = -MAX_BLUR_RADIUS;
         x <= MAX_BLUR_RADIUS;
         x++)
    {
        for (int y = -MAX_BLUR_RADIUS;
             y <= MAX_BLUR_RADIUS;
             y++)
        {
            float dist =
                max(
                    abs((float) x),
                    abs((float) y)
                );

            // Include this sample if it is inside
            // the requested blur radius.
            float weight =
                step(
                    dist - 0.5,
                    sampleCount
                );


            float2 sampleUV =
                uv +
                float2(
                    x * blurOffset.x,
                    y * blurOffset.y
                );


            float4 sampleColor =
                tex2D(
                    textureSampler,
                    sampleUV
                );


            total +=
                sampleColor.rgb *
                weight;

            count += weight;
        }
    }


    total /=
        max(
            count,
            1.0
        );


    return float4(
        total,
        1.0
    );
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
//     effect.Techniques["Blur"]
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


technique Bloom
{
    pass
    {
        PixelShader =
            compile PS_SHADERMODEL BloomPS();
    }
}