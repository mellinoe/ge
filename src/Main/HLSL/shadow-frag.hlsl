cbuffer LightInfoBuffer : register(b4)
{
    float4 lightColor;
    float3 lightDir;
    float __padding;
}

cbuffer TintInfoBuffer : register(b7)
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

float4 PS(PixelInput input) : SV_Target
{
    float4 surfaceColor = surfaceTexture.Sample(RegularSampler, input.texCoord);
    float4 ambient = float4(.4, .4, .4, 1);

    //re-homogenize position after interpolation
    input.lightPosition.xyz /= input.lightPosition.w;

    // if position is not visible to the light - dont illuminate it
    // results in hard light frustum
    if (input.lightPosition.x < -1.0f || input.lightPosition.x > 1.0f ||
        input.lightPosition.y < -1.0f || input.lightPosition.y > 1.0f ||
        input.lightPosition.z < 0.0f || input.lightPosition.z > 1.0f)
    {
        return ApplyTintColor(ambient * surfaceColor, tintColor, tintFactor);
    }

    //transform clip space coords to texture space coords (-1:1 to 0:1)
    input.lightPosition.x = input.lightPosition.x / 2 + 0.5;
    input.lightPosition.y = input.lightPosition.y / -2 + 0.5;

    float3 L = -1 * normalize(lightDir);
    float ndotl = dot(normalize(input.normal), L);

    float cosTheta = clamp(ndotl, 0, 1);
    float bias = 0.0005 * tan(acos(cosTheta));
    bias = clamp(bias, 0, 0.01);

    input.lightPosition.z -= bias;

	
	float width, height;
	shadowMap.GetDimensions(width, height); 
	float visibility = 1.0;
	float2 texelSize = 1.0 / float2(width, height);

#pragma warning( disable : 3570 ) // Loop needs to be unrolled because shadow map is sampled within.
	for (int x = -1; x <= 1; x++)
	{
		for (int y = -1; y <= 1; y++)
		{
			float depth = shadowMap.Sample(ShadowMapSampler, input.lightPosition.xy + (float2(x, y) * texelSize)).r;
			if (depth < input.lightPosition.z)
			{
				visibility -= (1.0 / 9.0);
			}
		}
	}

    //otherwise calculate ilumination at fragment
    ndotl = clamp(ndotl, 0, 1);
    float4 beforeTint = ambient * surfaceColor + (surfaceColor * ndotl * visibility);
    return ApplyTintColor(beforeTint, tintColor, tintFactor);
}