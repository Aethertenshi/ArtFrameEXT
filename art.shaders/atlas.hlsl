// --- Texture and Sampler Bindings (Direct3D 11 Registers) ---
Texture2D spriteTexture : register(t0);
SamplerState spriteSampler : register(s0);

// --- Input Structure (Matches SDL3 SDL_Vertex Layout) ---
struct PixelInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

/// <summary>
/// Computes the median of three color channels (R, G, B) to evaluate the signed distance.
/// </summary>
float median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

/// <summary>
/// The main entry point of the MTSDF Font Pixel Shader.
/// </summary>
float4 main(PixelInput input) : SV_TARGET
{
    // 1. Sample the multi-channel distance field texture
    float3 sample = spriteTexture.Sample(spriteSampler, input.TexCoord).rgb;
    
    // 2. Retrieve the signed distance from the median of R, G, B channels
    float sigDist = median(sample.r, sample.g, sample.b) - 0.5f;
    
    // 3. Screen-space anti-aliasing using standard pixel derivatives (fwidth)
    // fwidth(sigDist) calculates the change rate of the distance field in screen pixels,
    // automatically finding the perfect anti-aliased threshold width on every frame
    // without needing to pass any dynamic uniform parameters or atlas sizes!
    float pixelWidth = fwidth(sigDist);
    
    // 4. Perform smooth Hermite interpolation to generate razor-sharp outlines
    float opacity = smoothstep(-pixelWidth, pixelWidth, sigDist);
    
    // 5. Output modulated color
    return float4(input.Color.rgb, opacity * input.Color.a);
}
