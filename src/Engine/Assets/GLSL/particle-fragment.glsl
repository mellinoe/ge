#version 150

uniform CameraInfoBuffer
{
    vec3 cameraWorldPosition;
    float NearPlaneDistance;
    vec3 cameraLookDirection;
    float FarPlaneDistance;
};

uniform ParticlePropertiesBuffer
{
    vec4 colorTint;
    float softness;
    vec3 _unused;
};

uniform sampler2D SurfaceTexture;
uniform sampler2D DepthTexture;

in float fs_alpha;
in vec2 fs_texCoord;
in vec4 fs_fragPosition;

out vec4 outputColor;

float GetLinearDepth(float depth)
{
    float zNear = NearPlaneDistance;
    float zFar = FarPlaneDistance;
    float depthSample = 2.0 * depth - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - depthSample * (zFar - zNear));
}

void main()
{
    vec4 color = texture(SurfaceTexture, fs_texCoord);
    color.a *= fs_alpha;
    color = color * colorTint;

    float depthThreshold = softness;

    float fragDepth = GetLinearDepth(gl_FragCoord.z);

    ivec2 sceneTexCoord = ivec2(gl_FragCoord.xy);
    float depthSample = texelFetch(DepthTexture, sceneTexCoord, 0).r;
    float sceneDepth = GetLinearDepth(depthSample);

    float diff = sceneDepth - fragDepth;
    if (diff < 0)
    {
        discard;
    }

    color.a *= clamp(0 + (diff / depthThreshold), 0, 1);

    if (color.a == 0)
    {
    }

    outputColor = color;
}
