struct PixelInput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD0;
};

sampler sampler0;
Texture2D SurfaceTexture;

float4 PS(PixelInput input) : SV_Target
{
    float4 color = SurfaceTexture.Sample(sampler0, input.texCoord);
    return color;
}
