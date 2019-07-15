using System;

namespace ObjectPool
{
    public interface IPoolItem<T>
        : IDisposable
    {
        T Object { get; }
    }
}
