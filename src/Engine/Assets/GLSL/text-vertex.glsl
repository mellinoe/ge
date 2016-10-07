#version 140

uniform ProjectionMatrixBuffer
{
    mat4 projection_matrix;
};

in vec2 in_position;
in vec2 in_texCoord;
in vec4 in_color;

out vec2 texCoords;
out vec4 color;

void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
	texCoords = in_texCoord;
    color = in_color;
}
