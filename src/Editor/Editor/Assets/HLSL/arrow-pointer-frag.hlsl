struct PixelInput
{
    float4 position : SV_POSITION;
    float3 normal : NORMAL;
    float2 texCoord : TEXCOORD0;
};

Texture2D surfaceTexture;
SamplerState RegularSampler : register(s0);

float4 PS(PixelInput input) : SV_Target
{
    return surfaceTexture.Sample(RegularSampler, input.texCoord);
}
