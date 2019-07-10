using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ObjectPool
{
    public sealed class ObjectPool<T>
        : IObjectPool<T>
    {
        private readonly BlockingCollection<T> _pool = new BlockingCollection<T>();
        private readonly int _maxSize;
        private readonly object _sync = new object();
        private readonly Func<T> _objectFactory;

        private readonly Func<T, IPoolItem<T>> _poolItemFactory;

        private int _size;

        private ObjectPool()
        {
            if (typeof(IClearable).IsAssignableFrom(typeof(T)))
                _poolItemFactory = new Func<T, IPoolItem<T>>((t) => new ClearablePoolItem<T>(t, this));
            else
                _poolItemFactory = new Func<T, IPoolItem<T>>((t) => new SimplePoolItem<T>(t, this));
        }

        public ObjectPool(Func<T> factory, int maxSize = 10)
            : this()
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (maxSize <= 0)
                throw new ArgumentException("Pool size must be greater than 0");

            _objectFactory = factory;
            _maxSize = maxSize;
        }

        public ObjectPool(IReadOnlyCollection<T> pool)
            : this()
        {
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));

            _maxSize = pool.Count;

            foreach (var item in pool)
                _pool.Add(item);

            _size = _pool.Count;
        }

        public ObjectPool(IReadOnlyCollection<T> initial, Func<T> factory, int maxSize = 10)
            : this()
        {
            if (initial == null)
                throw new ArgumentNullException(nameof(initial));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (maxSize <= 0)
                throw new ArgumentException("Pool size must be greater than 0");

            _objectFactory = factory;
            _maxSize = maxSize;

            var i = 0;
            foreach (var item in initial)
            {
                if (i >= _maxSize)
                    break;
                ++i;
                _pool.Add(item);
            }

            _size = _pool.Count;
        }

        public IDisposable Take(out T item)
        {
            if (TryTakeOrCreate(out var poolItem))
            {
                item = poolItem.Object;
                return poolItem;
            }

            item = _pool.Take();
            return _poolItemFactory(item);
        }

        public IDisposable Take(CancellationToken token, out T item)
        {
            return Take(0, token, out item);
        }

        public IDisposable Take(int milliseconds, out T item)
        {
            return Take(milliseconds, CancellationToken.None, out item);
        }

        public IDisposable Take(int milliseconds, CancellationToken token, out T item)
        {
            if (TryTakeOrCreate(out var poolItem))
            {
                item = poolItem.Object;
                return poolItem;
            }

            if (_pool.TryTake(out item, milliseconds, token))
                return _poolItemFactory(item);

            throw new OperationCanceledException("Pool waiting timeout expired.");
        }

        private bool TryTakeOrCreate(out IPoolItem<T> poolItem)
        {
            if (_pool.TryTake(out var item))
            {
                poolItem = _poolItemFactory(item);
                return true;
            }

            return TryCreate(out poolItem);
        }

        private bool TryCreate(out IPoolItem<T> poolItem)
        {
            lock (_sync)
            {
                if (_size < _maxSize)
                {
                    var item = _objectFactory();
                    ++_size;
                    poolItem = _poolItemFactory(item);
                    return true;
                }
            }
            poolItem = null;
            return false;
        }

        internal void Release(T item)
        {
            _pool.TryAdd(item);
        }

        internal void Free()
        {
            if (_objectFactory == null)
                return;

            lock (_sync)
            {
                --_size;
            }
        }
    }
}
