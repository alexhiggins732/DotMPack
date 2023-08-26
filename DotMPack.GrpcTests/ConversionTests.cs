using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HigginsSoft.Utils
{
    public class TestDataProvider
    {
        public static T[] GetNumericTestData<T>(Func<object, T>? converter = null)
        {
            if (converter == null)
                converter = (object value) => (T)(object)Convert.ChangeType(value, typeof(T));

            int[] values = { 1, 2, 3, 4 };

            T[] data = values.Select(x => converter(x)).ToArray();
            return data;
        }
    }
}
namespace HigginsSoft.DotMPack.GrpcTests
{

    using static HigginsSoft.Utils.TestDataProvider;
    [TestClass]
    public class PackArrayConversionTests
    {


        public void TestNumericConversion<T>(Func<object, T>? converter = null)
        {
            var expected = GetNumericTestData<T>(converter);

            PackArray packed = null!;
            try
            {
                packed = PackArray.From<T>(expected);
            }
            catch (Exception ex)
            {
                var data = JsonConvert.SerializeObject(expected);
                Console.WriteLine($"Error Created Packed Arry from {typeof(T)}[] {data}: {ex}");
                throw;
            }

            Assert.IsNotNull(packed, $"Unable To Create PackedArray for type {typeof(T)}[]");
            T[] actual = null!;
            try
            {
                actual = packed.To<T[]>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting packed to {typeof(T)}[]: {ex}");
                throw;
            }

            Assert.IsNotNull(actual, $"Unable To Create {typeof(T)}[] from Packed Array");
            Assert.AreEqual(expected.Length, actual.Length, "Unpacked array length not as expected");
            var expectedJson = JsonConvert.SerializeObject(expected, Formatting.Indented);
            var actualJson = JsonConvert.SerializeObject(actual, Formatting.Indented);
            Assert.IsTrue(expected.SequenceEqual(actual), $"Array comparison failed: Actual {actualJson} - Expected: {expectedJson}");
        }


        [TestMethod]
        public void PackArrayNumeric_ConversionTests()
        {

            TestNumericConversion<byte>();
            TestNumericConversion<sbyte>();
            TestNumericConversion<short>();
            TestNumericConversion<ushort>();

            TestNumericConversion<int>();
            TestNumericConversion<uint>();
            TestNumericConversion<long>();
            TestNumericConversion<ulong>();
            TestNumericConversion<float>();
            TestNumericConversion<double>();
            TestNumericConversion<decimal>();
            //TestNumericConversion<Guid>();
            //TestNumericConversion<TimeSpan>();

            TestNumericConversion<bool>(x => x.ToString() == "0");
            TestNumericConversion<char>(x => (char)x);
            TestNumericConversion<string>(x => x.ToString());
            TestNumericConversion<DateTime>(x => DateTime.Now.AddDays(int.Parse(x.ToString())));


        }



    }



}
