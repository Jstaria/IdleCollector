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

float4 MainPS(VertexShaderOutput input) : SV_Target
{
    float2 position = input.TextureCoordinates - 0.5;
    float radius = length(position);
    float angle = atan2(position.y, position.x);
    float radialFade = 1.0 - smoothstep(0.12, 0.48, radius);
    float swirl = angle * 6.0 - radius * 31.0 + iTime * 2.8;
    float armA = pow(saturate(sin(swirl) * 0.5 + 0.5), 5.0);
    float armB = pow(saturate(sin(swirl * 1.7 + iTime * 1.1) * 0.5 + 0.5), 9.0);
    float turbulentEdge = sin(angle * 13.0 + radius * 18.0 + iTime * 1.7) * 0.08;
    float vortex = saturate((armA * 0.75 + armB * 0.45 + turbulentEdge) * radialFade);
    float core = exp2(-36.0 * radius);
    float rim = exp2(-100.0 * abs(radius - 0.26)) * 0.35;
    float alpha = saturate(vortex * 0.225 + core * 0.19 + rim * 0.35) * radialFade;

    return float4(input.Color.rgb * (vortex + core * 1.35 + rim), input.Color.a * alpha);
}

technique SkillTreeHoverPortal
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
