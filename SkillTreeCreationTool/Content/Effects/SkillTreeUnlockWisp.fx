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

float iTime;

float Hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float Noise(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
        lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), f.x), f.y);
}

float Wisp(float2 p, float offset, float speed, float width)
{
    float height = saturate((.92 - p.y) / .86);
    float2 flow = float2(p.x * 8.0 + offset, p.y * 7.0 + iTime * speed);
    float turbulence = Noise(flow) * .65 + Noise(flow * 2.3 + offset) * .35;
    float base = sin(offset * 1.7) * .23;
    float bend = sin(height * 9.0 - iTime * speed + offset) * height * .065;
    float center = .5 + base * (1.0 - height * .55) + bend + (turbulence - .5) * height * .12;
    float thickness = width * lerp(1.8, .18, height);
    float filament = exp2(-abs(p.x - center) / thickness);
    float tip = .68 + Noise(float2(offset, iTime * speed * .45)) * .3;
    float fragments = lerp(.65, smoothstep(.22, .7, turbulence), height);
    return filament * fragments * (1.0 - smoothstep(tip - .22, tip, height));
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
    float2 p = input.TextureCoordinates;
    float wisps = Wisp(p, 0.0, 2.2, .025) * .65;
    wisps += Wisp(p, 7.3, 2.8, .018) * .9;
    wisps += Wisp(p, 18.1, 1.9, .022) * .5;
    wisps += Wisp(p, 31.6, 3.1, .014) * .8;
    wisps += Wisp(p, 53.2, 2.5, .02) * .55;
    float baseX = (p.x - .5) / .34;
    float roundedBottom = .72 + .20 * sqrt(saturate(1.0 - baseX * baseX));
    float edgeFade = smoothstep(.02, .12, p.y) * (1.0 - smoothstep(roundedBottom - .04, roundedBottom + .04, p.y));
    edgeFade *= smoothstep(0.0, .08, p.x) * (1.0 - smoothstep(.92, 1.0, p.x));
    float alpha = saturate(wisps * edgeFade * .7) * input.Color.a;
    return float4(alpha, alpha, alpha, alpha);
}

technique SkillTreeUnlockWisp
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
