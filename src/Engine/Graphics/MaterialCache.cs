using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class MaterialCache
    {
        private readonly GraphicsSystem _gs;
        private readonly ResourceFactory _factory;

        private ConcurrentDictionary<MaterialKey, Material> _materials = new ConcurrentDictionary<MaterialKey, Material>();

        public MaterialCache(GraphicsSystem gs)
        {
            _gs = gs;
            _factory = gs.Context.ResourceFactory;
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

        public Task<Material> GetMaterialAsync(
            RenderContext rc,
            string vertexShaderName,
            string pixelShaderName,
            MaterialVertexInput vertexInputs,
            MaterialInputs<MaterialGlobalInputElement> globalInputs,
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs,
            MaterialTextureInputs textureInputs)
        {
            return _gs.ExecuteOnMainThread(
                () => GetMaterial(rc, vertexShaderName, pixelShaderName, vertexInputs, globalInputs, perObjectInputs, textureInputs));
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

                if (!_materials.TryAdd(key, m))
                {
                    return _materials[key];
                }
            }

            return m;
        }

        public Task<Material> GetMaterialAsync(
            RenderContext rc,
            string vertexShaderName,
            string geometryShaderName,
            string pixelShaderName,
            MaterialVertexInput vertexInputs,
            MaterialInputs<MaterialGlobalInputElement> globalInputs,
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs,
            MaterialTextureInputs textureInputs)
        {
            return _gs.ExecuteOnMainThread(
                () => GetMaterial(rc, vertexShaderName, geometryShaderName, pixelShaderName, vertexInputs, globalInputs, perObjectInputs, textureInputs));
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
