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

sampler2D textureSampler;

float pixelsX;
float pixelsY;
float pixelation;

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float currentX = input.TextureCoordinates.x * pixelsX;
    float currentY = input.TextureCoordinates.y * pixelsY;
    
    float x = round(currentX / pixelation) * pixelation;
    float y = round(currentY / pixelation) * pixelation;
    
    float2 position = float2(x / pixelsX, y / pixelsY);
    
    return tex2D(textureSampler, position);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};