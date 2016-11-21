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

cbuffer MaterialInfoBuffer : register(b10)
{
    float opacity;
    float3 __padding3;
}

struct PixelInput
{
    float4 position : SV_POSITION;
    float3 position_worldSpace : POSITION;
    float4 lightPosition : TEXCOORD0; //vertex with regard to light view
    float3 normal : NORMAL;
    float2 texCoord : TEXCOORD1;
    float fragDepth : TEXCOORD2;
};

Texture2D surfaceTexture;
SamplerState RegularSampler : register(s0);

Texture2D DepthTexture;
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

    // Directional light calculations

    float fragDepth = input.fragDepth;

    float zNear = NearPlaneDistance;
    float zFar = FarPlaneDistance;

    float depthSample = DepthTexture.Load(int3(input.position.xy, 0)).r;
    depthSample = 2.0 * depthSample - 1.0;
    float zLinear = 2.0 * zNear * zFar / (zFar + zNear - depthSample * (zFar - zNear));
    float sceneDepth = zLinear;

    float diff = sceneDepth - fragDepth;
    if (diff < 0)
    {
        discard;
    }

    float3 L = -1 * normalize(lightDir);
    float diffuseFactor = dot(normalize(input.normal), L);

    // Calculate ilumination at fragment
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
    float4 afterTint = ApplyTintColor(beforeTint, tintColor, tintFactor);
    return float4(afterTint.rgb, saturate(opacity));
}