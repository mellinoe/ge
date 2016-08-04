#version 140

uniform LightBuffer
{
    vec4 diffuseColor;
    vec3 lightDirection;
};

uniform TintInfoBuffer
{
   vec3 tintColor;
   float tintFactor;
};

uniform sampler2D surfaceTexture;

in vec3 normal;
in vec2 texCoord;

out vec4 outputColor;

void main()
{
    outputColor = texture(surfaceTexture, texCoord);

    vec4 ambientColor = vec4(.4, .4, .4, 1);

    vec4 color = texture(surfaceTexture, texCoord);
    vec3 lightDir = -normalize(lightDirection);
    float effectiveness = dot(normal, lightDir);
    float lightEffectiveness = clamp(effectiveness, 0, 1);
    vec4 lightColor = clamp(diffuseColor * lightEffectiveness, 0, 1);
    vec4 litColor = clamp((lightColor * color) + (ambientColor * color), 0, 1);
    outputColor = (litColor * (1 - tintFactor) + (tintFactor * vec4(tintColor, 1)));

}
