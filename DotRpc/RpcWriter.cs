using System.Buffers.Binary;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using Humanizer;
using System;
using DotRpc;

namespace DotRpc
{


    public class SR
    {
        public static readonly string StreamNotWritable = nameof(StreamNotWritable).Humanize();
        public static readonly string SurrogatesNotAllowedAsSingleChar = nameof(SurrogatesNotAllowedAsSingleChar).Humanize();
        public static readonly string ArgumentOutOfRange_NeedNonNegativeNumber = nameof(ArgumentOutOfRange_NeedNonNegativeNumber).Humanize();
        public static readonly string ArgumentOutOfRange_IndexCount = nameof(ArgumentOutOfRange_IndexCount).Humanize();
    }

    public enum RpcTypes
    {
        Unkown,
        Byte,
        SByte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        Decimal,
        Char,
        Bool,
        String,
        Date,
        Object,
        Generic,
        Poco,

        Nullable = 32 | Generic,
        Null = 64,
        HasAttributes = 128
    }

    [Flags]
    public enum RpcTypeAttributes
    {
        None,
        Dictionary,
        List,
        Array
    }


    public class RpcWriter : IRpcWriter
    {
        private Delegate CompileSerializer<T>()
        {
            Action<T, RpcBinaryWriter> fn = null!;

            if (typeof(T) == typeof(byte))
            {
                fn = (value, writer) => Write(Convert<byte>(value), writer);
            }
            else if (typeof(T) == typeof(byte?))
            {
                fn = (value, writer) => Write(Convert<byte?>(value), writer);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                fn = (value, writer) => Write(Convert<sbyte>(value), writer);
            }
            else if (typeof(T) == typeof(sbyte?))
            {
                fn = (value, writer) => Write(Convert<sbyte?>(value), writer);
            }
            else if (typeof(T) == typeof(short))
            {
                fn = (value, writer) => Write(Convert<short>(value), writer);
            }
            else if (typeof(T) == typeof(short?))
            {
                fn = (value, writer) => Write(Convert<short?>(value), writer);
            }
            else if (typeof(T) == typeof(ushort))
            {
                fn = (value, writer) => Write(Convert<ushort>(value), writer);
            }
            else if (typeof(T) == typeof(ushort?))
            {
                fn = (value, writer) => Write(Convert<ushort?>(value), writer);
            }
            else if (typeof(T) == typeof(int))
            {
                fn = (value, writer) => Write(Convert<int>(value), writer);
            }
            else if (typeof(T) == typeof(int?))
            {
                fn = (value, writer) => Write(Convert<int?>(value), writer);
            }
            else if (typeof(T) == typeof(long))
            {
                fn = (value, writer) => Write(Convert<long>(value), writer);
            }
            else if (typeof(T) == typeof(long?))
            {
                fn = (value, writer) => Write(Convert<long?>(value), writer);
            }
            else if (typeof(T) == typeof(ulong))
            {
                fn = (value, writer) => Write(Convert<ulong>(value), writer);
            }
            else if (typeof(T) == typeof(ulong?))
            {
                fn = (value, writer) => Write(Convert<ulong?>(value), writer);
            }
            else if (typeof(T) == typeof(float))
            {
                fn = (value, writer) => Write(Convert<float>(value), writer);
            }
            else if (typeof(T) == typeof(float?))
            {
                fn = (value, writer) => Write(Convert<float?>(value), writer);
            }
            else if (typeof(T) == typeof(double))
            {
                fn = (value, writer) => Write(Convert<double>(value), writer);
            }
            else if (typeof(T) == typeof(double?))
            {
                fn = (value, writer) => Write(Convert<double?>(value), writer);
            }
            else if (typeof(T) == typeof(decimal))
            {
                fn = (value, writer) => Write(Convert<decimal>(value), writer);
            }
            else if (typeof(T) == typeof(decimal?))
            {
                fn = (value, writer) => Write(Convert<decimal?>(value), writer);
            }
            else if (typeof(T) == typeof(char))
            {
                fn = (value, writer) => Write(Convert<char>(value), writer);
            }
            else if (typeof(T) == typeof(char?))
            {
                fn = (value, writer) => Write(Convert<char?>(value), writer);
            }
            else if (typeof(T) == typeof(bool))
            {
                fn = (value, writer) => Write(Convert<bool>(value), writer);
            }
            else if (typeof(T) == typeof(bool?))
            {
                fn = (value, writer) => Write(Convert<bool?>(value), writer);
            }
            else if (typeof(T) == typeof(string))
            {
                fn = (value, writer) => Write(Convert<string>(value), writer);
            }
            else if (typeof(T) == typeof(DateTime?))
            {
                fn = (value, writer) => Write(Convert<DateTime?>(value), writer);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                fn = (value, writer) => Write(Convert<DateTime>(value), writer);
            }
            else
            {
                fn = (value, writer) => Write(Convert<T>(value), writer, RpcTypes.Object);
            }
            return fn;
        }

