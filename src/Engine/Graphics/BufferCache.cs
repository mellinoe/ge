using System.Collections.Generic;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class BufferCache
    {
        private readonly ResourceFactory _factory;

        private Dictionary<BufferKey, VertexBuffer> _vbs = new Dictionary<BufferKey, VertexBuffer>();
        private Dictionary<BufferKey, IndexBufferAndCount> _ibs = new Dictionary<BufferKey, IndexBufferAndCount>();

        public BufferCache(ResourceFactory resourceFactory)
        {
            this._factory = resourceFactory;
        }

        public VertexBuffer GetVertexBuffer(MeshData mesh)
        {
            VertexBuffer vb;
            BufferKey key = new BufferKey(mesh);
            if (!_vbs.TryGetValue(key, out vb))
            {
                vb = mesh.CreateVertexBuffer(_factory);
                _vbs.Add(key, vb);
            }

            return vb;
        }

        public IndexBuffer GetIndexBuffer(MeshData mesh, out int indexCount)
        {
            IndexBufferAndCount bufferAndCount;
            BufferKey key = new BufferKey(mesh);
            if (!_ibs.TryGetValue(key, out bufferAndCount))
            {
                var indexBuffer = mesh.CreateIndexBuffer(_factory, out indexCount);
                bufferAndCount = new IndexBufferAndCount(indexBuffer, indexCount);
                _ibs.Add(key, bufferAndCount);
            }
            else
            {
                indexCount = bufferAndCount.IndexCount;
            }

            return bufferAndCount.Buffer;
        }

        private struct BufferKey
        {
            public readonly MeshData MeshData;

            public BufferKey(MeshData md)
            {
                MeshData = md;
            }
        }

        private struct IndexBufferAndCount
        {
            public readonly IndexBuffer Buffer;
            public readonly int IndexCount;

            public IndexBufferAndCount(IndexBuffer buffer, int indexCount)
            {
                Buffer = buffer;
                IndexCount = indexCount;
            }
        }
    }
}