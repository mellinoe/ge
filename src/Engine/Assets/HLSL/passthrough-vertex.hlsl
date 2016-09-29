struct VertexInput
{
    float3 offset : POSITION;
    float alpha : NORMAL;
};

struct GeoInput
{
    float3 offset : POSITION;
    float alpha : NORMAL;
};

GeoInput VS(VertexInput input)
{
    GeoInput output;
    output.offset = input.offset;
    output.alpha = input.alpha;
    return output;
}
