#version 140

uniform LightInfoBuffer
{
    vec4 lightColor;
    vec3 lightDir;
    float _padding;
};

uniform TintInfoBuffer
{
    vec3 tintColor;
    float tintFactor;
};

in vec3 out_position_worldSpace;
in vec4 out_lightPosition; //vertex with regard to light view
in vec3 out_normal;
in vec2 out_texCoord;

uniform sampler2D surfaceTexture;
uniform sampler2D ShadowMap;

out vec4 outputColor;

vec4 ApplyTintColor(vec4 beforeTint, vec3 tintColor, float tintFactor)
{
    return (beforeTint * (1 - tintFactor) + (tintFactor * vec4(tintColor, 1)));
}

void main()
{
    vec4 surfaceColor = texture(surfaceTexture, out_texCoord);
    vec4 ambient = vec4(.4, .4, .4, 1);

	// perform perspective divide
    vec3 projCoords = out_lightPosition.xyz / out_lightPosition.w;

    // if out_position is not visible to the light - dont illuminate it
    // results in hard light frustum
    if (projCoords.x < -1.0f || projCoords.x > 1.0f ||
        projCoords.y < -1.0f || projCoords.y > 1.0f ||
        projCoords.z < 0.0f || projCoords.z > 1.0f)
    {
        outputColor = ApplyTintColor(ambient * surfaceColor, tintColor, tintFactor);
		return;
    }

	// Transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;

    vec3 L = -1 * normalize(lightDir);
    float ndotl = dot(normalize(out_normal), L);

    float cosTheta = clamp(ndotl, 0, 1);
    float bias = 0.0015 * tan(acos(cosTheta));
    bias = clamp(bias, 0, 0.01);
    projCoords.z -= bias;

	float visibility = 1.0;
	vec2 texelSize = 1.0 / textureSize(ShadowMap, 0);

	for (int x = -1; x <= 1; x++)
	{
		for (int y = -1; y <= 1; y++)
		{
			float depth = texture(ShadowMap, projCoords.xy + (vec2(x, y) * texelSize)).r;
			if (depth < projCoords.z)
			{
				visibility -= (1.0 / 9.0);
			}
		}
	}

    //otherwise calculate ilumination at fragment
    ndotl = clamp(ndotl, 0, 1);
    outputColor = ApplyTintColor(ambient * surfaceColor + (surfaceColor * ndotl * visibility), tintColor, tintFactor);
	return;
}
