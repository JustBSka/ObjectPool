using System;

namespace ObjectPool
{
    internal class ClearablePoolItem<T>
        : IPoolItem<T>
    {
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
