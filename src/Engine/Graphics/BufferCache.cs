using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Veldrid.Graphics;

namespace Engine.Graphics
{
    public class BufferCache
    {
        private readonly GraphicsSystem _gs;
        private readonly ResourceFactory _factory;

        private ConcurrentDictionary<BufferKey, VertexBuffer> _vbs = new ConcurrentDictionary<BufferKey, VertexBuffer>();
        private ConcurrentDictionary<BufferKey, IndexBufferAndCount> _ibs = new ConcurrentDictionary<BufferKey, IndexBufferAndCount>();

        public BufferCache(GraphicsSystem gs)
        {
            _gs = gs;
            _factory = gs.Context.ResourceFactory;
        }

        public VertexBuffer GetVertexBuffer(MeshData mesh)
        {
            VertexBuffer vb;
            BufferKey key = new BufferKey(mesh);
            if (!_vbs.TryGetValue(key, out vb))
            {
                vb = mesh.CreateVertexBuffer(_factory);
                if (!_vbs.TryAdd(key, vb))
                {
                    vb.Dispose();
                    return _vbs[key];
                }
            }

            return vb;
        }

        public async Task<VertexBuffer> GetVertexBufferAsync(MeshData mesh)
        {
            return await _gs.ExecuteOnMainThread(() => GetVertexBuffer(mesh));
        }

        public IndexBuffer GetIndexBuffer(MeshData mesh, out int indexCount)
        {
            IndexBufferAndCount bufferAndCount = GetIndexBufferAndCount(mesh);
            indexCount = bufferAndCount.IndexCount;
            return bufferAndCount.Buffer;
        }

        public IndexBufferAndCount GetIndexBufferAndCount(MeshData mesh)
        {
            int indexCount;
            IndexBufferAndCount bufferAndCount;
            BufferKey key = new BufferKey(mesh);
            if (!_ibs.TryGetValue(key, out bufferAndCount))
            {
                var indexBuffer = mesh.CreateIndexBuffer(_factory, out indexCount);
                bufferAndCount = new IndexBufferAndCount(indexBuffer, indexCount);
                if (!_ibs.TryAdd(key, bufferAndCount))
                {
                    indexBuffer.Dispose();
                    return _ibs[key];
                }
            }
            else
            {
                indexCount = bufferAndCount.IndexCount;
            }

            return bufferAndCount;
        }


        public async Task<IndexBufferAndCount> GetIndexBufferAndCountAsync(MeshData mesh)
        {
            return await _gs.ExecuteOnMainThread(() => GetIndexBufferAndCount(mesh));
        }

        private struct BufferKey
        {
            public readonly MeshData MeshData;

            public BufferKey(MeshData md)
            {
                MeshData = md;
            }
        }

        public struct IndexBufferAndCount
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