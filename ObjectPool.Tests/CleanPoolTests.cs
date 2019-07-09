using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectPool.Tests
{
    [TestClass]
    public class CleanPoolTests
    {
        [TestMethod]
        public void CleanPool_MaxSize()
        {
            int count = 0;
            IObjectPool<object> pool = new ObjectPool<object>(() => { ++count; return new object(); }, 1);
            pool.Take(out var item);
            Assert.IsNotNull(item);
            Assert.AreEqual(1, count);
            Assert.ThrowsException<OperationCanceledException>(() => pool.Take(1, out _));
        }

        [TestMethod]
        public void CleanPool_TryStuck()
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
    }
}
