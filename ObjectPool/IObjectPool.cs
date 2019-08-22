using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectPool
{
    /// <summary>
    /// Object pool interface.
    /// </summary>
    /// <typeparam name="T">Type of pooled objects.</typeparam>
    public interface IObjectPool<T>
    {
        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        /// <param name="item">Object returning from the pool.</param>
        /// <returns>An object that can be disposed to return the pooled object.</returns>
        IDisposable Take(out T item);

        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        /// <param name="token">Cancellation token to cancel waiting.</param>
        /// <param name="item">Object returning from the pool.</param>
        /// <returns>An object that can be disposed to return the pooled object.</returns>
        IDisposable Take(CancellationToken token, out T item);

        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        /// <param name="milliseconds">Waiting time in milliseconds or -1 to wait indefinitely.</param>
        /// <param name="item">Object returning from the pool.</param>
        /// <returns>An object that can be disposed to return the pooled object.</returns>
        IDisposable Take(int milliseconds, out T item);

        /// <summary>
        /// Take an object from the pool.
        /// </summary>
        /// <param name="milliseconds">Waiting time in milliseconds or -1 to wait indefinitely.</param>
        /// <param name="token">Cancellation token to cancel waiting.</param>
        /// <param name="item">Object returning from the pool.</param>
        /// <returns>An object that can be disposed to return the pooled object.</returns>
        IDisposable Take(int milliseconds, CancellationToken token, out T item);

        /// <summary>
        /// Take an object from the pool asynchronously.
        /// </summary>
        /// <returns>A container with pooled object which can be disposed to return the object in the pool.</returns>
        Task<IPoolItem<T>> TakeAsync();

        /// <summary>
        /// Take an object from the pool asynchronously.
        /// </summary>
        /// <param name="token">Cancellation token to cancel waiting.</param>
        /// <returns>A container with pooled object which can be disposed to return the object in the pool.</returns>
        Task<IPoolItem<T>> TakeAsync(CancellationToken token);

        /// <summary>
        /// Take an object from the pool asynchronously.
        /// </summary>
        /// <param name="milliseconds">Waiting time in milliseconds or -1 to wait indefinitely.</param>
        /// <returns>A container with pooled object which can be disposed to return the object in the pool.</returns>
        Task<IPoolItem<T>> TakeAsync(int milliseconds);

        /// <summary>
        /// Take an object from the pool asynchronously.
        /// </summary>
        /// <param name="milliseconds">Waiting time in milliseconds or -1 to wait indefinitely.</param>
        /// <param name="token">Cancellation token to cancel waiting.</param>
        /// <returns>A container with pooled object which can be disposed to return the object in the pool.</returns>
        Task<IPoolItem<T>> TakeAsync(int milliseconds, CancellationToken token);
    }
}
