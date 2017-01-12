#version 140

uniform ProjectionMatrixBuffer
{
    mat4 projection;
};

uniform ViewMatrixBuffer
{
    mat4 view;
};

uniform WorldMatrixBuffer
{
    mat4 world;
};

in vec3 position;
in vec3 normal;
in vec2 texCoord;

out vec3 fsin_normal;
out vec2 fsin_texCoord;

void main()
{
	// This keeps the size of the object the same regardless of view position.
	// Approach taken from here:
    // https://www.opengl.org/discussion_boards/showthread.php/177936-draw-an-object-that-looks-the-same-size-regarles-the-distance-in-perspective-view
	float reciprScaleOnscreen = 0.075;
	float w = (projection * (view * (world * vec4(0, 0, 0, 1)))).w;
	w *= reciprScaleOnscreen;

	gl_Position = projection * (view * (world * vec4(position * w, 1)));
	fsin_normal = normal;
    fsin_texCoord = texCoord;
}
