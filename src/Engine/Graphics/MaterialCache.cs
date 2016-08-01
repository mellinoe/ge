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
            MaterialKey key = new MaterialKey()
            {
                VertexShaderName = vertexShaderName,
                PixelShaderName = pixelShaderName,
                VertexInputs = vertexInputs,
                GlobalInputs = globalInputs
            };

            Material m;
            if (!_materials.TryGetValue(key, out m))
            {
                Console.WriteLine("Caching failed, creating new material.");
                m = _factory.CreateMaterial(rc, vertexShaderName, pixelShaderName, vertexInputs, globalInputs, perObjectInputs, textureInputs);
                _materials.Add(key, m);
            }

            return m;
        }

        private struct MaterialKey
        {
            public string VertexShaderName;
            public string PixelShaderName;
            public MaterialVertexInput VertexInputs;
            public MaterialInputs<MaterialGlobalInputElement> GlobalInputs;
        }
    }
}
