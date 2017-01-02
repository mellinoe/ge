#version 140

uniform LightInfoBuffer
{
    vec4 lightColor;
    vec3 lightDir;
    float _padding;
};

uniform CameraInfoBuffer
{
	vec3 cameraPosition_worldSpace;
    float NearPlaneDistance;
    vec3 cameraLookDirection;
    float FarPlaneDistance;
};

#define MAX_POINT_LIGHTS 4

struct PointLightInfo
{
	vec3 position;
	float range;
	vec3 color;
	float intensity;
};

uniform PointLightsBuffer
{
	int numActiveLights;
	PointLightInfo pointLights[MAX_POINT_LIGHTS];
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

float GetPointLightAttenuation(float distance, float radius, float cutoff)
{
	// Attenuation formula: https://imdoingitwrong.wordpress.com/2011/01/31/light-attenuation/

	// calculate basic attenuation
	float denom = distance / radius + 1;
	float attenuation = 1 / (denom * denom);

	// scale and bias attenuation such that:
	//   attenuation == 0 at extent of max influence
	//   attenuation == 1 when distance == 0
	attenuation = (attenuation - cutoff) / (1 - cutoff);
	attenuation = max(attenuation, 0);

	return attenuation;
}

void main()
{
    vec4 surfaceColor = texture(surfaceTexture, out_texCoord);
    vec4 ambientLight = vec4(.4, .4, .4, 1);
	float specularPower = 64;
	float specularIntensity = .2;

	// Point Diffuse

	vec4 pointDiffuse = vec4(0, 0, 0, 1);
	vec4 pointSpec = vec4(0, 0, 0, 1);
	for (int i = 0; i < numActiveLights; i++)
	{
		PointLightInfo pli = pointLights[i];
		vec3 lightDir = normalize(pli.position - out_position_worldSpace);
		float intensity = clamp(dot(out_normal, lightDir), 0, 1);
		float lightDistance = distance(pli.position, out_position_worldSpace);
		float attenuation = GetPointLightAttenuation(lightDistance, pli.range, 0.001);

		pointDiffuse += intensity * vec4(pli.color, 1) * surfaceColor * attenuation * pli.intensity;

		// Specular
		vec3 vertexToEye = normalize(cameraPosition_worldSpace - out_position_worldSpace);
		vec3 lightReflect = normalize(reflect(lightDir, out_normal));

		float specularFactor = dot(vertexToEye, lightReflect);
		if (specularFactor > 0)
		{
			specularFactor = pow(abs(specularFactor), specularPower);
			pointSpec += attenuation * (vec4(pli.color * specularIntensity * specularFactor, 1.0f));
		}
	}

	// perform perspective divide
    vec3 projCoords = out_lightPosition.xyz / out_lightPosition.w;

    // if out_position is not visible to the light - dont illuminate it
    // results in hard light frustum
    if (projCoords.x < -1.0f || projCoords.x > 1.0f ||
        projCoords.y < -1.0f || projCoords.y > 1.0f ||
        projCoords.z < 0.0f || projCoords.z > 1.0f)
    {
		vec4 beforeTint = (ambientLight * surfaceColor) + pointDiffuse + pointSpec;
        outputColor = ApplyTintColor(beforeTint, tintColor, tintFactor);
		return;
    }

	// Transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;

    vec3 L = -1 * normalize(lightDir);
    float diffuseFactor = dot(normalize(out_normal), L);

    float cosTheta = clamp(diffuseFactor, 0, 1);
    float bias = 0.0005 * tan(acos(cosTheta));
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
    diffuseFactor = clamp(diffuseFactor, 0, 1);
	vec4 beforeTint = (ambientLight * surfaceColor + (surfaceColor * diffuseFactor * visibility)) + pointDiffuse + pointSpec;
    outputColor = ApplyTintColor(beforeTint, tintColor, tintFactor);
	return;
}
