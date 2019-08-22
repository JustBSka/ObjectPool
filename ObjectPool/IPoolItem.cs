using System;

namespace ObjectPool
{
    /// <summary>
    /// Interface of the container of a pooled object.
    /// </summary>
    /// <typeparam name="T">Type of the pooled object.</typeparam>
    public interface IPoolItem<T>
        : IDisposable
    {
        /// <summary>
        /// Pooled object.
        /// </summary>
        T Object { get; }
    }
}
