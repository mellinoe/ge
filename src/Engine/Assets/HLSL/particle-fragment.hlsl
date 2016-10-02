cbuffer CameraInfoBuffer : register(b2)
{
    float3 cameraWorldPosition;
    float NearPlaneDistance;
    float3 cameraLookDirection;
    float FarPlaneDistance;
}

cbuffer ParticlePropertiesBuffer : register(b4)
{
    float4 colorTint;
    float softness;
    float3 __unused;
}

struct PixelInput
{
    float4 position : SV_POSITION;
    float alpha : TEXCOORD0;
    float2 texCoord : TEXCOORD1;
    float fragDepth : TEXCOORD2;
};

sampler sampler0;
Texture2D SurfaceTexture;

Texture2D DepthTexture;

float4 PS(PixelInput input) : SV_Target
{
    float4 color = SurfaceTexture.Sample(sampler0, input.texCoord);
    color.a *= input.alpha;
    color = color * colorTint;

    float depthThreshold = softness;
    float fragDepth = input.fragDepth;

    float zNear = NearPlaneDistance;
    float zFar = FarPlaneDistance;

    float depthSample = DepthTexture.Load(int3(input.position.xy, 0)).r;
    depthSample = 2.0 * depthSample - 1.0;
    float zLinear = 2.0 * zNear * zFar / (zFar + zNear - depthSample * (zFar - zNear));
    float sceneDepth = zLinear;

    float diff = sceneDepth - fragDepth;
    if (diff < 0)
    {
        discard;
    }

    color.a *= saturate(0 + (diff / depthThreshold));

    if (color.a == 0)
    {
        discard;
    }

    return color;
}
