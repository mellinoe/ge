struct PixelInput
{
    float4 position : SV_POSITION;
    float alpha : TEXCOORD0;
    float2 texCoord : TEXCOORD1;
};

sampler sampler0;
Texture2D SurfaceTexture;

float4 PS(PixelInput input) : SV_Target
{
    float4 color = SurfaceTexture.Sample(sampler0, input.texCoord);
	color.a *= input.alpha;
    return color;
}
