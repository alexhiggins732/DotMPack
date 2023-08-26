namespace DotRpc
{



    public interface IRpcWriter
    {
        RpcBinaryWriter Writer { get; }

        T Convert<T>(object value);
        byte GetNullableBit(object? value);
        RpcTypes GetRpcType<T>();
        void Write(bool value, RpcBinaryWriter writer);
        void Write(bool? value, RpcBinaryWriter writer);
        void Write(byte value, RpcBinaryWriter writer);
        void Write(byte? value, RpcBinaryWriter writer);
        void Write(char value, RpcBinaryWriter writer);
        void Write(char? value, RpcBinaryWriter writer);
        void Write(decimal value, RpcBinaryWriter writer);
        void Write(decimal? value, RpcBinaryWriter writer);
        void Write(double value, RpcBinaryWriter writer);
        void Write(double? value, RpcBinaryWriter writer);
        void Write(float value, RpcBinaryWriter writer);
        void Write(float? value, RpcBinaryWriter writer);
        void Write(int value, RpcBinaryWriter writer);
        void Write(int? value, RpcBinaryWriter writer);
        void Write(long value, RpcBinaryWriter writer);
        void Write(long? value, RpcBinaryWriter writer);
        void Write(sbyte value, RpcBinaryWriter writer);
        void Write(sbyte? value, RpcBinaryWriter writer);
        void Write(short value, RpcBinaryWriter writer);
        void Write(short? value, RpcBinaryWriter writer);
        void Write(ulong value, RpcBinaryWriter writer);
        void Write(ulong? value, RpcBinaryWriter writer);
        void Write(ushort value, RpcBinaryWriter writer);
        void Write(ushort? value, RpcBinaryWriter writer);
        void Write<T>(T value);
        void Write<T>(T value, RpcBinaryWriter writer, RpcTypes rpcType = RpcTypes.Object);
        void Write<TKey, TValue>(Dictionary<TKey, TValue> value, RpcBinaryWriter writer);
        void WriteList<T>(List<T> value, RpcBinaryWriter writer);
    }

    public class IRpcWriterTests
    {

    }
}
