#if OPENGL
#define SV_POSITION POSITION
#define SV_Target COLOR0
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

#define ITER1 6
#define ITER2 12
#define iResolutionX 480.0f
#define iResolutionY 270.0f

#define GLOW_INTENSITY   0.004   // overall brightness of the halo
#define GLOW_RADIUS      0.08     // how far the glow spreads (lower = tighter)
#define GLOW_FALLOFF     1.5      // shape of falloff: 1.0=linear, 2.0=quadratic, 0.5=very soft
#define GLOW_MAX         0.2     // clamp ceiling so nearby flakes don't blow out
#define CORE_MAX         0.1      // clamp on the core contribution

float iTime;
float2 iPosition;

float rand2(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

float4 MainPS(VertexShaderOutput input) : SV_Target
{
    // Accumulates total brightness of all snow flakes across all layers
    float snow = 0.0;
    
    // Raw UV coords from the vertex shader, in [0,1] range
    float2 uv = input.TextureCoordinates;
    // Flip both axes so (0,0) is bottom-left instead of top-left
    uv = 1.0 - uv;
    
    // Widen the coordinate space horizontally to match the screen's aspect ratio,
    // preventing flakes from looking stretched on non-square screens
    float aspect = iResolutionX / iResolutionY;
    // Re-center around (0,0) and apply aspect — this is the space flakes are positioned in
    float2 p = (uv - 0.5) * float2(aspect, 1.0);
    
    // A simple top-to-bottom gradient: 0.0 at the top, 0.4 at the bottom.
    // Used to tint the sky darker at the top and lighter/warmer at the bottom
    float gradient = (1.0 - uv.y) * 0.4;
    
    // A single static random value per pixel, used only for the fine noise grain overlay.
    // Does NOT affect flake placement — it's purely a screen-space dither
    float random = rand2(uv);
    
    // Accumulates the blue-tinted glow contribution from all flakes.
    // Kept separate from snow so glow can be colored differently (blue tint in the return)
    float glowBlue = 0.0;
    
    // Accumulates the weighted tint color across all layers.
    // Each layer contributes its own front-to-back tint color, weighted by cell size
    float3 tintAccum = float3(0.0, 0.0, 0.0);
    // Accumulates the total weight so we can normalize tintAccum at the end,
    // giving a proper weighted average instead of a raw sum
    float tintWeight = 0.0;
    
    // Outer loop: k is the "pass" index. Each k value is an independent set of layers
    // with different random seeds. Increasing ITER1 adds more overlapping snow passes,
    // making the scene denser without changing the layer depth structure
    for (int k = 0; k < ITER1; k++)
    {
        // Inner loop: i controls depth. i=1 is the closest layer, i=ITER2-1 is furthest.
        // Higher i = smaller flakes, slower movement, darker, more background-like
        for (int i = 1; i < ITER2; i++)
        {
            float fi = float(i); // float version of i, used in all the math below
            
            // Larger fi = larger cell = more space between potential flake positions = sparser layer.
            // Front layers (small fi) have small cells = dense, large flakes.
            // Back layers (large fi) have large cells = sparse, small flakes
            float cellSize = 2.0 + fi * 3.0;
            
            // Base fall speed varies per layer and oscillates slowly over time.
            // The sin term adds a gentle variation so layers don't all move at the same speed.
            // The very small multiplier (0.00008) keeps the oscillation subtle
            float downSpeed = 0.3 + (sin(iTime * 0.4 + float(k + i * 20)) + 1.0) * 0.00008;
            
            // uv2 is the "camera-relative" coordinate for this layer.
            // It combines:
            //   p                          — aspect-corrected screen position
            //   iPosition * parallax       — world scroll offset, scaled per layer for parallax.
            //                               (ITER2 - i) means front layers move more with the camera
            //   horizontal sine wave       — gentle left-right drift, faster/wider for front layers
            //   vertical scroll            — downward fall, faster for front layers (1/fi scaling)
            float2 uv2 =
                p + iPosition * (ITER2 - i) / (ITER2 / 2) +
                float2(
                    // Horizontal drift: unique per k-pass, oscillates with time, dampened for back layers
                    0.01 * sin((iTime + float(k * 6185)) * 0.6 + fi) * (5.0 / fi),
                    // Vertical fall: continuous downward scroll, slower for back layers
                    downSpeed * (iTime + float(k * 1352)) * (1.0 / fi)
                );
            
            // Snap uv2 to the nearest cell grid point.
            // This gives each cell a fixed representative coordinate used to generate
            // that cell's random values — so each "slot" in the grid gets consistent flake data
            float2 uvStep = ceil(uv2 * cellSize - float2(0.5, 0.5)) / cellSize;
            
            // Per-cell random offsets in [-0.5, 0.5].
            // x and y are used to jitter the flake position within its cell,
            // and also feed into the glow/core size calculation.
            // The k*12.0 / k*315.156 seeds ensure different passes produce different flakes
            float x = frac(sin(dot(uvStep, float2(12.9898 + k * 12.0, 78.233 + k * 315.156))) * 43758.5453 + k * 12.0) - 0.5;
            float y = frac(sin(dot(uvStep, float2(62.2364 + k * 23.0, 94.674 + k * 95.0))) * 62159.8432 + k * 12.0) - 0.5;
            
            // These two values animate the flake jitter over time, making flakes wobble.
            // Divided by cellSize so wobble magnitude scales with flake size —
            // front (large) flakes wobble more visibly than back (small) ones
            float randomMagnitude1 = sin(iTime * 2.5) * 0.7 / cellSize;
            float randomMagnitude2 = cos(iTime * 2.5) * 0.7 / cellSize;
            
            // Euclidean distance from the current pixel (uv2) to the wobbled flake center.
            // The wobble offsets (float2(x*sin(y), y) and float2(y, x)) break the circular
            // symmetry slightly, giving flakes a natural irregular shape.
            // Multiplied by 5.0 to scale into a useful 0..1+ range for the falloff formulas
            float d = 5.0 * distance(
                uvStep + float2(x * sin(y), y) * randomMagnitude1 + float2(y, x) * randomMagnitude2,
                uv2
            );
            
            // depthFactor darkens back layers. Ranges from ~1.0 (front, fi=1) to ~0.25 (back, fi=ITER2).
            // Multiply any per-flake contribution by this to make distant flakes dimmer.
            // The 0.75 controls how steep the darkening is — 1.0 would make the back nearly black
            float depthFactor = 1.0 - (fi / float(ITER2)) * 0.75;
            
            // A random value per cell in [0,1), independent of x/y above.
            // Used as a gate: only cells where omiVal < 0.08 actually contain a flake.
            // 0.08 = 8% of cells have flakes. Lower = sparser snow, higher = denser snow
            float omiVal = frac(sin(dot(uvStep, float2(32.4691, 94.615))) * 31572.1684);
            
            // Normalized depth in [0,1]: 0 = front layer, 1 = back layer
            float depth = fi / float(ITER2);
            // Tint color for the closest layers (cool white-blue, like lit snow)
            float3 frontTint = float3(0.8, 0.9, 1.0);
            // Tint color for the furthest layers (deep navy, like distant dark atmosphere)
            float3 backTint = float3(0.05, 0.05, 0.2);
            // Interpolate between front and back tint based on this layer's depth
            float3 layerTint = lerp(frontTint, backTint, depth);
            
            // Accumulate tint weighted by 1/cellSize^2.
            // Smaller cells (back layers) contribute less weight, so front layers
            // dominate the final tint average — matching the visual dominance of foreground layers.
            // 0.4 scales the overall tint contribution strength
            tintAccum += layerTint * (1.0 / (cellSize * cellSize)) * 0.4;
            tintWeight += (1.0 / (cellSize * cellSize)) * 0.4;
            
            // Only process pixels that are near an actual flake (8% of cells)
            if (omiVal < 0.08)
            {
                // The bright solid center of the flake.
                // (x + 1.0) * 0.4 biases brightness based on the cell's random x — some flakes
                // are naturally brighter than others.
                // The clamp computes a sharp falloff from the flake center outward:
                //   1.9 - d * steep_slope — pixels close to center get high values, far ones get 0.
                //   (cellSize / 1.4) makes larger (front) flakes have a steeper edge = crisper look
                float core = (x + 1.0) * 0.4 *
                    clamp(1.9 - d * (15.0 + (x * 6.3)) * (cellSize / 1.4), 0.0, 1.0);
                
                // Normalize d by GLOW_RADIUS so the glow radius is directly tunable in world units.
                // When normalizedDist = 1.0, we're exactly at the glow radius boundary
                float normalizedDist = d / GLOW_RADIUS;
                
                // Inverse power falloff: bright at center, fading out as distance grows.
                // GLOW_INTENSITY = peak brightness multiplier
                // GLOW_FALLOFF = exponent — higher = faster falloff = tighter halo
                // 0.001 prevents divide-by-zero at d=0
                float glow = GLOW_INTENSITY / (pow(normalizedDist, GLOW_FALLOFF) + 0.001);
                
                // Add the clamped glow to the blue channel accumulator.
                // GLOW_MAX prevents a single very-close flake from saturating the whole halo.
                // depthFactor makes back-layer glows dimmer
                glowBlue += clamp(glow, 0.0, GLOW_MAX) * depthFactor;
                
                // Separately clamp glow before adding to snow (white channel).
                // CORE_MAX is usually lower than GLOW_MAX so the white core stays tight
                // while the blue halo can spread wider
                glow = clamp(glow, 0.0, CORE_MAX);
                snow += (core * 2.0 + glow) * depthFactor;
            }
        }
    }
    
    // Final white/grey color from all flake cores and their white glow contribution
    float4 snowColor = float4(snow, snow, snow, 1.0);
    
    // The vertical gradient tinted sky blue — brighter/bluer at bottom, dark at top
    float4 skyColor = gradient * float4(0.4, 0.8, 1.0, 1.0);
    
    // Tiny per-pixel static noise to break up banding in dark areas (dithering).
    // 0.01 keeps it imperceptible except under close inspection
    float4 noise = float4(random * 0.01, random * 0.01, random * 0.01, 0.0);
    
    // Normalize the tint accumulation to get a weighted average color,
    // then scale by 0.15 to keep the atmospheric tint subtle.
    // If no weight accumulated (shouldn't happen), fall back to black
    float3 tint = (tintWeight > 0.0) ? (tintAccum / tintWeight) * 0.15 : float3(0.0, 0.0, 0.0);
    
    float4 finalColor = snowColor
        // Blue-tinted glow: R=0.3, G=0.6, B=1.0 gives a cold blue-white halo color.
        // Adjust these ratios to change the glow color (e.g. 1.0,0.8,0.4 for warm orange)
        + float4(glowBlue * 0.3, glowBlue * 0.6, glowBlue * 1.0, 0.0)
        // Subtract the depth tint to darken back layers atmospherically.
        // Using minus here means the tint darkens rather than brightens — 
        // change to + if you want the tint to add a colored fog instead
        - float4(tint.r, tint.g, tint.b, 0.0)
        + skyColor
        + noise;
    
    //if (finalColor.a * finalColor.g * finalColor.b < .05)
    //    discard; // optimize by skipping very dark pixels
    
    //finalColor.a = .5f;
    
    return finalColor;
}

technique Starfield
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}