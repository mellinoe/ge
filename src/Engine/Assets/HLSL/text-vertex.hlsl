cbuffer ProjectionMatrixBuffer : register(b0)
{
    float4x4 ProjectionMatrix;
};

cbuffer TextOffsetBuffer : register(b1)
{
    float2 offset;
    float2 __unused;
};

struct VS_INPUT
{
    float2 position : POSITION;
    float2 texCoords : TEXCOORD0;
    float4 color : COLOR0;
};

struct PS_INPUT
{
    float4 position : SV_POSITION;
    float2 texCoords : TEXCOORD0;
    float4 color : COLOR0;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;
    output.position = mul(ProjectionMatrix, float4(input.position + offset, 0, 1));
    output.texCoords = input.texCoords;
    output.color = input.color;
    return output;
}