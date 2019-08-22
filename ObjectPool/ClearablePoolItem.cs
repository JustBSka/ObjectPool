using System;

namespace ObjectPool
{
    /// <summary>
    /// Example of pool item which can be cleared before returning to the pool.
    /// </summary>
    /// <typeparam name="T">Pooling object type.</typeparam>
    internal class ClearablePoolItem<T>
        : IPoolItem<T>
    {
        /// <summary>
        /// Object from the pool.
        /// </summary>
        public T Object { get; }

        private readonly ObjectPool<T> _pool;

        public ClearablePoolItem(T item, ObjectPool<T> pool)
        {
            Object = item;
            _pool = pool;
        }

        public void Dispose()
        {
            try
            {
                ((IClearable)Object).Clear();
                _pool.Release(Object);
            }
            catch (Exception)
            {
                _pool.Free();
            }
        }
    }
}
