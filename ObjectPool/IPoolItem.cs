using System;

namespace ObjectPool
{
    internal interface IPoolItem<T>
        : IDisposable
    {
        T Object { get; }
    }
}
