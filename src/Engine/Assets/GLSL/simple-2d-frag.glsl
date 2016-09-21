#version 140

uniform sampler2D SurfaceTexture;

in vec2 out_texCoord;

out vec4 outputColor;

void main()
{
    bool flipTexCoords = true;
    vec2 texCoord_mod = out_texCoord;
    if (flipTexCoords)
    {
        texCoord_mod.y = 1 - texCoord_mod.y;
    }
    outputColor = texture(SurfaceTexture, texCoord_mod);
}
