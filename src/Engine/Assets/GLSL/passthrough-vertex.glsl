#version 150

in vec3 in_offset;
in float in_alpha;
in float in_size;

out VertexData
{
    float alpha;
    float size;
} gs_in;

void main()
{
    gl_Position = vec4(in_offset, 1);
    gs_in.alpha = in_alpha;
    gs_in.size = in_size;
}
