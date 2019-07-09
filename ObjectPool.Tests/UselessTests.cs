using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ObjectPool.Tests
{
    /// <summary>
    /// These tests are checking if constructors are throwing on wrong parameters.
    /// Who does that?
    /// </summary>
    [TestClass]
    public class UselessTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Useless_Param1()
        {
            new ObjectPool<int>(null, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Useless_Param2()
        {
            new ObjectPool<int>(() => 1, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Useless_Param3()
        {
            new ObjectPool<int>((int[])null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Useless_Param4()
        {
            new ObjectPool<int>(null, () => 1, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Useless_Param5()
        {
            new ObjectPool<int>(new[] { 1 }, null, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Useless_Param6()
        {
            new ObjectPool<int>(new[] { 1 }, () => 1, 0);
        }
    }
}
