using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HigginsSoft.DotMPack
{
    [DataContract]
    public class Pack
    {
        [DataMember(Order = 1)]
        public List<byte> Payload { get; set; } = new();

        public static implicit operator DotMPack(Pack request)
            => DotMPack.ParseFromBytes(request.Payload.ToArray());
        public static implicit operator Pack(DotMPack request)
            => new Pack() { Payload = request.EncodeToBytes().ToList() };

        public static implicit operator DotMPackMap(Pack request)
            => (DotMPackMap)DotMPack.ParseFromBytes(request.Payload.ToArray());
        public static implicit operator Pack(DotMPackMap request)
            => new Pack() { Payload = request.EncodeToBytes().ToList() };

        public static implicit operator DotMPackArray(Pack request)
            => (DotMPackArray)DotMPack.ParseFromBytes(request.Payload.ToArray());
        public static implicit operator Pack(DotMPackArray request)
            => new Pack() { Payload = request.EncodeToBytes().ToList() };


        public static implicit operator Pack(bool value) => DotMPack.From(value);
        public static implicit operator Pack(float value) => DotMPack.From(value);
        public static implicit operator Pack(double value) => DotMPack.From(value);
        public static implicit operator Pack(byte value) => DotMPack.From(value);
        public static implicit operator Pack(ushort value) => DotMPack.From(value);
        public static implicit operator Pack(uint value) => DotMPack.From(value);
        public static implicit operator Pack(ulong value) => DotMPack.From(value);
        public static implicit operator Pack(sbyte value) => DotMPack.From(value);
        public static implicit operator Pack(short value) => DotMPack.From(value);
        public static implicit operator Pack(int value) => DotMPack.From(value);
        public static implicit operator Pack(string value) => DotMPack.From(value);
        public static implicit operator Pack(byte[] value) => DotMPack.From(value);
        public static implicit operator Pack(Pack[] value) => DotMPack.From(value);
        public static implicit operator Pack(object[] value) => DotMPack.From(value.Select(x => DotMPack.From(x)).ToArray());


        public static implicit operator bool(Pack value) => (bool)(DotMPack)value;
        public static implicit operator float(Pack value) => (float)(DotMPack)value;
        public static implicit operator double(Pack value) => (double)(DotMPack)value;
        public static implicit operator byte(Pack value) => (byte)(DotMPack)value;
        public static implicit operator ushort(Pack value) => (ushort)(DotMPack)value;
        public static implicit operator uint(Pack value) => (uint)(DotMPack)value;
        public static implicit operator ulong(Pack value) => (ulong)(DotMPack)value;
        public static implicit operator sbyte(Pack value) => (sbyte)(DotMPack)value;
        public static implicit operator short(Pack value) => (short)(DotMPack)value;
        public static implicit operator int(Pack value) => (int)(DotMPack)value;
        public static implicit operator string(Pack value) => (string)(DotMPack)value;
        public static implicit operator byte[](Pack value) => (byte[])(DotMPack)value;
        public static implicit operator object[](Pack value) => (object[])(DotMPack)value;
    }


    [DataContract]
    public class PackArray
    {
        [DataMember(Order = 1)]
        public List<byte> Payload { get; set; } = new();

        public static implicit operator DotMPack(PackArray request)
          => DotMPack.ParseFromBytes(request.Payload.ToArray());
        public static implicit operator PackArray(DotMPack request)
            => new PackArray() { Payload = request.EncodeToBytes().ToList() };

        public static implicit operator DotMPackArray(PackArray request)
            => (DotMPackArray)DotMPack.ParseFromBytes(request.Payload.ToArray());
        public static implicit operator PackArray(DotMPackArray request)
            => new PackArray() { Payload = request.EncodeToBytes().ToList() };


        public static PackArray From<T>(T[] value)
            => From(value.Select(x => (object)x).ToArray());


        public static T To<T>(PackArray value) => ((DotMPack)value).To<T>();
        public T To<T>() => ((DotMPack)this).To<T>();



        public static PackArray From(sbyte[] value)
           => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(byte[] value)
            => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(short[] value)
            => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(ushort[] value)
           => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(int[] value)
            => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(uint[] value)
           => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(long[] value)
            => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(ulong[] value)
            => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(float[] value)
           => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(double[] value)
            => From(value.Select(x => (object)x).ToArray());


        public static PackArray From(bool[] value)
            => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(string[] value)
            => From(value.Select(x => (object)x).ToArray());
        public static PackArray From(DateTime[] value)
            => From(value.Select(x => (object)x).ToArray());

        public static PackArray From(object[] value)
        {
            var packs = value.Select(x => DotMPack.From(x)).ToArray();
            var mpack = DotMPack.From(packs);
            DotMPackArray mArray= mpack as DotMPackArray;
            var result = (PackArray)mArray;
            return (DotMPackArray)DotMPack.From(packs);
        }
      
        public static PackArray From(PackArray[] value)
            => (DotMPackArray)DotMPack.From(value.Select(DotMPack.From).ToArray());



        public static implicit operator PackArray(sbyte[] value) => From(value);
        public static implicit operator PackArray(byte[] value) => From(value);
        public static implicit operator PackArray(short[] value) => From(value);
        public static implicit operator PackArray(ushort[] value) => From(value);

        public static implicit operator PackArray(int[] value) => From(value);
        public static implicit operator PackArray(uint[] value) => From(value);
        public static implicit operator PackArray(long[] value) => From(value);
        public static implicit operator PackArray(ulong[] value) => From(value);

        public static implicit operator PackArray(float[] value) => From(value);
        public static implicit operator PackArray(double[] value) => From(value);

        public static implicit operator PackArray(bool[] value) => From(value);
        public static implicit operator PackArray(string[] value) => From(value);
        public static implicit operator PackArray(DateTime[] value) => From(value);

        public static implicit operator PackArray(object[] value) => From(value);
        public static implicit operator PackArray(PackArray[] value) => From(value);


        public static implicit operator sbyte[](PackArray value) => To<sbyte[]>(value);
        public static implicit operator byte[](PackArray value) => To<byte[]>(value);
        public static implicit operator short[](PackArray value) => To<short[]>(value);
        public static implicit operator ushort[](PackArray value) => To<ushort[]>(value);

        public static implicit operator int[](PackArray value) => To<int[]>(value);
        public static implicit operator uint[](PackArray value) => To<uint[]>(value);
        public static implicit operator long[](PackArray value) => To<long[]>(value);
        public static implicit operator ulong[](PackArray value) => To<ulong[]>(value);

        public static implicit operator float[](PackArray value) => To<float[]>(value);
        public static implicit operator double[](PackArray value) => To<double[]>(value);

        public static implicit operator bool[](PackArray value) => To<bool[]>(value);
        public static implicit operator DateTime[](PackArray value) => To<DateTime[]>(value);
        public static implicit operator string[](PackArray value) => To<string[]>(value);

        public static implicit operator object[](PackArray value) => (object[])(DotMPack)value;

     
    
    }

}
