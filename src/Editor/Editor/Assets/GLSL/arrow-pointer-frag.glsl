#version 140

uniform sampler2D SurfaceTexture;

in vec3 fsin_normal;
in vec2 fsin_texCoord;

out vec4 outputColor;

void main()
{
    outputColor = texture(SurfaceTexture, fsin_texCoord);
}
