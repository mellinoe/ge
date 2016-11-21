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

uniform MaterialInfoBuffer
{
    float opacity;
    vec3 _padding3;
};

in vec3 out_position_worldSpace;
in vec4 out_lightPosition; //vertex with regard to light view
in vec3 out_normal;
in vec2 out_texCoord;
in float out_fragCoord;

uniform sampler2D surfaceTexture;
uniform sampler2D DepthTexture;

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

float GetLinearDepth(float depth)
{
    float zNear = NearPlaneDistance;
    float zFar = FarPlaneDistance;
    float depthSample = 2.0 * depth - 1.0;
    return 2.0 * zNear * zFar / (zFar + zNear - depthSample * (zFar - zNear));
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

    // Directional light calculations

    float fragDepth = GetLinearDepth(gl_FragCoord.z);

    ivec2 sceneTexCoord = ivec2(gl_FragCoord.xy);
    float depthSample = texelFetch(DepthTexture, sceneTexCoord, 0).r;
    float sceneDepth = GetLinearDepth(depthSample);

    float diff = sceneDepth - fragDepth;
    if (diff < 0)
    {
        discard;
    }

    vec3 L = -1 * normalize(lightDir);
    float diffuseFactor = dot(normalize(out_normal), L);

    // Calculate ilumination at fragment
    diffuseFactor = clamp(diffuseFactor, 0, 1);
    vec4 beforeTint = (ambientLight * surfaceColor + (surfaceColor * diffuseFactor)) + pointDiffuse + pointSpec;
    outputColor = ApplyTintColor(beforeTint, tintColor, tintFactor);
    outputColor.a = clamp(opacity, 0, 1);
    return;
}
