struct VertexInput
{
    float3 offset : POSITION;
    float alpha : TEXCOORD0;
};

struct GeoInput
{
    float3 offset : POSITION;
    float alpha : TEXCOORD0;
};

GeoInput VS(VertexInput input)
{
    GeoInput output;
    output.offset = input.offset;
    output.alpha = input.alpha;
    return output;
}
