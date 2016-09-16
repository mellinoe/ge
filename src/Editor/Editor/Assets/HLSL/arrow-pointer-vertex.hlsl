cbuffer ProjectionMatrixBuffer : register(b0)
{
    float4x4 projection;
}

cbuffer ViewMatrixBuffer : register(b1)
{
    float4x4 view;
}

cbuffer WorldMatrixBuffer : register(b2)
{
    float4x4 world;
}

struct VertexInput
{
    float3 position : POSITION;
	float3 normal : NORMAL;
	float2 texCoord : TEXCOORD0;
};

struct PixelInput
{
	float4 position : SV_POSITION;
	float3 normal : NORMAL;
	float2 texCoord : TEXCOORD0;
};

PixelInput VS(VertexInput input)
{
    PixelInput output;

	// This keeps the size of the object the same regardless of view position.
	// Approach taken from here:
    // https://www.opengl.org/discussion_boards/showthread.php/177936-draw-an-object-that-looks-the-same-size-regarles-the-distance-in-perspective-view
	float reciprScaleOnscreen = 0.075;
	float w = mul(projection, mul(view, mul(world, float4(0, 0, 0, 1)))).w;
	w *= reciprScaleOnscreen;

	output.position = mul(projection, mul(view, mul(world, float4(input.position * w, 1))));
	output.normal = input.normal;
    output.texCoord = input.texCoord;

    return output;
}
