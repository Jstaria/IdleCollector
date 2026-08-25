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
technique Bloom
{
    pass
    {
        PixelShader =
            compile PS_SHADERMODEL BloomPS();
    }
}
