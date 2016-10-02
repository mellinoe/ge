struct VertexInput
{
    float3 offset : POSITION;
    float alpha : TEXCOORD0;
    float size : TEXCOORD1;
};

struct GeoInput
{
    float3 offset : POSITION;
    float alpha : TEXCOORD0;
    float size : TEXCOORD1;
};

GeoInput VS(VertexInput input)
{
    GeoInput output;
    output.offset = input.offset;
    output.alpha = input.alpha;
    output.size = input.size;
    return output;
}
