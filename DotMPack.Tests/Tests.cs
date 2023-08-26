
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;


namespace HigginsSoft.DotMPack.Tests
{
    [TestClass()]
    public class Tests
    {
        [TestMethod()]
        public void TestDouble()
        {
            var tests = new[]
            {
                0d,
                1d,
                -1d,
                224d,
                256d,
                65530d,
                65540d,
                Double.NaN,
                Double.MaxValue,
                Double.MinValue,
                Double.PositiveInfinity,
                Double.NegativeInfinity
            };
            foreach (var value in tests)
            {
                Assert.AreEqual(value, DotMPack.ParseFromBytes(DotMPack.From(value).EncodeToBytes()).To<double>());
            }
        }

        [TestMethod()]
        public void TestNull()
        {
            Assert.AreEqual(null, DotMPack.ParseFromBytes(DotMPack.Null().EncodeToBytes()).To<object>());
        }
        [TestMethod()]
        public void TestString()
        {
            var tests = new string[]
            {
                Helpers.GetString(2),
                Helpers.GetString(8),
                Helpers.GetString(16),
                Helpers.GetString(32),
                Helpers.GetString(257),
                Helpers.GetString(65537)
            };
            foreach (var value in tests)
            {
                Assert.AreEqual(value, DotMPack.ParseFromBytes(DotMPack.From(value).EncodeToBytes()).To<string>());
            }
        }
        [TestMethod()]
        public void TestInteger()
        {
            var tests = new[]
            {
                0,
                1,
                -1,
                sbyte.MinValue,
                sbyte.MaxValue,
                byte.MaxValue,
                short.MinValue,
                short.MaxValue,
                int.MinValue,
                int.MaxValue,
                long.MaxValue,
                long.MinValue,
            };
            foreach (var value in tests)
            {
                Assert.AreEqual(value, DotMPack.ParseFromBytes(DotMPack.From(value).EncodeToBytes()).To<long>());
            }
        }
        [TestMethod()]
        public void TestMap()
        {
            DotMPackMap dictionary = new DotMPackMap
            {
                {
                    "array1", DotMPack.From(new[]
                    {
                        DotMPack.From("array1_value1"),
                        DotMPack.From("array1_value2"),
                        DotMPack.From("array1_value3"),
                    })
                },
                {"bool1", DotMPack.From(true)},
                {"double1", DotMPack.From(50.5)},
                {"double2", DotMPack.From(15.2)},
                {"int1", DotMPack.From(50505)},
                {"int2", DotMPack.From(50)},
                {3.14, DotMPack.From(3.14)},
                {42, DotMPack.From(42)}
            };
            
            var bytes = dictionary.EncodeToBytes();
            var result = DotMPack.ParseFromBytes(bytes) as DotMPackMap;
            Assert.AreEqual(dictionary, result);
        }
        [TestMethod()]
        public void TestArray()
        {
            var tests = new[]
            {
                (float) 0,
                (float) 50505,
                Single.NaN,
                Single.MaxValue,
                Single.MinValue,
                Single.PositiveInfinity,
                Single.NegativeInfinity,
                Single.Epsilon,
            }.Select(f => DotMPack.From(f))
            .ToArray();

            var arr = new DotMPackArray(tests);
            var bytes = arr.EncodeToBytes();
            var round = DotMPack.ParseFromBytes(bytes) as DotMPackArray;

            Assert.IsNotNull(round);
            Assert.IsTrue(arr.Count == round.Count);
            for (int i = 0; i < arr.Count; i++)
            {
                Assert.AreEqual(arr[i], round[i]);
            }
            Assert.AreEqual(arr, round);
        }
        [TestMethod()]
        public void TestUInt64()
        {
            var tests = new[]
            {
                UInt64.MaxValue,
                UInt64.MinValue,
            };
            foreach (var value in tests)
            {
                Assert.AreEqual(value, DotMPack.ParseFromBytes(DotMPack.From(value).EncodeToBytes()).To<ulong>());
            }
        }
        [TestMethod()]
        public void TestBoolean()
        {
            var tru = DotMPack.ParseFromBytes(DotMPack.From(true).EncodeToBytes()).To<bool>();
            var fal = DotMPack.ParseFromBytes(DotMPack.From(false).EncodeToBytes()).To<bool>();
            Assert.IsTrue(tru);
            Assert.IsFalse(fal);
        }
        [TestMethod()]
        public void TestSingle()
        {
            var tests = new[]
            {
                (float)0,
                (float)50505,
                Single.NaN,
                Single.MaxValue,
                Single.MinValue,
                Single.PositiveInfinity,
                Single.NegativeInfinity,
                Single.Epsilon,
            };
            foreach (var value in tests)
            {
                Assert.AreEqual(value, DotMPack.ParseFromBytes(DotMPack.From(value).EncodeToBytes()).To<float>());
            }
        }
        [TestMethod()]
        public void TestBinary()
        {
            var tests = new[]
            {
                Helpers.GetBytes(8),
                Helpers.GetBytes(16),
                Helpers.GetBytes(32),
                Helpers.GetBytes(257),
                Helpers.GetBytes(65537)
            };
            foreach (var value in tests)
            {
                var result = DotMPack.ParseFromBytes(DotMPack.From(value).EncodeToBytes()).To<byte[]>();
                Assert.IsTrue(Enumerable.SequenceEqual(value, result));
            }
        }
    }
}
