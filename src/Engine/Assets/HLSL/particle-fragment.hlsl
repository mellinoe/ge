cbuffer ColorTintBuffer : register(b4)
{
    float4 colorTint;
}

struct PixelInput
{
    float4 position : SV_POSITION;
    float alpha : TEXCOORD0;
    float2 texCoord : TEXCOORD1;
};

sampler sampler0;
Texture2D SurfaceTexture;

Texture2D DepthTexture;

float4 PS(PixelInput input) : SV_Target
{
    float4 color = SurfaceTexture.Sample(sampler0, input.texCoord);
	color.a *= input.alpha;
    color = color * colorTint;

    float depthThreshold = 0.1;
    float fragDepth = input.position.z / input.position.w;
    float sceneDepth = DepthTexture.Sample(sampler0, input.position.xy).r;
    float diff = sceneDepth - fragDepth;
    if (diff > 0 && diff < depthThreshold)
    {
        color.a *= 0 + (diff / depthThreshold);
    }

    if (color.a == 0)
        discard;
    return color;
}
