DotMPack   
=====
This library is a lightweight Grpc compatible implementation of the [MessagePack](http://msgpack.org/) binary serialization format. MessagePack is a 1-to-1 binary representation of JSON, and the official specification can be found here: [https://github.com/msgpack...](https://github.com/msgpack/msgpack/blob/master/spec.md).

![Build status](https://ci.appveyor.com/api/projects/status/84jv0lllniqsicpb?svg=true) (https://ci.appveyor.com/project/caesay/DotMPack)

Notes
-----
* This library is designed to be super light weight.
* This library is compatible for use as Grpc DataContracts.
* Its easiest to understand how this library works if you think in terms of json. The type `DotMPackMap` represents a dictionary, and the type `DotMPackArray` represents an array. 
* Create DotMPack instances with the static method `DotMPack.From(object);`. You can pass any simple type (such as string, integer, etc), or any Array composed of a simple type. DotMPack also has implicit conversions from most of the basic types built in.
* Transform an DotMPack object back into a CLR type with the static method `DotMPack.To<T>();` or DotMPack.To(type);. DotMPack also has **explicit** converions going back to most basic types, you can do `string str = (string)DotMPack;` for instance.
* DotMPack now supports native asynchrounous reading and cancellation tokens. It will *not* block a thread to wait on a stream.

NuGet
-----
DotMPack is available as a NuGet package!
```
PM> Install-Package DotMPack
```

Usage
-----
Create a object model that can be represented as MsgPack. Here we are creating a dictionary, but really it can be anything:
```csharp
DotMPackMap dictionary = new DotMPackMap
{
    {
        "array1", DotMPack.From(new[]
        {
            "array1_value1",  // implicitly converted string
            DotMPack.From("array1_value2"),
            DotMPack.FromString("array1_value3"),
        })
    },
    {"bool1", DotMPack.From(true)}, //boolean
    {"double1", DotMPack.From(50.5)}, //single-precision float
    {"double2", DotMPack.From(15.2)},
    {"int1", 50505}, // implicitly converted integer
    {"int2", DotMPack.From(50)} // integer
};
```
Serialize the data to a byte array or to a stream to be saved, transmitted, etc:
```csharp
byte[] encodedBytes = dictionary.EncodeToBytes();
// -- or --
dictionary.EncodeToStream(stream);
```
Parse the binary data back into a DotMPack object model (you can also cast back to an DotMPackMap or DotMPackArray after reading if you want dictionary/array methods):
```csharp
var reconstructed = DotMPack.ParseFromBytes(encodedBytes);
// -- or --
var reconstructed = DotMPack.ParseFromStream(stream);
```
Turn DotMPack objects back into types that we understand with the generic `To<>()` method. Since we know the types of everything here we can just call `To<bool>()` to reconstruct our bool, but if you don't know you can access the instance enum `DotMPack.ValueType` to know what kind of value it is:
```csharp
bool bool1 = reconstructed["bool1"].To<bool>();
var array1 = reconstructed["array1"] as DotMPackArray;
var array1_value1 = array1[0];
double double1 = reconstructed["double1"].To<double>();
//etc...
```

Credits
-------
The following people/projects have made this possible:

0. MPack Author: [caelantsayler]at[gmail]dot[com]
0. All of the people that make MessagePack happen: https://github.com/msgpack
