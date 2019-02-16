// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Data
{
    /// <summary>
    /// This pool only cares about amortizing the allocations of the objects themselves, not the buffers inside the objects or configuration or reset or decommission of the objects.
    /// </summary>
    internal sealed class ObjectsPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly T[] _items;
        private int _count;
        private readonly int _capacity;

        public ObjectsPool(Func<T> factory)
            : this(factory, Environment.ProcessorCount * 8)
        {
        }

        public ObjectsPool(Func<T> factory, int capacity)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
            _items = new T[_capacity];
        }

        public void Rent(Span<T> items)
        {
            int createCount = items.Length;
            if (createCount == 0)
            {
                return;
            }

            int rentCount = 0;
            if (Volatile.Read(ref _count) > 0)
            {
                lock (_items)
                {
                    rentCount = Math.Min(_count, createCount);
                    if (rentCount > 0)
                    {
                        _count -= rentCount;
                        Span<T> copy = _items.AsSpan(_count, rentCount);
                        copy.CopyTo(items);
                        copy.Clear(); // remove the items from the cache so they have a single owner
                    }
                }
            }

            createCount -= rentCount;
            if (createCount > 0)
            {
                Create(items.Slice(rentCount));
            }

#if DEBUG
            for (int index = 0; index < items.Length; index++)
            {
                Debug.Assert(items[index] != null, "rented span parameter contains null");
            }
#endif
        }

        public void Return(Span<T> items, bool clearItems = true)
        {
            if (items.Length == 0)
            {
                return;
            }

#if DEBUG
            for (int index = 0; index < items.Length; index++)
            {
                Debug.Assert(items[index] != null, "return span parameter contains null");   
            }
#endif

            int takeCount = 0;
            if (Volatile.Read(ref _count) < _capacity)
            {
                lock (_items)
                {
                    takeCount = Math.Min(_capacity - _count, items.Length);
                    if (takeCount > 0)
                    {
                        Span<T> source = items.Slice(0, takeCount);
                        Span<T> dest = _items.AsSpan(_count);
                        source.CopyTo(dest);
                        _count += takeCount;
                        source.Clear(); // remove the items from the span so we are the single owner
                    }
                }
            }
            if (clearItems)
            {
                items.Slice(takeCount).Clear();
            }

#if DEBUG
            lock (_items)
            {
                for (int index = 0; index < _count; index++)
                {
                    Debug.Assert(_items[index] != null, "pool contains null entry");
                }
            }
#endif
        }

        public void Clear()
        {
            lock (_items)
            {
                Array.Clear(_items, 0, _capacity);
                _count = 0;
            }
        }

        private void Create(Span<T> items)
        {
            for (int index = 0; index < items.Length; index++)
            {
                items[index] = _factory();
            }
        }
    }
}

