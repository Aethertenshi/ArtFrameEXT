// --- Parameters passed from C# ---
float2 TexelSize;
float BlurAmount;

// --- Texture and Sampler Bindings ---
texture SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
    MIPFILTER = Linear;
    MINFILTER = Linear;
    MAGFILTER = Linear;
};

// --- Structs ---
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

// --- Pixel Shader ---
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = float4(0, 0, 0, 0);

    // Pre-calculated Gaussian weights for 9 taps (Sum equals exactly 1.0)
    float weights[9] = { 0.016216, 0.054054, 0.121622, 0.194595, 0.227027, 0.194595, 0.121622, 0.054054, 0.016216 };
    float offsets[9] = { -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0 };

    // Accumulate the weighted samples
    [unroll]
    for (int i = 0; i < 9; i++)
    {
        float2 sampleOffset = offsets[i] * TexelSize * BlurAmount;
        color += tex2D(SpriteTextureSampler, input.TextureCoordinates + sampleOffset) * weights[i];
    }

    return color * input.Color;
}

// --- Technique ---
technique GaussianBlur
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}