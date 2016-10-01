cbuffer LightInfoBuffer : register(b4)
{
	float4 lightColor;
	float3 lightDir;
	float __padding;
}

cbuffer CameraInfoBuffer : register(b5)
{
    float3 cameraPosition_worldSpace;
    float NearPlaneDistance;
    float3 cameraLookDirection;
    float FarPlaneDistance;
}

#define MAX_POINT_LIGHTS 4

struct PointLightInfo
{
	float3 position;
	float range;
	float3 color;
	float intensity;
};

cbuffer PointLightsBuffer : register(b6)
{
	int numActiveLights;
	float3 __padding2;
	PointLightInfo pointLights[MAX_POINT_LIGHTS];
}

cbuffer TintInfoBuffer : register(b9)
{
	float3 tintColor;
	float tintFactor;
}

struct PixelInput
{
	float4 position : SV_POSITION;
	float3 position_worldSpace : POSITION;
	float4 lightPosition : TEXCOORD0; //vertex with regard to light view
	float3 normal : NORMAL;
	float2 texCoord : TEXCOORD1;
};

Texture2D surfaceTexture;
SamplerState RegularSampler : register(s0);

Texture2D shadowMap;
SamplerState ShadowMapSampler : register(s1);

float4 ApplyTintColor(float4 beforeTint, float3 tintColor, float tintFactor)
{
	return (beforeTint * (1 - tintFactor) + (tintFactor * float4(tintColor, 1)));
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

float4 PS(PixelInput input) : SV_Target
{
	float4 surfaceColor = surfaceTexture.Sample(RegularSampler, input.texCoord);
	float4 ambientLight = float4(.4, .4, .4, 1);
	float specularPower = 64;
	float specularIntensity = .2;

	// Point Diffuse

	float4 pointDiffuse = float4(0, 0, 0, 1);
	float4 pointSpec = float4(0, 0, 0, 1);
	for (int i = 0; i < numActiveLights; i++)
	{
		PointLightInfo pli = pointLights[i];
		float3 lightDir = normalize(pli.position - input.position_worldSpace);
		float intensity = saturate(dot(input.normal, lightDir));
		float lightDistance = distance(pli.position, input.position_worldSpace);
		float attenuation = GetPointLightAttenuation(lightDistance, pli.range, 0.001);

		pointDiffuse += intensity * float4(pli.color, 1) * surfaceColor * attenuation * pli.intensity;

		// Specular
		float3 vertexToEye = normalize(cameraPosition_worldSpace - input.position_worldSpace);
		float3 lightReflect = normalize(reflect(lightDir, input.normal));

		float specularFactor = dot(vertexToEye, lightReflect);
		if (specularFactor > 0)
		{
			specularFactor = pow(abs(specularFactor), specularPower);
			pointSpec += attenuation * (float4(pli.color * specularIntensity * specularFactor, 1.0f));
		}
	}

	// pointDiffuse = saturate(pointDiffuse);
	// pointSpec = saturate(pointSpec);

	// Directional light calculations

	//re-homogenize position after interpolation
	input.lightPosition.xyz /= input.lightPosition.w;

	// if position is not visible to the light - dont illuminate it
	// results in hard light frustum
	if (input.lightPosition.x < -1.0f || input.lightPosition.x > 1.0f ||
		input.lightPosition.y < -1.0f || input.lightPosition.y > 1.0f ||
		input.lightPosition.z < 0.0f || input.lightPosition.z > 1.0f)
	{
		float4 beforeTint = (ambientLight * surfaceColor) + pointDiffuse + pointSpec;
		return ApplyTintColor(beforeTint, tintColor, tintFactor);
	}

	// transform clip space coords to texture space coords (-1:1 to 0:1)
	input.lightPosition.x = input.lightPosition.x / 2 + 0.5;
	input.lightPosition.y = input.lightPosition.y / -2 + 0.5;

	float3 L = -1 * normalize(lightDir);
	float diffuseFactor = dot(normalize(input.normal), L);

	float cosTheta = clamp(diffuseFactor, 0, 1);
	float bias = 0.0005 * tan(acos(cosTheta));
	bias = clamp(bias, 0, 0.01);

	input.lightPosition.z -= bias;

	//sample shadow map - point sampler
	float shadowMapDepth = shadowMap.Sample(ShadowMapSampler, input.lightPosition.xy).r;

	//if clip space z value greater than shadow map value then pixel is in shadow
	if (shadowMapDepth < input.lightPosition.z)
	{
		float4 beforeTint = (ambientLight * surfaceColor) + pointDiffuse + pointSpec;
		return ApplyTintColor(beforeTint, tintColor, tintFactor);
	}

	//otherwise calculate ilumination at fragment
	diffuseFactor = clamp(diffuseFactor, 0, 1);

	float4 specularColor = float4(0, 0, 0, 0);

	float3 vertexToEye = normalize(cameraPosition_worldSpace - input.position_worldSpace);
	float3 lightReflect = normalize(reflect(lightDir, input.normal));

	float specularFactor = dot(vertexToEye, lightReflect);
	if (specularFactor > 0)
	{
		specularFactor = pow(abs(specularFactor), specularPower);
		specularColor = float4(lightColor.rgb * specularIntensity * specularFactor, 1.0f);
	}

	float4 beforeTint = specularColor + (ambientLight * surfaceColor) + (diffuseFactor * lightColor * surfaceColor) + pointDiffuse + pointSpec;
	return ApplyTintColor(beforeTint, tintColor, tintFactor);
}