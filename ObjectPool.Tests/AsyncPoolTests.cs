using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectPool.Tests
{
    [TestClass]
    public class AsyncPoolTests
    {
        [TestMethod]
        public async Task Async_Take()
        {
            const int count = 5;
            IObjectPool<object> pool = new ObjectPool<object>(() => new object(), count);
            for (int i = 0; i < count; i++)
            {
                var item = await pool.TakeAsync();
                Assert.IsNotNull(item.Object);
            }

            try
            {
                await pool.TakeAsync(0);
                Assert.Fail($"{nameof(OperationCanceledException)} is expected");
            }
            catch (OperationCanceledException)
            {
            }
        }

        [TestMethod]
        public async Task Async_TakeWithWaiting()
        {
            IObjectPool<object> pool = new ObjectPool<object>(() => new object(), 1);
            var noWaitTask = pool.TakeAsync();
            Assert.IsTrue(noWaitTask.IsCompleted);

            var waitTask = pool.TakeAsync();
            Assert.IsFalse(waitTask.IsCompleted);

            noWaitTask.Result.Dispose();
            await waitTask;
        }

        [TestMethod]
        public async Task Async_Cancel()
        {
            IObjectPool<object> pool = new ObjectPool<object>(() => new object(), 1);
            pool.Take(out _);

            using (var cts = new CancellationTokenSource())
            {
                var task = pool.TakeAsync(cts.Token);
                cts.Cancel();
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        [TestMethod]
        public async Task AsyncPool_TakeParallelCheckDistinct()
        {
            const int count = 100;
            var pack = new ConcurrentBag<object>();
            Parallel.For(0, count / 10, (_) => pack.Add(new object()));
            var pool = new ObjectPool<object>(pack, () => new object(), count);

            var retrieved = new ConcurrentBag<object>();
            var tasks = new List<Task>();

            const int tasksCount = count / 20;
            for (int i = 0; i < tasksCount; i++)
            {
                var task = Task.Run(async () =>
                {
                    for (int j = 0; j < count / tasksCount; j++)
                    {
                        var item = await pool.TakeAsync();
                        retrieved.Add(item.Object);
                    }
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            Assert.AreEqual(count, retrieved.Distinct().Count());
        }
    }
}
