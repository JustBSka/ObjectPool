using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectPool
{
    /// <summary>
    /// Default object pool implementation.
    /// </summary>
    /// <typeparam name="T">Type of pooled objects.</typeparam>
    public sealed class ObjectPool<T>
        : IObjectPool<T>
    {
        private readonly object _sync = new object();
        private readonly BlockingCollection<T> _pool = new BlockingCollection<T>();
        private readonly int _maxSize;
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

        /// <summary>
        /// Initializes a new pool instance with an item factory.
        /// </summary>
        /// <param name="factory">Pooled objects factory.</param>
        /// <param name="maxSize">Maximum pool size.</param>
        public ObjectPool(Func<T> factory, int maxSize = Int32.MaxValue)
            : this()
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (maxSize <= 0)
                throw new ArgumentException("Pool size must be greater than 0");

            _objectFactory = factory;
            _maxSize = maxSize;
        }

        /// <summary>
        /// Initializes a new pool instance with prepared object collection.
        /// </summary>
        /// <param name="pool">Pooled object collection.</param>
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

        /// <summary>
        /// Initializes warmed up pool instance with an item factory.
        /// </summary>
        /// <param name="initial">Collection of items to be inserted into the pool.</param>
        /// <param name="factory">Pooled objects factory.</param>
        /// <param name="maxSize">Maximum pool size.</param>
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

        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        /// <param name="item">Object returning from the pool.</param>
        /// <returns>An object that can be disposed to return the pooled object.</returns>
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

        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        /// <param name="token">Cancellation token to cancel waiting.</param>
        /// <param name="item">Object returning from the pool.</param>
        /// <returns>An object that can be disposed to return the pooled object.</returns>
        public IDisposable Take(CancellationToken token, out T item)
        {
            return Take(0, token, out item);
        }

        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        /// <param name="milliseconds">Waiting time in milliseconds or -1 to wait indefinitely.</param>
        /// <param name="item">Object returning from the pool.</param>
        /// <returns>An object that can be disposed to return the pooled object.</returns>
        public IDisposable Take(int milliseconds, out T item)
        {
            return Take(milliseconds, CancellationToken.None, out item);
        }

        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        /// <param name="milliseconds">Waiting time in milliseconds or -1 to wait indefinitely.</param>
        /// <param name="token">Cancellation token to cancel waiting.</param>
        /// <param name="item">Object returning from the pool.</param>
        /// <returns>An object that can be disposed to return the pooled object.</returns>
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

        /// <summary>
        /// Take an object from the pool asynchronously.
        /// </summary>
        /// <returns>A container with pooled object which can be disposed to return the object in the pool.</returns>
        public Task<IPoolItem<T>> TakeAsync()
        {
            if (TryTakeOrCreate(out var poolItem))
                return Task.FromResult(poolItem);

            return Task.Run(() =>
            {
                var item = _pool.Take();
                return _poolItemFactory(item);
            });
        }

        /// <summary>
        /// Take an object from the pool asynchronously.
        /// </summary>
        /// <param name="token">Cancellation token to cancel waiting.</param>
        /// <returns>A container with pooled object which can be disposed to return the object in the pool.</returns>
        public Task<IPoolItem<T>> TakeAsync(CancellationToken token)
        {
            return TakeAsync(0, token);
        }

        /// <summary>
        /// Take an object from the pool asynchronously.
        /// </summary>
        /// <param name="milliseconds">Waiting time in milliseconds or -1 to wait indefinitely.</param>
        /// <returns>A container with pooled object which can be disposed to return the object in the pool.</returns>
        public Task<IPoolItem<T>> TakeAsync(int milliseconds)
        {
            return TakeAsync(milliseconds, CancellationToken.None);
        }

        /// <summary>
        /// Take an object from the pool asynchronously.
        /// </summary>
        /// <param name="milliseconds">Waiting time in milliseconds or -1 to wait indefinitely.</param>
        /// <param name="token">Cancellation token to cancel waiting.</param>
        /// <returns>A container with pooled object which can be disposed to return the object in the pool.</returns>
        public Task<IPoolItem<T>> TakeAsync(int milliseconds, CancellationToken token)
        {
            if (TryTakeOrCreate(out var poolItem))
                return Task.FromResult(poolItem);

            return Task.Run(() =>
            {
                if (_pool.TryTake(out var item, milliseconds, token))
                    return _poolItemFactory(item);

                throw new OperationCanceledException("Pool waiting timeout expired.");
            });
        }
    }
}
