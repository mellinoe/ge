#version 140

uniform AtlasInfoBuffer
{
    vec3 AtlasInfo;
};

uniform usampler2D FontAtlas;

in vec2 texCoords;
in vec4 color;

out vec4 outputColor;

void main()
{
    uint fontSample = texture(FontAtlas, texCoords).r;
    float floatSample = float(fontSample) / 255.0;
    outputColor = color;
    outputColor.a *= floatSample + (AtlasInfo.x - AtlasInfo.y);
}