using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        //TODO IClearable tests
        //TODO try stuck
        //TODO undersize

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
    }
}
