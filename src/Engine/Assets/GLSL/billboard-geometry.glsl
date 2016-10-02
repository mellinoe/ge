#version 150

uniform ProjectionMatrixBuffer
{
    mat4 projection;
};

uniform ViewMatrixBuffer
{
    mat4 view;
};

uniform CameraInfoBuffer
{
    vec3 cameraWorldPosition;
    float NearPlaneDistance;
    vec3 cameraLookDirection;
    float FarPlaneDistance;
};

uniform WorldMatrixBuffer
{
    mat4 world;
};

layout (points) in;
layout (triangle_strip, max_vertices = 4) out;

in VertexData
{
    float alpha;
    float size;
} gs_in[1];

out float fs_alpha;
out vec2 fs_texCoord;
out vec4 fs_fragPosition;

void main()
{
    vec3 worldCenter = (world * gl_in[0].gl_Position).xyz;
    vec3 globalUp = vec3(0, 1, 0);
    vec3 right = normalize(cross(cameraLookDirection, globalUp));
    vec3 up = normalize(cross(right.xyz, cameraLookDirection));
    float halfWidth = 0.5 * gs_in[0].size; // Half-width of each edge of the generated quad.

    vec3 worldPositions[4] = vec3[4]
    (
        worldCenter - right * halfWidth + up * halfWidth,
        worldCenter + right * halfWidth + up * halfWidth,
        worldCenter - right * halfWidth - up * halfWidth,
        worldCenter + right * halfWidth - up * halfWidth
    );

    vec2 uvs[4] = vec2[4]
    (
        vec2(0, 0),
        vec2(1, 0),
        vec2(0, 1),
        vec2(1, 1)
    );

    for (int i = 0; i < 4; i++)
    {
        vec4 outPosition = (projection * (view * vec4(worldPositions[i], 1)));
        gl_Position = outPosition;
        fs_alpha = gs_in[0].alpha;
        fs_texCoord = uvs[i];
        //fs_fragDepth = outPosition.z;
        fs_fragPosition = outPosition;
        EmitVertex();
    }
}