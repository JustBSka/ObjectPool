using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectPool
{
    public interface IObjectPool<T>
    {
        IDisposable Take(out T item);

        IDisposable Take(CancellationToken token, out T item);

        IDisposable Take(int milliseconds, out T item);

        IDisposable Take(int milliseconds, CancellationToken token, out T item);

        Task<IPoolItem<T>> TakeAsync();

        Task<IPoolItem<T>> TakeAsync(CancellationToken token);

        Task<IPoolItem<T>> TakeAsync(int milliseconds);

        Task<IPoolItem<T>> TakeAsync(int milliseconds, CancellationToken token);
    }
}
