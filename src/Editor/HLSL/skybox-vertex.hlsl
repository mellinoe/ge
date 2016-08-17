cbuffer ProjectionMatrixBuffer : register(b0)
{
    float4x4 projection;
}

cbuffer ViewMatrixBuffer : register(b1)
{
    float4x4 view;
}

struct VS_INPUT
{
    float3 Position : POSITION;
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float3 TexCoord : TEXCOORD;
};

PS_INPUT VS(VS_INPUT input)
{
	float4x4 view3x3 = view;
	view3x3._m03 = 0;
	view3x3._m13 = 0;
	view3x3._m23 = 0;
	view3x3._m30 = 0;
	view3x3._m31 = 0;
	view3x3._m32 = 0;
	view3x3._m33 = 1;
	PS_INPUT output;
    output.Position = mul(mul(projection, view3x3), float4(input.Position, 1.0f)).xyww;
    output.TexCoord = input.Position;
    return output;
}