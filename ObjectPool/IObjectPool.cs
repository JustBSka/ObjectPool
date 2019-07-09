using System;
using System.Threading;

namespace ObjectPool
{
    public interface IObjectPool<T>
    {
        IDisposable Take(out T item);

        IDisposable Take(CancellationToken token, out T item);

        IDisposable Take(int milliseconds, out T item);

        IDisposable Take(int milliseconds, CancellationToken token, out T item);
    }
}