        public static ConcurrentDictionary<Type, Delegate> WriteFactory = new();
        public RpcBinaryWriter Writer { get; }

        public RpcWriter(Stream OutputSteam, Encoding? encoding = null, bool leaveOpen = false)
        {
            encoding = encoding ?? Encoding.UTF8;
            Writer = new RpcBinaryWriter(OutputSteam, encoding, leaveOpen);
        }

        public void Write<T>(T value)
        {
            var fn = WriteFactory.GetOrAdd(typeof(T), type => CompileSerializer<T>());
            var act = (Action<T, RpcBinaryWriter>)fn;
            act(value, Writer);
        }


        public T Convert<T>(object value) => Convert<T>(value, null);

        public T Convert<T>(object value, Func<object, T>? converter = null)
        {
            if (value is null) return default;
            if (value is T valueAsT) return valueAsT;

            var typeOfT = typeof(T);
            if (converter == null && typeOfT.IsValueType && value is IConvertible ic)
            {
                if (typeOfT.IsGenericType && typeOfT.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(typeOfT);
                    if (underlyingType != null && value != null)
                    {
                        converter = (object value) => (T)System.Convert.ChangeType(value, underlyingType);
                    }
                }
                else
                {
                    converter = (object value) => (T)System.Convert.ChangeType(value, typeof(T));
                }
            }


            if (converter is null)
            {
                converter = (x) => (T)x;
            }

            return converter(value);

        }

        public byte GetNullableBit(object? value)
        => (byte)(value is null ? 128 : 0);

        public void Write2<T>(object value, RpcBinaryWriter writer)
        {
            Write((T)value, writer);
        }
        public void Write(byte value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.Byte);
            writer.Write((byte)value);
        }
        public void Write(byte? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Byte | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write((byte)value);
        }

