using System;
using System.Collections.Generic;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class MaterialCache
    {
        private readonly ResourceFactory _factory;

        private Dictionary<MaterialKey, Material> _materials = new Dictionary<MaterialKey, Material>();

        public MaterialCache(ResourceFactory factory)
        {
            _factory = factory;
        }

        public Material GetMaterial(
            RenderContext rc,
            string vertexShaderName,
            string pixelShaderName,
            MaterialVertexInput vertexInputs,
            MaterialInputs<MaterialGlobalInputElement> globalInputs,
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs,
            MaterialTextureInputs textureInputs)
        {
            return GetMaterial(rc, vertexShaderName, null, pixelShaderName, vertexInputs, globalInputs, perObjectInputs, textureInputs);
        }

        public Material GetMaterial(
            RenderContext rc,
            string vertexShaderName,
            string geometryShaderName,
            string fragmentShaderName,
            MaterialVertexInput vertexInputs,
            MaterialInputs<MaterialGlobalInputElement> globalInputs,
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs,
            MaterialTextureInputs textureInputs)
        {
            MaterialKey key = new MaterialKey()
            {
                VertexShaderName = vertexShaderName,
                FragmentShaderName = fragmentShaderName,
                GeometryShaderName = geometryShaderName,
                VertexInputs = vertexInputs,
                GlobalInputs = globalInputs
            };

            Material m;
            if (!_materials.TryGetValue(key, out m))
            {
                Shader vs = _factory.CreateShader(ShaderType.Vertex, vertexShaderName);
                Shader fs = _factory.CreateShader(ShaderType.Fragment, fragmentShaderName);
                VertexInputLayout inputLayout = _factory.CreateInputLayout(vs, vertexInputs);

                ShaderSet shaderSet;
                if (geometryShaderName != null)
                {
                    Shader gs = _factory.CreateShader(ShaderType.Geometry, geometryShaderName);
                    shaderSet = _factory.CreateShaderSet(inputLayout, vs, gs, fs);
                }
                else
                {
                    shaderSet = _factory.CreateShaderSet(inputLayout, vs, fs);
                }

                ShaderConstantBindings constantBindings = _factory.CreateShaderConstantBindings(rc, shaderSet, globalInputs, perObjectInputs);
                ShaderTextureBindingSlots textureSlots = _factory.CreateShaderTextureBindingSlots(shaderSet, textureInputs);
                m = new Material(rc, shaderSet, constantBindings, textureSlots, _factory.CreateDefaultTextureBindingInfos(rc, textureInputs));

                _materials.Add(key, m);
            }

            return m;
        }

        private struct MaterialKey
        {
            public string VertexShaderName;
            public string GeometryShaderName;
            public string FragmentShaderName;
            public MaterialVertexInput VertexInputs;
            public MaterialInputs<MaterialGlobalInputElement> GlobalInputs;
        }
    }
}
