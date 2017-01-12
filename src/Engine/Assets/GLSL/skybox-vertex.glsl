#version 140

in vec3 position;
out vec3 TexCoords;

uniform mat4 ProjectionMatrixBuffer;
uniform mat4 ViewMatrixBuffer;

void main()
{
	mat4 view3x3 = ViewMatrixBuffer;
	view3x3[3][0] = 0;
	view3x3[3][1] = 0;
	view3x3[3][2] = 0;
	view3x3[0][3] = 0;
	view3x3[1][3] = 0;
	view3x3[2][3] = 0;
	view3x3[3][3] = 1;
    gl_Position = ((ProjectionMatrixBuffer * view3x3) * vec4(position, 1.0f)).xyww;
    TexCoords = position;
}  