cbuffer ProjectionMatrixBuffer : register(b0)
{
    float4x4 projection;
}

cbuffer ViewMatrixBuffer : register(b1)
{
    float4x4 view;
}

cbuffer CameraInfoBuffer : register(b2)
{
    float3 cameraWorldPosition;
    float NearPlaneDistance;
    float3 cameraLookDirection;
    float FarPlaneDistance;
}

cbuffer WorldMatrixBuffer : register(b3)
{
    float4x4 world;
}

struct GeoInput
{
    float3 offset : POSITION;
    float alpha : TEXCOORD0;
    float size : TEXCOORD1;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    float alpha : TEXCOORD0;
    float2 texCoord : TEXCOORD1;
    float fragDepth : TEXCOORD2;
};

[maxvertexcount(4)]
void GS(point GeoInput input[1], inout TriangleStream<PixelInput> outputStream)
{
    float3 worldCenter = mul(world, float4(input[0].offset, 1)).xyz;
    float3 globalUp = float3(0, 1, 0);
    float3 right = normalize(cross(cameraLookDirection, globalUp));
    float3 up = normalize(cross(right.xyz, cameraLookDirection));
    
    float halfWidth = 0.5 * input[0].size; // Half-width of each edge of the generated quad.
    float3 worldPositions[4] =
    {
        worldCenter - right * halfWidth + up * halfWidth,
        worldCenter + right * halfWidth + up * halfWidth,
        worldCenter - right * halfWidth - up * halfWidth,
        worldCenter + right * halfWidth - up * halfWidth,
    };

    float2 uvs[4] = 
    {
        float2(0, 0),
        float2(1, 0),
        float2(0, 1),
        float2(1, 1)
    };

    PixelInput output;

    for (int i = 0; i < 4; i++)
    {
        float4 outPosition = mul(projection, mul(view, float4(worldPositions[i], 1)));
        output.position = outPosition;
        output.texCoord = uvs[i];
        output.alpha = input[0].alpha;
        output.fragDepth = outPosition.z;
        outputStream.Append(output);
    }
}