using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !VALIDATE
using System.Diagnostics;
#endif

namespace Veldrid.Collections
{
    public unsafe class NativeList<T> : IEnumerable<T>, IDisposable where T : struct
    {
        private byte* _dataPtr;
        private uint _elementCapacity;
        private uint _count;

        public const int DefaultCapacity = 4;
        private const float GrowthFactor = 2f;
        private static readonly uint s_elementByteSize = InitializeTypeSize();

        public NativeList() : this(DefaultCapacity) { }
        public NativeList(uint capacity)
        {
            Allocate(capacity);
        }

        public IntPtr Data
        {
            get
            {
                ThrowIfDisposed();
                return new IntPtr(_dataPtr);
            }
        }

        public uint Count
        {
            get
            {
                ThrowIfDisposed();
                return _count;
            }
            set
            {
                ThrowIfDisposed();
                if (value > _elementCapacity)
                {
                    uint newLements = value - Count;
                    CoreResize(value);
                    Unsafe.InitBlock(_dataPtr + _count * s_elementByteSize, 0, newLements * s_elementByteSize);
                }

                _count = value;
            }
        }

        public ref T this[uint index]
        {
            get
            {
                ThrowIfDisposed();
#if VALIDATE
                if (index >= _count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#else
                Debug.Assert(index < _count);
#endif
                return ref Unsafe.AsRef<T>(_dataPtr + index * s_elementByteSize);
            }
        }

        public ref T this[int index]
        {
            get
            {
                ThrowIfDisposed();
#if VALIDATE
                if (index < 0 || index >= _count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#else
                Debug.Assert(index >= 0 && index < _count);
#endif
                return ref Unsafe.AsRef<T>(_dataPtr + index * s_elementByteSize);
            }
        }

        public ReadOnlyNativeListView<T> GetReadOnlyView()
        {
            ThrowIfDisposed();
            return new ReadOnlyNativeListView<T>(this, 0, _count);
        }

        public ReadOnlyNativeListView<T> GetReadOnlyView(uint start, uint count)
        {
            ThrowIfDisposed();
#if VALIDATE
            if (start + count > _count)
            {
                throw new ArgumentOutOfRangeException();
            }
#else
            Debug.Assert(start + count < _count);
#endif
            return new ReadOnlyNativeListView<T>(this, start, count);
        }

        public View<ViewType> GetView<ViewType>() where ViewType : struct
        {
            ThrowIfDisposed();
            return new View<ViewType>(this);
        }

        public bool IsDisposed => _dataPtr == null;

        public void Add(ref T item)
        {
            ThrowIfDisposed();
            if (_count == _elementCapacity)
            {
                CoreResize((uint)(_elementCapacity * GrowthFactor));
            }

            Unsafe.Copy(_dataPtr + _count * s_elementByteSize, ref item);
            _count += 1;
        }

        public void Add(T item)
        {
            ThrowIfDisposed();
            if (_count == _elementCapacity)
            {
                CoreResize((uint)(_elementCapacity * GrowthFactor));
            }

            Unsafe.Write(_dataPtr + _count * s_elementByteSize, item);
            _count += 1;
        }

        public void Add(void* data, uint numElements)
        {
            ThrowIfDisposed();
            uint needed = _count + numElements;
            if (numElements > _elementCapacity)
            {
                CoreResize((uint)(needed * GrowthFactor));
            }

            Unsafe.CopyBlock(_dataPtr + _count * s_elementByteSize, data, numElements * s_elementByteSize);
            _count += numElements;
        }

        public bool Remove(ref T item)
        {
            ThrowIfDisposed();
            uint index;
            bool result = IndexOf(ref item, out index);
            if (result)
            {
                CoreRemoveAt(index);
            }

            return result;
        }

        public bool Remove(T item) => Remove(ref item);

        public void RemoveAt(uint index)
        {
            ThrowIfDisposed();
#if VALIDATE
            if (index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
#else
            Debug.Assert(index < _count);
#endif
            CoreRemoveAt(index);
        }

        public void Clear()
        {
            ThrowIfDisposed();
            _count = 0;
        }

        public bool IndexOf(ref T item, out uint index)
        {
            ThrowIfDisposed();
            byte* itemPtr = (byte*)Unsafe.AsPointer(ref item);
            for (index = 0; index < _count; index++)
            {
                byte* ptr = _dataPtr + index * s_elementByteSize;
                if (Equals(ptr, itemPtr, s_elementByteSize))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IndexOf(T item, out uint index)
        {
            ThrowIfDisposed();
            byte* itemPtr = (byte*)Unsafe.AsPointer(ref item);
            for (index = 0; index < _count; index++)
            {
                byte* ptr = _dataPtr + index * s_elementByteSize;
                if (Equals(ptr, itemPtr, s_elementByteSize))
                {
                    return true;
                }
            }

            return false;
        }

        public void Resize(uint elementCount)
        {
            ThrowIfDisposed();
            CoreResize(elementCount);
            if (_elementCapacity < _count)
            {
                _count = _elementCapacity;
            }
        }

        private static uint InitializeTypeSize()
        {
#if VALIDATE
            // TODO: DHetermine if the structure type contains references and throw if it does.
            // https://github.com/dotnet/corefx/issues/14047
#endif
            return (uint)Unsafe.SizeOf<T>();
        }

        private void CoreResize(uint elementCount)
        {
            _dataPtr = (byte*)Marshal.ReAllocHGlobal(new IntPtr(_dataPtr), (IntPtr)(elementCount * s_elementByteSize));
            _elementCapacity = elementCount;
        }

        private void Allocate(uint elementCount)
        {
            _dataPtr = (byte*)Marshal.AllocHGlobal((int)(elementCount * s_elementByteSize));
            _elementCapacity = elementCount;
        }

        private bool Equals(byte* ptr, byte* itemPtr, uint s_elementByteSize)
        {
            for (int i = 0; i < s_elementByteSize; i++)
            {
                if (ptr[i] != itemPtr[i])
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CoreRemoveAt(uint index)
        {
            Unsafe.CopyBlock(_dataPtr + index * s_elementByteSize, _dataPtr + (_count - 1) * s_elementByteSize, s_elementByteSize);
            _count -= 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
#if VALIDATE
            if (_dataPtr == null)
            {
                throw new ObjectDisposedException(nameof(Data));
            }
#else
            Debug.Assert(_dataPtr != null, "NativeList is disposed.");
#endif
        }

        public void Dispose()
        {
            ThrowIfDisposed();
            Marshal.FreeHGlobal(new IntPtr(_dataPtr));
            _dataPtr = null;
        }

        public Enumerator GetEnumerator()
        {
            ThrowIfDisposed();
            return new Enumerator(_dataPtr, _count);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private byte* _basePtr;
            private uint _count;
            private uint _currentIndex;
            private T _current;

            public Enumerator(byte* basePtr, uint count)
            {
                _basePtr = basePtr;
                _count = count;
                _currentIndex = 0;
                _current = default(T);
            }

            public T Current => _current;
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentIndex != _count)
                {
                    _current = Unsafe.Read<T>(_basePtr + _currentIndex * s_elementByteSize);
                    _currentIndex += 1;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _current = default(T);
                _currentIndex = 0;
            }

            public void Dispose() { }
        }

        public struct View<ViewType> : IEnumerable<ViewType> where ViewType : struct
        {
            private static readonly uint s_elementByteSize = (uint)Unsafe.SizeOf<ViewType>();
            private readonly NativeList<T> _parent;

            public View(NativeList<T> parent)
            {
                _parent = parent;
            }

            public uint Count => (_parent.Count * NativeList<T>.s_elementByteSize) / s_elementByteSize;

            public ViewType this[uint index]
            {
                get
                {
#if VALIDATE
                    if (index >= Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
#else
                    Debug.Assert(index < Count);
#endif
                    return Unsafe.Read<ViewType>(_parent._dataPtr + index * s_elementByteSize);
                }
            }

            public Enumerator GetEnumerator() => new Enumerator(this);

            IEnumerator<ViewType> IEnumerable<ViewType>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<ViewType>
            {
                private View<ViewType> _view;
                private uint _currentIndex;
                private ViewType _current;

                public Enumerator(View<ViewType> view)
                {
                    _view = view;
                    _currentIndex = 0;
                    _current = default(ViewType);
                }

                public ViewType Current => _view[_currentIndex];
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_currentIndex != _view.Count)
                    {
                        _current = _view[_currentIndex];
                        _currentIndex += 1;
                        return true;
                    }

                    return false;
                }

                public void Reset()
                {
                    _currentIndex = 0;
                    _current = default(ViewType);
                }

                public void Dispose() { }
            }
        }
    }

    public static class NativeList
    {
        public static void Sort<TKey, TValue>(
            NativeList<TKey> keys,
            NativeList<TValue> items,
            uint index,
            uint count,
            IComparer<TKey> comparer)
            where TKey : struct
            where TValue : struct
        {
#if VALIDATE
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }
            if (keys.Count - index < count || (items != null && index > items.Count - count))
            {
                throw new ArgumentException();
            }
#else
            Debug.Assert(keys != null);
            Debug.Assert(items != null);
            Debug.Assert(!(keys.Count - index < count || (items != null && index > items.Count - count)));
            Debug.Assert(comparer != null);
#endif

            if (count > 1)
            {
                NativeSortHelper<TKey, TValue>.Sort(keys, items, index, count, comparer);
            }
        }
    }

    public struct ReadOnlyNativeListView<T> : IEnumerable<T> where T : struct
    {
        private readonly NativeList<T> _list;
        private readonly uint _start;
        public readonly uint Count;

        public ReadOnlyNativeListView(NativeList<T> list, uint start, uint count)
        {
            _list = list;
            _start = start;
            Count = count;
        }

        public T this[uint index]
        {
            get
            {
#if VALIDATE
                if (index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#else
                Debug.Assert(index < Count);
#endif
                return _list[index + _start];
            }
        }

        public T this[int index]
        {
            get
            {
#if VALIDATE
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#else
                Debug.Assert(index >= 0 && index < Count);
#endif
                return _list[(uint)index + _start];
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private ReadOnlyNativeListView<T> _view;
            private uint _currentIndex;
            private T _current;

            public Enumerator(ReadOnlyNativeListView<T> view)
            {
                _view = view;
                _currentIndex = view._start;
                _current = default(T);
            }

            public T Current => _view[_currentIndex];
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_currentIndex != _view._start + _view.Count)
                {
                    _current = _view[_currentIndex];
                    _currentIndex += 1;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _currentIndex = _view._start;
                _current = default(T);
            }

            public void Dispose() { }
        }
    }
}