        public void Write(sbyte value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.SByte);
            writer.Write((byte)value);
        }
        public void Write(sbyte? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.SByte | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write((byte)value);
        }

        public void Write(short value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.Short);
            writer.Write(value);
        }
        public void Write(short? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Short | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write(value.Value);
        }

        public void Write(ushort value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.UShort);
            writer.Write(value);
        }
        public void Write(ushort? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.UShort | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write(value.Value);
        }

        public void Write(int value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.Int);
            writer.Write(value);
        }
        public void Write(int? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Int | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write(value.Value);
        }

        public void Write(long value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.Long);
            writer.Write(value);
        }
        public void Write(long? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Long | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write(value.Value);
        }

        public void Write(ulong value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.ULong);
            writer.Write(value);
        }
        public void Write(ulong? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.ULong | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write(value.Value);
        }

        public void Write(float value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.Float);
            writer.Write(value);
        }
        public void Write(float? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Float | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write(value.Value);
        }

        public void Write(double value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.Double);
            writer.Write(value);
        }
        public void Write(double? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Double | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));

            if (value.HasValue)
                writer.Write(value.Value);
        }

        public void Write(decimal value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.Decimal);
            writer.Write(value);
        }
        public void Write(decimal? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Decimal | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write(value.Value);
        }

        public void Write(bool value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.Bool);
            writer.Write(value);
        }
        public void Write(bool? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Bool | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write(value.Value);
        }

        public void Write(char value, RpcBinaryWriter writer)
        {
            writer.Write((byte)RpcTypes.Char);
            writer.Write(value);
        }
        public void Write(char? value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Char | RpcTypes.Nullable | (value is null ? RpcTypes.Null : 0)));
            if (value.HasValue)
                writer.Write(value.Value);
        }


        public void Write<T>(T value, RpcBinaryWriter writer, RpcTypes rpcType = RpcTypes.Object)
        {
            if (rpcType == RpcTypes.Object)
            {
                rpcType = GetRpcType<T>();
                if (value is null) rpcType = rpcType | RpcTypes.Null;
            }
            writer.Write((byte)rpcType);
            if (value is not null)
            {
                if ((rpcType & RpcTypes.Nullable) == 0)
                {
                    switch (rpcType)
                    {
                        case RpcTypes.Byte: writer.Write(Convert<byte>(value)); break;
                        case RpcTypes.SByte: writer.Write(Convert<sbyte>(value)); break;
                        case RpcTypes.Short: writer.Write(Convert<short>(value)); break;
                        case RpcTypes.Int: writer.Write(Convert<int>(value)); break;
                        case RpcTypes.UInt: writer.Write(Convert<uint>(value)); break;
                        case RpcTypes.Long: writer.Write(Convert<long>(value)); break;
                        case RpcTypes.ULong: writer.Write(Convert<ulong>(value)); break;
                        case RpcTypes.Float: writer.Write(Convert<byte>(value)); break;
                        case RpcTypes.Double: writer.Write(Convert<byte>(value)); break;
                        case RpcTypes.Decimal: writer.Write(Convert<byte>(value)); break;
                        case RpcTypes.Char: writer.Write(Convert<Char>(value)); break;
                        case RpcTypes.Bool: writer.Write(Convert<bool>(value)); break;
                        case RpcTypes.Date: writer.Write(Convert<DateTime>(value)); break;
                        case RpcTypes.String: writer.Write(Convert<string>(value)); break;
                    }
                }

            }

        }

        //public enum RpcTypes
        //{
        //    Unkown,
        //    Byte,
        //    SByte,
        //    Short,
        //    UShort,
        //    Int,
        //    UInt,
        //    Long,
        //    ULong,
        //    Float,
        //    Double,
        //    Decimal,
        //    Char,
        //    Bool,
        //    String,
        //    Date,
        //    Object,
        //    Generic,
        //    Poco,

        //    Nullable = 32,
        //    Null = 64,
        //    HasAttributes = 128
        //}

        public RpcTypes GetRpcType<T>()
        {
            var t = typeof(T);
            bool isGeneric = t.IsGenericType;
            bool isNullable = false;
            if (isGeneric && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = Nullable.GetUnderlyingType(t);
                isNullable = true;
            }
            var genericFlag = !isGeneric ? 0 : RpcTypes.Generic;
            var nullableFlag = !isNullable ? 0 : RpcTypes.Nullable;
            var flags = genericFlag | nullableFlag;
            var type =  RpcTypes.Unkown;
            if (!t.IsGenericType) //todo handle nested generics
            {
                if (t == typeof(byte)) type = RpcTypes.Byte;
                else if (t == typeof(sbyte)) type = RpcTypes.SByte;
                else if (t == typeof(short)) type = RpcTypes.Short;
                else if (t == typeof(ushort)) type = RpcTypes.UShort;
                else if (t == typeof(int)) type = RpcTypes.Int;
                else if (t == typeof(uint)) type = RpcTypes.UInt;
                else if (t == typeof(long)) type = RpcTypes.Long;
                else if (t == typeof(ulong)) type = RpcTypes.ULong;
                else if (t == typeof(float)) type = RpcTypes.Float;
                else if (t == typeof(double)) type = RpcTypes.Double;
                else if (t == typeof(decimal)) type = RpcTypes.Decimal;
                else if (t == typeof(char)) type = RpcTypes.Char;
                else if (t == typeof(bool)) type = RpcTypes.Bool;
                else if (t == typeof(string)) type = RpcTypes.String;
                else if (t == typeof(DateTime)) type = RpcTypes.Date;
                else if (t == typeof(object)) type = RpcTypes.Object;
                else type = RpcTypes.Poco;
            }
            else

            {
                // Handle generic types
                // You might need to further analyze the generic type's structure to determine its RpcType
                type =  RpcTypes.Generic;
            }
            return type | flags;
        }

        public void Write<TKey, TValue>(Dictionary<TKey, TValue> value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Generic | RpcTypes.HasAttributes | (value is null ? RpcTypes.Null : 0)));
            writer.Write((byte)RpcTypeAttributes.List);
            var typeKey = GetRpcType<TKey>();
            var valueKeyType = GetRpcType<TValue>();
            writer.Write((byte)typeKey);   // Write the type of TKey
            writer.Write((byte)valueKeyType);  // Write the type of TValue

            // Write the dictionary data, e.g., count and key-value pairs
            writer.Write(value.Count);
            foreach (var kvp in value)
            {
                Write(kvp.Key, writer, typeKey);
                Write(kvp.Value, writer, valueKeyType);
            }
        }

        public void WriteList<T>(List<T> value, RpcBinaryWriter writer)
        {
            writer.Write((byte)(RpcTypes.Generic | RpcTypes.HasAttributes | (value is null ? RpcTypes.Null : 0)));
            writer.Write((byte)RpcTypeAttributes.List);
            Write(typeof(T), writer);   // Write the type of the list elements

            // Write the list data, e.g., count and elements
            writer.Write(value.Count);
            foreach (var item in value)
            {
                Write(item, writer);
            }
        }

    }


    // Adapted from System.IO.BinaryWriter.cs
    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.


    // This abstract base class represents a writer that can write
    // primitives to an arbitrary stream. A subclass can override methods to
    // give unique encodings.
    //
    public class RpcBinaryWriter : IDisposable, IAsyncDisposable
    {
        private const int MaxArrayPoolRentalSize = 64 * 1024; // try to keep rentals to a reasonable size

        public static readonly RpcBinaryWriter Null = new RpcBinaryWriter();

        protected Stream OutStream;
        private readonly BinaryWriter SysWriter;
        private readonly Encoding _encoding;
        private readonly bool _leaveOpen;
        private readonly bool _useFastUtf8;

        // Protected default constructor that sets the output stream
        // to a null stream (a bit bucket).
        protected RpcBinaryWriter()
        {
            OutStream = Stream.Null;
            _encoding = Encoding.UTF8;
            _useFastUtf8 = true;

        }

        // BinaryWriter never emits a BOM, so can use Encoding.UTF8 fast singleton
        public RpcBinaryWriter(Stream output) : this(output, Encoding.UTF8, false)
        {
        }

        public RpcBinaryWriter(Stream output, Encoding encoding) : this(output, encoding, false)
        {
        }

        public RpcBinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (!output.CanWrite)
                throw new ArgumentException(SR.StreamNotWritable);

            OutStream = output;
            SysWriter = new BinaryWriter(output, encoding, true);
            _encoding = encoding;
            _leaveOpen = leaveOpen;

            _useFastUtf8 = IsUTF8CodePage(encoding) && encoding.EncoderFallback.MaxCharCount <= 1;
        }

        bool IsUTF8CodePage(Encoding encoding) => encoding.CodePage == CodePageUTF8;
        const int CodePageUTF8 = 65001;
        // Closes this writer and releases any system resources associated with the
        // writer. Following a call to Close, any operations on the writer
        // may raise exceptions.
        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_leaveOpen)
                    OutStream.Flush();
                else
                    OutStream.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual ValueTask DisposeAsync()
        {
            try
            {
                if (GetType() == typeof(BinaryWriter))
                {
                    if (_leaveOpen)
                    {
                        return new ValueTask(OutStream.FlushAsync());
                    }

                    OutStream.Close();
                }
                else
                {
                    // Since this is a derived BinaryWriter, delegate to whatever logic
                    // the derived implementation already has in Dispose.
                    Dispose();
                }

                return default;
            }
            catch (Exception exc)
            {
                return ValueTask.FromException(exc);
            }
        }

        // Returns the stream associated with the writer. It flushes all pending
        // writes before returning. All subclasses should override Flush to
        // ensure that all buffered data is sent to the stream.
        public virtual Stream BaseStream
        {
            get
            {
                Flush();
                return OutStream;
            }
        }

        // Clears all buffers for this writer and causes any buffered data to be
        // written to the underlying device.
        public virtual void Flush()
        {
            OutStream.Flush();
        }

        public virtual long Seek(int offset, SeekOrigin origin)
        {
            return OutStream.Seek(offset, origin);
        }

        // Writes a boolean to this stream. A single byte is written to the stream
        // with the value 0 representing false or the value 1 representing true.
        //
        public virtual void Write(bool value) => OutStream.WriteByte((byte)(value ? 1 : 0));

        // Writes a boolean to this stream. A single byte is written to the stream
        // with the value 0 representing false or the value 1 representing true.
        //
        public virtual void Write(DateTime value) => Write(value.ToFileTimeUtc());

        // Writes a byte to this stream. The current position of the stream is
        // advanced by one.
        //
        public virtual void Write(byte value) => OutStream.WriteByte(value);

        // Writes a signed byte to this stream. The current position of the stream
        // is advanced by one.
        //

        public virtual void Write(sbyte value) => OutStream.WriteByte((byte)value);

        // Writes a byte array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        //
        public virtual void Write(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            OutStream.Write(buffer, 0, buffer.Length);
        }

        // Writes a section of a byte array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        //
        public virtual void Write(byte[] buffer, int index, int count)
        {
            OutStream.Write(buffer, index, count);
        }

        // Writes a character to this stream. The current position of the stream is
        // advanced by two.
        // Note this method cannot handle surrogates properly in UTF-8.
        //
        public virtual void Write(char ch)
        {
            if (!Rune.TryCreate(ch, out Rune rune)) // optimistically assume UTF-8 code path (which uses Rune) will be hit
            {
                throw new ArgumentException(SR.SurrogatesNotAllowedAsSingleChar);
            }

            Span<byte> buffer = stackalloc byte[8]; // reasonable guess for worst-case expansion for any arbitrary encoding

            if (_useFastUtf8)
            {
                int utf8ByteCount = rune.EncodeToUtf8(buffer);
                OutStream.Write(buffer.Slice(0, utf8ByteCount));
            }
            else
            {
                byte[]? rented = null;
                int maxByteCount = _encoding.GetMaxByteCount(1);

                if (maxByteCount > buffer.Length)
                {
                    rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
                    buffer = rented;
                }

                int actualByteCount = _encoding.GetBytes(MemoryMarshal.CreateReadOnlySpan(ref ch, 1), buffer);
                OutStream.Write(buffer.Slice(0, actualByteCount));

                if (rented != null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        // Writes a character array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        //
        public virtual void Write(char[] chars)
        {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            WriteCharsCommonWithoutLengthPrefix(chars, useThisWriteOverride: false);
        }

        // Writes a section of a character array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        //
        public virtual void Write(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegativeNumber);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegativeNumber);
            if (index > chars.Length - count)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexCount);

            WriteCharsCommonWithoutLengthPrefix(chars.AsSpan(index, count), useThisWriteOverride: false);
        }

        // Writes a double to this stream. The current position of the stream is
        // advanced by eight.
        //
        public virtual void Write(double value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(double)];
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        public virtual void Write(decimal value)
        {
            SysWriter.Write(value);

        }

        // Writes a two-byte signed integer to this stream. The current position of
        // the stream is advanced by two.
        //
        public virtual void Write(short value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a two-byte unsigned integer to this stream. The current position
        // of the stream is advanced by two.
        //

        public virtual void Write(ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a four-byte signed integer to this stream. The current position
        // of the stream is advanced by four.
        //
        public virtual void Write(int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        //
        public virtual void Write(uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes an eight-byte signed integer to this stream. The current position
        // of the stream is advanced by eight.
        //
        public virtual void Write(long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes an eight-byte unsigned integer to this stream. The current
        // position of the stream is advanced by eight.
        //

        public virtual void Write(ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a float to this stream. The current position of the stream is
        // advanced by four.
        //
        public virtual void Write(float value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a half to this stream. The current position of the stream is
        // advanced by two.
        //
        public virtual void Write(Half value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort) /* = sizeof(Half) */];
            BinaryPrimitives.WriteHalfLittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a length-prefixed string to this stream in the BinaryWriter's
        // current Encoding. This method first writes the length of the string as
        // an encoded unsigned integer with variable length, and then writes that many characters
        // to the stream.
        //
        public virtual void Write(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Common: UTF-8, small string, avoid 2-pass calculation
            // Less common: UTF-8, large string, avoid 2-pass calculation
            // Uncommon: excessively large string or not UTF-8

            if (_useFastUtf8)
            {
                if (value.Length <= 127 / 3)
                {
                    // Max expansion: each char -> 3 bytes, so 127 bytes max of data, +1 for length prefix
                    Span<byte> buffer = stackalloc byte[128];
                    int actualByteCount = _encoding.GetBytes(value, buffer.Slice(1));
                    buffer[0] = (byte)actualByteCount; // bypass call to Write7BitEncodedInt
                    OutStream.Write(buffer.Slice(0, actualByteCount + 1 /* length prefix */));
                    return;
                }
                else if (value.Length <= MaxArrayPoolRentalSize / 3)
                {
                    byte[] rented = ArrayPool<byte>.Shared.Rent(value.Length * 3); // max expansion: each char -> 3 bytes
                    int actualByteCount = _encoding.GetBytes(value, rented);
                    Write7BitEncodedInt(actualByteCount);
                    OutStream.Write(rented, 0, actualByteCount);
                    ArrayPool<byte>.Shared.Return(rented);
                    return;
                }
            }

            // Slow path: not fast UTF-8, or data is very large. We need to fall back
            // to a 2-pass mechanism so that we're not renting absurdly large arrays.

            int actualBytecount = _encoding.GetByteCount(value);
            Write7BitEncodedInt(actualBytecount);
            WriteCharsCommonWithoutLengthPrefix(value, useThisWriteOverride: false);
        }

        public virtual void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() == typeof(RpcBinaryWriter))
            {
                OutStream.Write(buffer);
            }
            else
            {
                byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
                try
                {
                    buffer.CopyTo(array);
                    Write(array, 0, buffer.Length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
        }

        public virtual void Write(ReadOnlySpan<char> chars)
        {
            // When Write(ROS<char>) was first introduced, it dispatched to the this.Write(byte[], ...)
            // virtual method rather than write directly to the output stream. We maintain that same
            // double-indirection for compat purposes.
            WriteCharsCommonWithoutLengthPrefix(chars, useThisWriteOverride: true);
        }

        private void WriteCharsCommonWithoutLengthPrefix(ReadOnlySpan<char> chars, bool useThisWriteOverride)
        {
            // If our input is truly enormous, the call to GetMaxByteCount might overflow,
            // which we want to avoid. Theoretically, any Encoding could expand from chars -> bytes
            // at an enormous ratio and cause us problems anyway given small inputs, but this is so
            // unrealistic that we needn't worry about it.

            byte[] rented;

            if (chars.Length <= MaxArrayPoolRentalSize)
            {
                // GetByteCount may walk the buffer contents, resulting in 2 passes over the data.
                // We prefer GetMaxByteCount because it's a constant-time operation.

                int maxByteCount = _encoding.GetMaxByteCount(chars.Length);
                if (maxByteCount <= MaxArrayPoolRentalSize)
                {
                    rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
                    int actualByteCount = _encoding.GetBytes(chars, rented);
                    WriteToOutStream(rented, 0, actualByteCount, useThisWriteOverride);
                    ArrayPool<byte>.Shared.Return(rented);
                    return;
                }
            }

            // We're dealing with an enormous amount of data, so acquire an Encoder.
            // It should be rare that callers pass sufficiently large inputs to hit
            // this code path, and the cost of the operation is dominated by the transcoding
            // step anyway, so it's ok for us to take the allocation here.

            rented = ArrayPool<byte>.Shared.Rent(MaxArrayPoolRentalSize);
            Encoder encoder = _encoding.GetEncoder();
            bool completed;

            do
            {
                encoder.Convert(chars, rented, flush: true, out int charsConsumed, out int bytesWritten, out completed);
                if (bytesWritten != 0)
                {
                    WriteToOutStream(rented, 0, bytesWritten, useThisWriteOverride);
                }

                chars = chars.Slice(charsConsumed);
            } while (!completed);

            ArrayPool<byte>.Shared.Return(rented);

            void WriteToOutStream(byte[] buffer, int offset, int count, bool useThisWriteOverride)
            {
                if (useThisWriteOverride)
                {
                    Write(buffer, offset, count); // bounce through this.Write(...) overridden logic
                }
                else
                {
                    OutStream.Write(buffer, offset, count); // ignore this.Write(...) override, go straight to inner stream
                }
            }
        }

        public void Write7BitEncodedInt(int value)
        {
            uint uValue = (uint)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                Write((byte)(uValue | ~0x7Fu));
                uValue >>= 7;
            }

            Write((byte)uValue);
        }

        public void Write7BitEncodedInt64(long value)
        {
            ulong uValue = (ulong)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                Write((byte)((uint)uValue | ~0x7Fu));
                uValue >>= 7;
            }

            Write((byte)uValue);
        }
    }
}
