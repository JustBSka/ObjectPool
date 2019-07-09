namespace ObjectPool
{
    internal class SimplePoolItem<T>
        : IPoolItem<T>
    {
        public T Object { get; }

        private readonly ObjectPool<T> _pool;

        public SimplePoolItem(T item, ObjectPool<T> pool)
        {
            Object = item;
            _pool = pool;
        }

        public void Dispose()
        {
            _pool.Release(Object);
        }
    }
}
