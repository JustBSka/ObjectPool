using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ObjectPool.Tests
{
    [TestClass]
    public class FactoryPoolTests
    {
        [TestMethod]
        public void FactoryPool_ThrowingFactory()
        {
            IObjectPool<object> pool = new ObjectPool<object>(() => throw new InvalidOperationException(), 1);
            Assert.ThrowsException<InvalidOperationException>(() => pool.Take(out _));
        }

        [TestMethod]
        public void FactoryPool_MaxSize()
        {
            int count = 0;
            IObjectPool<object> pool = new ObjectPool<object>(() => { ++count; return new object(); }, 1);
            pool.Take(out var item);
            Assert.IsNotNull(item);
            Assert.AreEqual(1, count);
            Assert.ThrowsException<OperationCanceledException>(() => pool.Take(1, out _));
        }

        [TestMethod]
        public void FactoryPool_TryStuck()
        {
            var factory = new Func<IClearable>(() =>
            {
                var mock = new Mock<IClearable>();
                mock.Setup(m => m.Clear()).Throws(new Exception());
                return mock.Object;
            });

            const int count = 5;

            IObjectPool<IClearable> pool = new ObjectPool<IClearable>(factory, 5);
            for (int i = 0; i < count; i++)
                using (pool.Take(out _)) { }
            pool.Take(out var item);
            Assert.IsNotNull(item);
        }

        [TestMethod]
        public void FactoryPool_CheckReferences()
        {
            const int count = 5;
            IObjectPool<object> pool = new ObjectPool<object>(() => new object(), count);

            var items = new HashSet<object>();
            var disposables = new List<IDisposable>();
            for (int i = 0; i < count; i++)
            {
                var disposable = pool.Take(out var item);
                disposables.Add(disposable);
                items.Add(item);
            }

            disposables.ForEach(d => d.Dispose());
            for (int i = 0; i < count * 2; i++)
            {
                var poolItem = pool.Take(out var item);
                Assert.IsTrue(items.Contains(item));
                if (i % 2 == 0)
                    poolItem.Dispose();
            }
        }

        [TestMethod]
        public void FactoryPool_TakeWithTimeout()
        {
            const int count = 5;
            IObjectPool<object> pool = new ObjectPool<object>(() => new object(), count);
            for (int i = 0; i < count; i++)
                pool.Take(1, out _);

            Assert.ThrowsException<OperationCanceledException>(() => pool.Take(1, out _));
        }

        [TestMethod]
        public void FactoryPool_TakeWithToken()
        {
            const int count = 5;
            using (var cts = new CancellationTokenSource())
            {
                IObjectPool<object> pool = new ObjectPool<object>(() => new object(), count);
                for (int i = 0; i < count; i++)
                    pool.Take(cts.Token, out _);

                cts.Cancel();
                Assert.ThrowsException<OperationCanceledException>(() => pool.Take(cts.Token, out _));
            }
        }
    }
}
