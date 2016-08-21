#version 140

in vec3 position;
out vec3 TexCoords;

uniform mat4 ProjectionMatrixBuffer;
uniform mat4 ViewMatrixBuffer;

void main()
{
	mat4 view3x3 = ViewMatrixBuffer;
	view3x3._m03 = 0;
	view3x3._m13 = 0;
	view3x3._m23 = 0;
	view3x3._m30 = 0;
	view3x3._m31 = 0;
	view3x3._m32 = 0;
	view3x3._m33 = 1;
    gl_Position = ((ProjectionMatrixBuffer * view3x3) * vec4(position, 1.0f)).xyww;
    TexCoords = position;
}  