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
sampler2D lightGeometryTexture;
sampler2D lightColorTexture;
sampler2D lightFalloffTexture;

float2 screenSize;
float darkness;
float3 nightTint;
int lightCount;

float4 NightOverlayPS(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(textureSampler, input.TextureCoordinates) * input.Color;
    float3 ambientColor = color.rgb * nightTint * (1 - darkness);
    float3 litColor = 0;

    for (int i = 0; i < 64; i++)
    {
        if (i < lightCount)
        {
            float lightUv = (i + 0.5) / 64.0;
            float4 geometry = tex2D(lightGeometryTexture, float2(lightUv, 0.5));
            float4 lightColor = tex2D(lightColorTexture, float2(lightUv, 0.5));
            float inverseGraphFloor = clamp(tex2D(lightFalloffTexture, float2(lightUv, 0.5)).x, 0.05, 1.0);
            float2 lightOffset = input.TextureCoordinates - geometry.xy;
            lightOffset.x *= screenSize.x / screenSize.y;
            float distanceToLight = length(lightOffset);
            float falloffDistance = saturate((distanceToLight - geometry.z) / geometry.w);
            float inverseExponent = exp2(1 / max(falloffDistance, inverseGraphFloor));
            float maxInverseExponent = exp2(1 / inverseGraphFloor);
            float lightAmount = saturate((inverseExponent - 2) / (maxInverseExponent - 2));
            litColor += color.rgb * lightColor.rgb * lightColor.a * lightAmount;
        }
    }

    return float4(ambientColor + litColor, color.a);
}

technique NightOverlay
{
    pass
    {
        PixelShader = compile PS_SHADERMODEL NightOverlayPS();
    }
}
