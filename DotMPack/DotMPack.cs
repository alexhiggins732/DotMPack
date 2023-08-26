using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HigginsSoft.DotMPack
{
    [DataContract]
    public class DotMPack : IEquatable<DotMPack>, IConvertible
    {
        [DataMember(Order = 1)]
        public List<byte> Payload
        {
            get
            {
                var result = this.EncodeToBytes().ToList();
                var pack = DotMPack.ParseFromBytes(result.ToArray());
                return result;
            }
            set
            {
                var pack = DotMPack.ParseFromBytes(value.ToArray());
                this._value = pack.Value;
                this._type = pack.ValueType;
            }
        }

        public virtual object Value { get { return _value; } }
        public virtual DotMPackType ValueType { get { return _type; } }

        internal DotMPack(object value, DotMPackType type)
        {
            _value = value;
            _type = type;
        }
        public DotMPack()
        {
        }

        private object _value = null;
        private DotMPackType _type = DotMPackType.Null;

        public virtual DotMPack this[int index]
        {
            get
            {
                if (this is DotMPackMap)
                    return this[(DotMPack)index];
                throw new NotSupportedException("Array indexor not supported in this context.");
            }
            set
            {
                if (this is DotMPackMap)
                    this[(DotMPack)index] = value;
                else
                    throw new NotSupportedException("Array indexor not supported in this context.");
            }
        }
        public virtual DotMPack this[DotMPack key]
        {
            get
            {
                throw new NotSupportedException("Map indexor not supported in this context.");
            }
            set
            {
                throw new NotSupportedException("Map indexor not supported in this context.");
            }
        }

        public static DotMPack Null()
        {
            return new DotMPack() { _type = DotMPackType.Null };
        }

        public static DotMPack From(object value)
        {
            if (value is null)
                return DotMPack.Null();
            var result = From(value, value.GetType());
            return result;
        }

        public static DotMPack From(object value, Type type)
        {
            if (value == null)
                return new DotMPack(null, DotMPackType.Null);

            if (!type.IsInstanceOfType(value))
                throw new ArgumentException("Type does not match provided object.");
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType == typeof(byte))
                    return new DotMPack(value, DotMPackType.Binary);
                if (elementType == typeof(DotMPack))
                    return new DotMPackArray((DotMPack[])value);

                var elementTypeCode = (int)Type.GetTypeCode(elementType);
                if (elementTypeCode <= 2 || elementTypeCode == 16)
                    throw new NotSupportedException(String.Format("The specified array type ({0}) is not supported by MsgPack", elementType.Name));

                DotMPackArray resultArray = new DotMPackArray();
                Array inputArray = (Array)value;
                foreach (var obj in inputArray)
                {
                    resultArray.Add(From(obj));
                }
                return resultArray;
            }

            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                    return new DotMPack(value, DotMPackType.Bool);
                case TypeCode.Char:
                    return new DotMPack(value, DotMPackType.UInt);
                case TypeCode.SByte:
                    return new DotMPack(value, DotMPackType.SInt);
                case TypeCode.Byte:
                    return new DotMPack(value, DotMPackType.UInt);
                case TypeCode.Int16:
                    return new DotMPack(value, DotMPackType.SInt);
                case TypeCode.UInt16:
                    return new DotMPack(value, DotMPackType.UInt);
                case TypeCode.Int32:
                    return new DotMPack(value, DotMPackType.SInt);
                case TypeCode.UInt32:
                    return new DotMPack(value, DotMPackType.UInt);
                case TypeCode.Int64:
                    return new DotMPack(value, DotMPackType.SInt);
                case TypeCode.UInt64:
                    return new DotMPack(value, DotMPackType.UInt);
                case TypeCode.Single:
                    return new DotMPack(value, DotMPackType.Single);
                case TypeCode.Double:
                    return new DotMPack(value, DotMPackType.Double);
                case TypeCode.Decimal:
                    return new DotMPack((double)(decimal)value, DotMPackType.Double);
                case TypeCode.DateTime:
                    return new DotMPack(((DateTime)value).ToFileTimeUtc(), DotMPackType.Date);
                case TypeCode.String:
                    return new DotMPack(value, DotMPackType.String);
            }
            throw new NotSupportedException("Tried to create DotMPack object from unsupported type: " + type.Name);
        }

        public object To(Type t)
        {
            if (ValueType == DotMPackType.Null)
                return null;
            if (t == typeof(object))
                return Value;

            // handle basic array types, ex. string[], int[], etc.
            // will fail if one of the child objects is of the incorrect type.
            if (t.IsArray)
            {
                var thisType = this.GetType();
                var elementType = t.GetElementType();

                if (elementType == typeof(byte))
                {
                    //var val =   ((IEnumerable<byte>)Value);
                    //return val.ToArray();
                }


                if (elementType == typeof(object))
                    throw new ArgumentException("Array element type must not equal typeof(object).", nameof(t));

                int elementTypeCode = (int)Type.GetTypeCode(elementType);
                if (elementType != typeof(DotMPack) && elementTypeCode <= 2)
                    throw new NotSupportedException(String.Format("Casting to an array of type {0} is not supported.",
                        elementType.Name));

                if (Value is IEnumerable<DotMPack> enumerable)
                {
                    if (t.Name == "DotMPack[]")
                        return enumerable.ToArray();
                }


                var DotMPackArray = Value as DotMPackArray;
                if (DotMPackArray == null)
                    DotMPackArray = this as DotMPackArray;

                if (DotMPackArray == null)
                {
                    if (elementType != typeof(DotMPack))
                        throw new ArgumentException(String.Format("Cannot conver DotMPack type {0} into type {1} (it is not an array).",
                            ValueType, t.Name));

                    DotMPackArray val = new();
                    var mArray = (DotMPack[])Value;
                    foreach (var el in mArray)
                        val.Add(el);
                    return mArray;
                }

                if (elementType == typeof(DotMPack))
                    return DotMPackArray.ToArray();

                var count = DotMPackArray.Count;
                var objArray = Array.CreateInstance(elementType, count);
                for (int i = 0; i < count; i++)
                    objArray.SetValue(DotMPackArray[i].To(elementType), i);
                return objArray;
            }

            if (ValueType == DotMPackType.Date || t == typeof(DateTime))
            {
                var ticks = (ulong)Value;
                var result = DateTime.FromFileTime((long)ticks);
                return result;
            }
            return Convert.ChangeType(Value, t);
        }
        public T To<T>()
        {
            var res = To(typeof(T));
            return (T)res;
        }
        public T ToOrDefault<T>()
        {
            try
            {
                return To<T>();
            }
            catch
            {
                return default(T);
            }
        }

        public static bool operator ==(DotMPack m1, DotMPack m2)
        {
            if (ReferenceEquals(m1, m2)) return true;
            if (!ReferenceEquals(m1, null))
                return m1.Equals(m2);
            return false;
        }
        public static bool operator !=(DotMPack m1, DotMPack m2)
        {
            if (ReferenceEquals(m1, m2)) return false;
            if (!ReferenceEquals(m1, null))
                return !m1.Equals(m2);
            return true;
        }

        public static implicit operator DotMPack(bool value) { return From(value); }
        public static implicit operator DotMPack(float value) { return From(value); }
        public static implicit operator DotMPack(double value) { return From(value); }
        public static implicit operator DotMPack(byte value) { return From(value); }
        public static implicit operator DotMPack(ushort value) { return From(value); }
        public static implicit operator DotMPack(uint value) { return From(value); }
        public static implicit operator DotMPack(ulong value) { return From(value); }
        public static implicit operator DotMPack(sbyte value) { return From(value); }
        public static implicit operator DotMPack(short value) { return From(value); }
        public static implicit operator DotMPack(int value) { return From(value); }
        public static implicit operator DotMPack(string value) { return From(value); }
        public static implicit operator DotMPack(byte[] value) { return From(value); }
        public static implicit operator DotMPack(DotMPack[] value) { return From(value); }

        public static implicit operator bool(DotMPack value) { return value.To<bool>(); }
        public static implicit operator float(DotMPack value) { return value.To<float>(); }
        public static implicit operator double(DotMPack value) { return value.To<double>(); }
        public static implicit operator byte(DotMPack value) { return value.To<byte>(); }
        public static implicit operator ushort(DotMPack value) { return value.To<ushort>(); }
        public static implicit operator uint(DotMPack value) { return value.To<uint>(); }
        public static implicit operator ulong(DotMPack value) { return value.To<ulong>(); }
        public static implicit operator sbyte(DotMPack value) { return value.To<sbyte>(); }
        public static implicit operator short(DotMPack value) { return value.To<short>(); }
        public static implicit operator int(DotMPack value) { return value.To<int>(); }
        public static implicit operator string(DotMPack value) { return value.To<string>(); }
        public static implicit operator byte[](DotMPack value) { return value.To<byte[]>(); }
        public static implicit operator DotMPack[](DotMPack value) { return value.To<DotMPack[]>(); }

        public static DotMPack ParseFromBytes(byte[] array)
        {
            using (MemoryStream ms = new MemoryStream(array))
                return ParseFromStream(ms);
        }
        public static DotMPack ParseFromStream(Stream stream)
        {
            return Reader.ParseFromStream(stream);
        }
        public static Task<DotMPack> ParseFromStreamAsync(Stream stream)
        {
            return ParseFromStreamAsync(stream, CancellationToken.None);
        }
        public static Task<DotMPack> ParseFromStreamAsync(Stream stream, CancellationToken token)
        {
            return Reader.ParseFromStreamAsync(stream, token);
        }
        public void EncodeToStream(Stream stream)
        {
            Writer.EncodeToStream(stream, this);

        }
        public Task EncodeToStreamAsync(Stream stream)
        {
            return EncodeToStreamAsync(stream, CancellationToken.None);
        }
        public async Task EncodeToStreamAsync(Stream stream, CancellationToken token)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Writer.EncodeToStream(ms, this);

                ms.Position = 0;
                await ms.CopyToAsync(stream, 65535, token);
            }
        }
        public byte[] EncodeToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Writer.EncodeToStream(ms, this);
                return ms.ToArray();
            }
        }

        public bool Equals(DotMPack other)
        {
            if (ReferenceEquals(other, null))
                return false;
            else if (Value is null)
            {
                return other.Value is null;
            }
            else if (other.Value is null)
            {
                return false;
            }
            else if (this is DotMPackArray && other is DotMPackArray)
            {
                var ob1 = (DotMPackArray)this;
                var ob2 = (DotMPackArray)other;
                if (ob1.Count == ob2.Count)
                {
                    return ob1.SequenceEqual(ob2);
                }
            }
            else if (this is DotMPackMap && other is DotMPackMap)
            {
                var ob1 = (DotMPackMap)this;
                var ob2 = (DotMPackMap)other;
                if (ob1.Count == ob2.Count)
                {
                    return ob1.OrderBy(r => r.Key).SequenceEqual(ob2.OrderBy(r => r.Key));
                }
            }
            else if ((this.ValueType == DotMPackType.SInt || this.ValueType == DotMPackType.UInt) &&
                     (other.ValueType == DotMPackType.SInt || other.ValueType == DotMPackType.UInt))
            {
                decimal xd = Convert.ToDecimal(Value);
                decimal yd = Convert.ToDecimal(other.Value);
                return xd == yd;
            }
            else return Value.Equals(other.Value);

            return false;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is DotMPack)
                return Equals((DotMPack)obj);
            return false;
        }
        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }
        public override string ToString()
        {
            if (Value == null)
                return "null";
            return Value.ToString();
        }

        TypeCode IConvertible.GetTypeCode()
        {
            if (ValueType == DotMPackType.Null)
                return TypeCode.Object;
            return Type.GetTypeCode(Value.GetType());
        }
        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return To<bool>();
        }
        char IConvertible.ToChar(IFormatProvider provider)
        {
            return To<char>();
        }
        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return To<sbyte>();
        }
        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return To<byte>();
        }
        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return To<short>();
        }
        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return To<ushort>();
        }
        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return To<int>();
        }
        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return To<uint>();
        }
        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return To<long>();
        }
        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return To<ulong>();
        }
        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return To<float>();
        }
        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return To<double>();
        }
        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return To<decimal>();
        }
        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return To<DateTime>();
        }
        string IConvertible.ToString(IFormatProvider provider)
        {
            return To<string>();
        }
        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return To(conversionType);
        }
    }
}
