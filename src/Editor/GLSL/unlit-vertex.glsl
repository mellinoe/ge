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

in vec3 in_position;
in vec4 in_color;

out vec4 out_color;

void main()
{
	// This keeps the size of the object the same regardless of view position.
	// Approach taken from here:
    // https://www.opengl.org/discussion_boards/showthread.php/177936-draw-an-object-that-looks-the-same-size-regarles-the-distance-in-perspective-view
	float reciprScaleOnscreen = 0.075;
	float w = (projection *  (view * (world * vec4(0, 0, 0, 1)))).w;
	w *= reciprScaleOnscreen;

	gl_Position = (projection * (view * (world * vec4(in_position * w, 1))));

    out_color = in_color;
}
