
using System.Security.Cryptography.Pkcs;
using System.Text;
using HigginsSoft.DotMPack;
using Newtonsoft.Json;

namespace DotRpc.RpcWriterTests
{

    using static HigginsSoft.Utils.TestDataProvider;

    [TestClass()]
    public class RpcWriterTests : IDisposable
    {
        MemoryStream ms;
        private RpcWriter writer;
        BinaryReader reader;
        public RpcWriterTests()
        {
            ms = new MemoryStream();
            writer = new RpcWriter(ms, Encoding.UTF8, true);
            reader = new BinaryReader(ms, Encoding.UTF8, true);
        }

        public void TestNumericConversion<T>(Func<object, T>? converter = null)
        {
            using var ms = new MemoryStream();
            var writer = new RpcWriter(ms);

            var expected = GetNumericTestData<T>(converter).FirstOrDefault();

            var actual = writer.Convert<T>(1, converter);

            Assert.AreEqual(expected, actual);

        }



        void TestSerialize<T>(T expectedValue, Func<BinaryReader, T> read)
        {
            ms.Position = 0;
            writer.Write(expectedValue);

            ms.Position = 0;

            var actualWithNullFlag = reader.ReadByte();
            int nullFlag = (int)RpcTypes.Null;
            byte nullFlagMask = (byte)~nullFlag;

            var actualFullTypeByte = actualWithNullFlag & nullFlagMask;
            var actualRpcTypeByte = (actualFullTypeByte & 31);


            RpcTypes expectedFullRpcType = writer.GetRpcType<T>();
            var expectedFullTypeByte = (byte)expectedFullRpcType;
            var expectedRpcTypeByte = (expectedFullTypeByte & 31);



            var actualRpcType = (RpcTypes)actualRpcTypeByte;
            var actualIsNullable = (RpcTypes)(actualFullTypeByte & (byte)RpcTypes.Nullable);
            var actualIsNull = (RpcTypes)(actualWithNullFlag & (byte)RpcTypes.Null);

            var expectedRpcType = (RpcTypes)expectedRpcTypeByte;
            var expectedIsNullable = (RpcTypes)(expectedFullTypeByte & (byte)RpcTypes.Nullable);
            var expectedlIsNull = expectedValue is null ? RpcTypes.Null : RpcTypes.Unkown;


            byte expectedType = (byte)(expectedFullTypeByte & 31);
            Assert.AreEqual(expectedRpcType, actualRpcType, $"Unexpected RpcType for type {typeof(T)} : Expected {(RpcTypes)expectedRpcType} - Actual {(RpcTypes)actualFullTypeByte}");
            if (actualIsNull != RpcTypes.Null)
            {
                var value = read(reader);
                Assert.AreEqual(expectedValue, value, "Unexpected Value");
            }

            Assert.AreEqual(expectedlIsNull, actualIsNull);
            Assert.AreEqual(expectedIsNullable, actualIsNullable);
       

        }

        [TestMethod()]
        public void RpcWriterTest()
        {
            using (var ms = new MemoryStream())
            {
                var writer = new RpcWriter(ms);
                var expectedValue = 1;
                writer.Write(expectedValue);

                ms.Position = 0;
                var reader = new BinaryReader(ms);
                var type = reader.ReadByte();
                byte exptedType = (byte)RpcTypes.Int;
                Assert.AreEqual(exptedType, type);
                var value = reader.ReadInt32();
                Assert.AreEqual(expectedValue, value);
            }

        }

        [TestMethod()]
        public void ConvertTest()
        {
            using var ms = new MemoryStream();
            var writer = new RpcWriter(ms);



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
            TestNumericConversion<char>(x => (char)x.ToString()[0]);
            TestNumericConversion<string>(x => x.ToString());
            TestNumericConversion<DateTime>(x => DateTime.Now.AddDays(int.Parse(x.ToString())).Date);

        }

        [TestMethod()]
        public void TestPrimitives()
        {

            TestSerialize((byte)1, x => x.ReadByte());
            TestSerialize((sbyte)1, x => x.ReadSByte());
            TestSerialize((short)1, x => x.ReadInt16());
            TestSerialize((ushort)1, x => x.ReadUInt16());
            TestSerialize((int)1, x => x.ReadInt32());
            TestSerialize((uint)1, x => x.ReadUInt32());
            TestSerialize((long)1, x => x.ReadInt64());
            TestSerialize((ulong)1, x => x.ReadUInt64());
            TestSerialize((float)1, x => x.ReadSingle());
            TestSerialize((double)1, x => x.ReadDouble());
            TestSerialize((decimal)1, x => x.ReadDecimal());

            TestSerialize((string)"1", x => x.ReadString());
            TestSerialize((char)'1', x => x.ReadChar());
            TestSerialize((bool)true, x => x.ReadBoolean());

        }

        [TestMethod()]
        public void TestNullablePrimitivesWithValue()
        {

            TestSerialize((byte?)1, x => x.ReadByte());
            TestSerialize((sbyte?)1, x => x.ReadSByte());
            TestSerialize((short?)1, x => x.ReadInt16());
            TestSerialize((ushort?)1, x => x.ReadUInt16());
            TestSerialize((int?)1, x => x.ReadInt32());
            TestSerialize((uint?)1, x => x.ReadUInt32());
            TestSerialize((long?)1, x => x.ReadInt64());
            TestSerialize((ulong?)1, x => x.ReadUInt64());
            TestSerialize((float?)1, x => x.ReadSingle());
            TestSerialize((double?)1, x => x.ReadDouble());
            TestSerialize((decimal?)1, x => x.ReadDecimal());

            TestSerialize((string?)"1", x => x.ReadString());
            TestSerialize((char?)'1', x => x.ReadChar());
            TestSerialize((bool?)true, x => x.ReadBoolean());

        }

        [TestMethod()]
        public void TestNullablePrimitivesWithNull()
        {

            TestSerialize((byte?)null, x => x.ReadByte());
            TestSerialize((sbyte?)null, x => x.ReadSByte());
            TestSerialize((short?)null, x => x.ReadInt16());
            TestSerialize((ushort?)null, x => x.ReadUInt16());
            TestSerialize((int?)null, x => x.ReadInt32());
            TestSerialize((uint?)null, x => x.ReadUInt32());
            TestSerialize((long?)null, x => x.ReadInt64());
            TestSerialize((ulong?)null, x => x.ReadUInt64());
            TestSerialize((float?)null, x => x.ReadSingle());
            TestSerialize((double?)null, x => x.ReadDouble());
            TestSerialize((decimal?)null, x => x.ReadDecimal());

            TestSerialize((string?)null, x => x.ReadString());
            TestSerialize((char?)null, x => x.ReadChar());
            TestSerialize((bool?)null, x => x.ReadBoolean());

        }


        public void Dispose()
        {
            ((IDisposable)ms).Dispose();
        }
    }
}
