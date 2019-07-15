using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectPool.Tests
{
    [TestClass]
    public class PreparedPoolTests
    {
        private static IReadOnlyCollection<object> Pack(int size = 5)
        {
            var list = new List<object>();
            for (int i = 0; i < size; i++)
                list.Add(new object());
            return list;
        }

        [TestMethod]
        public void PreparedPool_ThrowingFactory()
        {
            const int count = 10;
            var pack = Pack();
            IObjectPool<object> pool = new ObjectPool<object>(pack, () => throw new InvalidOperationException(), count);
            for (int i = 0; i < pack.Count; i++)
                pool.Take(out _);

            Assert.ThrowsException<InvalidOperationException>(() => pool.Take(out _));
        }

        [TestMethod]
        public void PreparedPool_Undersize()
        {
            const int count = 3;
            var pack = Pack();
            IObjectPool<object> pool = new ObjectPool<object>(pack, () => new object(), count);
            for (int i = 0; i < count; i++)
            {
                pool.Take(out var item);
                Assert.IsTrue(pack.Contains(item));
            }

            Assert.ThrowsException<OperationCanceledException>(() => pool.Take(1, out _));
        }

        [TestMethod]
        public void PreparedPool_CheckReferences()
        {
            const int count = 10;
            var pack = Pack();
            IObjectPool<object> pool = new ObjectPool<object>(pack, () => new object(), count);
            for (int i = 0; i < pack.Count; i++)
            {
                pool.Take(out var item);
                Assert.IsTrue(pack.Contains(item));
            }

            for (int i = 0; i < count - pack.Count; i++)
            {
                pool.Take(out var item);
                Assert.IsFalse(pack.Contains(item));
            }
        }

        [TestMethod]
        public void PreparedPool_TryStuck()
        {
            var factory = new Func<IClearable>(() =>
            {
                var mock = new Mock<IClearable>();
                mock.Setup(m => m.Clear()).Throws(new Exception());
                return mock.Object;
            });

            const int count = 10;
            var pack = new List<IClearable>();
            for (int i = 0; i < count / 2; i++)
                pack.Add(factory());

            var pool = new ObjectPool<IClearable>(pack, factory, count);

            for (int i = 0; i < count; i++)
            {
                using (pool.Take(out var item))
                {
                    Assert.IsNotNull(item);
                }
            }

            pool.Take(1, out var notStucked);
            Assert.IsNotNull(notStucked);
        }

        [TestMethod]
        public void PreparedPool_TakeWithTimeout()
        {
            const int count = 10;
            IObjectPool<object> pool = new ObjectPool<object>(Pack(), () => new object(), count);
            for (int i = 0; i < count; i++)
                pool.Take(1, out _);

            Assert.ThrowsException<OperationCanceledException>(() => pool.Take(1, out _));
        }

        [TestMethod]
        public void PreparedPool_TakeWithToken()
        {
            const int count = 10;
            using (var cts = new CancellationTokenSource())
            {
                IObjectPool<object> pool = new ObjectPool<object>(Pack(), () => new object(), count);
                for (int i = 0; i < count; i++)
                    pool.Take(cts.Token, out _);

                cts.Cancel();
                Assert.ThrowsException<OperationCanceledException>(() => pool.Take(cts.Token, out _));
            }
        }

        [TestMethod]
        public void PreparedPool_TakeParallelCheckDistinct()
        {
            const int count = 100;
            var pack = new ConcurrentBag<object>();
            Parallel.For(0, count / 10, (_) => pack.Add(new object()));
            var pool = new ObjectPool<object>(pack, () => new object(), count);

            var retrieved = new ConcurrentBag<object>();
            var opts = new ParallelOptions { MaxDegreeOfParallelism = 5 };
            Parallel.For(0, count, opts, (_) =>
            {
                pool.Take(out var item);
                retrieved.Add(item);
            });

            Assert.AreEqual(count, retrieved.Distinct().Count());
        }
    }
}
