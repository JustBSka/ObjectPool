using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ObjectPool.Tests
{
    /// <summary>
    /// These tests are checking object pool working in fixed mode.
    /// Fixed mode is when all pool items are passed in the constructor.
    /// </summary>
    [TestClass]
    public class FixedPoolTests
    {
        private static readonly int Count = 5;

        private IReadOnlyCollection<object> Pack;

        private IObjectPool<object> Pool;

        [TestInitialize]
        public void Init()
        {
            var list = new List<object>(Count);
            for (int i = 0; i < Count; i++)
                list.Add(new object());
            Pack = list;

            Pool = new ObjectPool<object>(Pack);
        }

        [TestMethod]
        public void FixedPool_UsePooling()
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < Pack.Count; j++)
                {
                    using (var obj = Pool.Take(out var number))
                    {
                        Assert.IsNotNull(number);
                    }
                }
            }
        }

        [TestMethod]
        public void FixedPool_CheckReferences()
        {
            for (int i = 0; i < Pack.Count * 2; i++)
            {
                var poolItem = Pool.Take(out var item);
                Assert.IsTrue(Pack.Contains(item));
                if (i % 2 == 0)
                    poolItem.Dispose();
            }
        }

        [TestMethod]
        public void FixedPool_Clearing()
        {
            var mock = new Mock<IClearable>();
            var pool = new ObjectPool<IClearable>(new[] { mock.Object });
            using (pool.Take(out var item))
            {
                Assert.AreEqual(mock.Object, item);
            }

            mock.Verify(m => m.Clear(), Times.Once());
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public void FixedPool_StuckUp()
        {
            var factory = new Func<IClearable>(() =>
            {
                var mock = new Mock<IClearable>();
                mock.Setup(m => m.Clear()).Throws(new Exception());
                return mock.Object;
            });

            var pack = new List<IClearable> { factory(), factory(), factory(), factory(), factory() };
            var pool = new ObjectPool<IClearable>(pack);
            for (int i = 0; i < pack.Count; i++)
            {
                using (pool.Take(out var item))
                {
                    Assert.IsNotNull(item);
                }
            }

            pool.Take(1, out _);
        }

        [TestMethod]
        public void FixedPool_TakeWithTimeout()
        {
            var pool = Pool;
            for (int i = 0; i < Pack.Count; i++)
                pool.Take(1, out _);

            Assert.ThrowsException<OperationCanceledException>(() => pool.Take(1, out _));
        }

        [TestMethod]
        public void FixedPool_TakeWithToken()
        {
            using (var cts = new CancellationTokenSource())
            {
                var pool = Pool;
                for (int i = 0; i < Pack.Count; i++)
                    pool.Take(cts.Token, out _);

                cts.Cancel();
                Assert.ThrowsException<OperationCanceledException>(() => pool.Take(cts.Token, out _));
            }
        }
    }
}
