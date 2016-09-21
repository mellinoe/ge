#version 140

uniform sampler2D SurfaceTexture;

in vec3 normal;
in vec2 texCoord;

out vec4 outputColor;

void main()
{
    outputColor = texture(SurfaceTexture, texCoord);
}
