// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CoreWf.Runtime
{
    // Pool<T> defined below is a LIFO pool of Pool.IClearable objects.
    // It is strongly typed to make the Acquire/Release methods more user
    // friendly.  To use this pool, subclass it with a concrete type and
    // override the CreateNew method.  Typically, the type of T will
    // have a default ctor and will use an Initialize(...) method in order
    // to configure it for use.
    // NOTE: CreateNew is required because T : new() requires that the default
    // ctor is public.  We did not want to put public ctors on some of our
    // pooled resources (like NativeActivityContext).
    internal abstract class Pool<T>
    {
        private const int DefaultPoolSize = 10;

        private T[] _items;
        private int _count;
        private int _poolSize;

        public Pool()
            : this(DefaultPoolSize)
        {
        }

        public Pool(int poolSize)
        {
            _items = new T[poolSize];
            _poolSize = poolSize;
        }

        public T Acquire()
        {
            if (_count > 0)
            {
                _count--;
                T item = _items[_count];

                return item;
            }
            else
            {
                return CreateNew();
            }
        }

        protected abstract T CreateNew();

        public void Release(T item)
        {
            if (_count < _poolSize)
            {
                _items[_count] = item;
                _count++;
            }
        }
    }
}
