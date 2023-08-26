using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

namespace HigginsSoft.DotMPack.GrpcTests
{

    public class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Guid Guid { get; set; }
        public List<TestChildModel> TestChildModels { get; set; } = new();
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class TestChildModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public byte[] Payload { get; set; }
        public IEnumerable<DateTime> Schedule { get; set; } = Enumerable.Empty<DateTime>();
    }


    public interface ICompiler
    {
        IBinaryReader CreateReader(Type type);
        IBinaryWriter CreateWriter(Type type);
    }

    public interface IBinaryReader
    {
        T Read<T>(Stream stream);
        T Read<T>(BinaryReader stream);
        TestModel Write(BinaryWriter writer);
    }

    public interface IBinaryWriter
    {
        void Write<T>(T? value, Stream stream);
        void Write<T>(T? value, BinaryWriter stream);
        void Write(TestModel value, Stream writer);
        void Write(TestModel value, BinaryWriter writer);
    }

    public interface IBinaryFactory
    {
        T ReadFromBytes<T>(IEnumerable<byte> data);
        T ReadFromStream<T>(Stream stream);
        byte[] WriteToBytes<T>(T value);
        void WriteToStream<T>(T value, Stream stream);
    }

    public interface IWriterFactory
    {
        ConcurrentDictionary<Type, IBinaryWriter> Cache { get; set; }

        IBinaryWriter CreateWriter<T>();
        IBinaryWriter GetWriter<T>();
    }


    public class PrimitiveWriter : IBinaryWriter
    {
        public void Write<T>(T value, Stream stream)
        {

            var writer = new BinaryWriter(stream);
            if (value is bool boolValue)
                writer.Write(boolValue);
            else if (value is sbyte sbyteValue)
                writer.Write(sbyteValue);
            else if (value is byte byteValue)
                writer.Write(byteValue);
            else if (value is byte[] byteArray)
                writer.Write(byteArray);
            else if (value is char charValue)
                writer.Write(charValue);
            else if (value is char[] charArray)
                writer.Write(charArray);
            else if (value is double doubleValue)
                writer.Write(doubleValue);
            else if (value is decimal decimalValue)
                writer.Write(decimalValue);
            else if (value is ushort ushortValue)
                writer.Write(ushortValue);
            else if (value is int intValue)
                writer.Write(intValue);
            else if (value is uint uintValue)
                writer.Write(uintValue);
            else if (value is long longValue)
                writer.Write(longValue);
            else if (value is ulong ulongValue)
                writer.Write(ulongValue);
            else if (value is float floatValue)
                writer.Write(floatValue);
            else if (value is Half halfValue)
                writer.Write(halfValue);
            else if (value is string stringValue)
                writer.Write(stringValue);
            else if (typeof(T).IsGenericType)
            {
                var genericType = value.GetType().GetGenericTypeDefinition();
                var elementType = genericType.GetElementType();
                if (genericType == typeof(Nullable<>))
                {
                    // Handle nullable types...
                }
                else if (genericType == typeof(ReadOnlySpan<>))
                {
                }
            }

        }

        public void Write<T>(T? value, BinaryWriter stream)
        {
            throw new NotImplementedException();
        }

        public void Write(TestModel value, Stream writer)
        {
            throw new NotImplementedException();
        }

        public void Write(TestModel value, BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
    public class BinaryWriterCompiled : IBinaryWriter
    {
        public BinaryWriterCompiled(BinaryFactory binaryFactor)
        {
            BinaryFactory = binaryFactor;
        }

        public BinaryFactory BinaryFactory { get; }

        public void Write<T>(T? value, Stream stream)
        {
            var writer = new BinaryWriter(stream);
            Write(value, writer);
        }
        public void Write(TestModel value, Stream stream)
        {
            var writer = new BinaryWriter(stream);
            Write(value, writer);
        }
        public void Write(TestModel value, BinaryWriter writer)
        {
            BinaryFactory.WriteToWriter(value.Id, writer);
            BinaryFactory.WriteToWriter(value.Name, writer);
            BinaryFactory.WriteToWriter(value.TestChildModels, writer);
            BinaryFactory.WriteToWriter(value.Attributes, writer);
        }

        public void Write<T>(T? value, BinaryWriter writer)
        {
            Write(value, writer);
            foreach (var property in typeof(T).GetProperties())
            {
                var propertyValue = property.GetValue(value);
                BinaryFactory.WriteToWriter(propertyValue, writer);
            }
        }
    }


    public class WriterFactory : IWriterFactory
    {
        public ConcurrentDictionary<Type, IBinaryWriter> Cache { get; set; } = new();
        public WriterFactory(ICompiler compiler)
        {
            Compiler = compiler;
        }

        public IBinaryWriter GetWriter<T>()
        {
            return Cache.GetOrAdd(typeof(T), (type) => CreateWriter<T>());
        }

        public ICompiler Compiler { get; set; }


        public IBinaryWriter CreateWriter<T>()
        {
            return Compiler.CreateWriter(typeof(T));
        }
    }

    public interface IReaderFactory
    {
        ConcurrentDictionary<Type, IBinaryReader> Cache { get; set; }

        IBinaryReader GetReader<T>();
    }

    public class ReaderFactory : IReaderFactory
    {
        public ConcurrentDictionary<Type, IBinaryReader> Cache { get; set; } = new();

        public ICompiler Compiler { get; }
        public ReaderFactory(ICompiler compiler)
        {
            Compiler = compiler;
        }


        public IBinaryReader GetReader<T>()
        {
            return Cache.GetOrAdd(typeof(T), (type) => CreateReader<T>());
        }

        private IBinaryReader CreateReader<T>()
        {
            return Compiler.CreateReader(typeof(T));
        }
    }


    public class Compiler : ICompiler
    {
        public Compiler()
        {
        }

        public IBinaryReader CreateReader(Type type)
        {
            var readerType = CreateReaderType(type);
            var instance = Activator.CreateInstance(readerType) ?? new Exception($"Failed to create IBinaryReader for type of {type.Name}");
            return (IBinaryReader)instance;
        }

        public IBinaryWriter CreateWriter(Type type)
        {
            var WriterType = CreateWriterType(type);
            var instance = Activator.CreateInstance(WriterType) ?? new Exception($"Failed to create IBinaryWriter for type of {type.Name}");
            return (IBinaryWriter)instance;
        }



        public Type CreateReaderType(Type type)
        {
            var assemblyName = new AssemblyName("DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
            var typeName = $"{type.FullName}Reader";

            var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
            typeBuilder.AddInterfaceImplementation(typeof(IBinaryReader));

            var readMethod = typeBuilder.DefineMethod(
                "Read",
                MethodAttributes.Public | MethodAttributes.Virtual,
                type,
                new Type[] { typeof(Stream) }
            );

            var il = readMethod.GetILGenerator();
            il.Emit(OpCodes.Ldnull); // You would need to implement the actual reading logic here
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(
                readMethod,
                typeof(IBinaryReader).GetMethod("Read").MakeGenericMethod(type)
            );

            return typeBuilder.CreateType();
        }

        public Type CreateWriterType(Type type)
        {
            var assemblyName = new AssemblyName("DynamicAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");


            var moduleToken = assemblyBuilder.ManifestModule.MetadataToken;
            var typeName = $"{type.FullName}Writer_{moduleToken}";

            var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);

            typeBuilder.AddInterfaceImplementation(typeof(IBinaryWriter));

            var writeMethod = typeBuilder.DefineMethod(
                "Write",
                MethodAttributes.Public | MethodAttributes.Virtual,
                null,
                new Type[] { type, typeof(Stream) }
            );

            var il = writeMethod.GetILGenerator();

            il.Emit(OpCodes.Ret); // You would need to implement the actual writing logic here

            typeBuilder.DefineMethodOverride(
                writeMethod,
                typeof(IBinaryWriter).GetMethod("Write").MakeGenericMethod(type)
            );

            return typeBuilder.CreateType();
        }


    }



    public class BinaryFactory : IBinaryFactory
    {
        public IWriterFactory WriterFactory { get; }
        public IReaderFactory ReaderFactory { get; }

        public BinaryFactory(IWriterFactory writerFactory, IReaderFactory readerFactory)
        {
            WriterFactory = writerFactory;
            ReaderFactory = readerFactory;
        }
        public void WriteToStream<T>(T value, Stream stream)
        {
            var writer = WriterFactory.GetWriter<T>();
            writer.Write(value, stream);
        }

        public void WriteToWriter<T>(T value, BinaryWriter stream)
        {
            var writer = WriterFactory.GetWriter<T>();
            writer.Write(value, stream);
        }

        public byte[] WriteToBytes<T>(T value)
        {
            using var ms = new MemoryStream();
            WriteToStream(value, ms);
            return ms.ToArray();
        }

        public T ReadFromStream<T>(Stream stream)
        {
            var reader = ReaderFactory.GetReader<T>();
            var result = reader.Read<T>(stream);
            return result;
        }

        public T ReadFromBytes<T>(IEnumerable<byte> data)
        {
            using var ms = new MemoryStream();
            ms.Write(data.ToArray());
            var result = ReadFromStream<T>(ms);
            return result;
        }

        public void init()
        {

        }
    }

}
