using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HigginsSoft.DotMPack
{
    internal class Reader
    {
        private static readonly FamilyReaderLookup _lookup = new FamilyReaderLookup();
        private static Encoding _encoding = Encoding.UTF8;
        private static IBitConverter _convert = EndianBitConverter.Big;

        static Reader()
        {
            //positive fixint	0xxxxxxx	0x00 - 0x7f
            _lookup.AddRange(0, 0x7F,
                (b, s) => new DotMPack(b, DotMPackType.SInt),
                (b, s, c) => Task.FromResult(new DotMPack(b, DotMPackType.SInt)));

            //positive fixint	0xxxxxxx	0x00 - 0x7f
            _lookup.AddRange(0x80, 0x8F, (b, s) =>
            {
                DotMPackMap map = new DotMPackMap();
                int len = b - 0x80;
                for (int i = 0; i < len; i++)
                {
                    map.Add(ParseFromStream(s), ParseFromStream(s));
                }
                return map;
            }, async (b, s, c) =>
            {
                DotMPackMap map = new DotMPackMap();
                int len = b - 0x80;
                for (int i = 0; i < len; i++)
                {
                    map.Add(await ParseFromStreamAsync(s, c), await ParseFromStreamAsync(s, c));
                }
                return map;
            });

            //fixarray	1001xxxx	0x90 - 0x9f
            _lookup.AddRange(0x90, 0x9f, (b, s) =>
            {
                DotMPackArray array = new DotMPackArray();
                int len = b - 0x90;
                for (int i = 0; i < len; i++)
                {
                    array.Add(ParseFromStream(s));
                }
                return array;
            }, async (b, s, c) =>
            {
                DotMPackArray array = new DotMPackArray();
                int len = b - 0x90;
                for (int i = 0; i < len; i++)
                {
                    array.Add(await ParseFromStreamAsync(s, c));
                }
                return array;
            });

            // fixstr	101xxxxx	0xa0 - 0xbf
            _lookup.AddRange(0xA0, 0xBF, (b, s) =>
            {
                int len = b - 0xA0;
                return new DotMPack(_encoding.GetString(s.Read(len)), DotMPackType.String);
            }, async (b, s, c) =>
            {
                int len = b - 0xA0;
                return new DotMPack(_encoding.GetString(await s.ReadAsync(len, c)), DotMPackType.String);
            });

            // negative fixnum stores 5-bit negative integer 111xxxxx
            _lookup.AddRange(0xE0, 0xFF,
                (b, s) => new DotMPack((sbyte)b, DotMPackType.SInt),
                (b, s, c) => Task.FromResult(new DotMPack((sbyte)b, DotMPackType.SInt)));

            // null
            _lookup.Add(0xC0,
                (b, s) => DotMPack.Null(),
                (b, s, c) => Task.FromResult(DotMPack.Null()));

            // note, no 0xC1, it's not used.

            // bool: false
            _lookup.Add(0xC2,
                (b, s) => new DotMPack(false, DotMPackType.Bool),
                (b, s, c) => Task.FromResult(new DotMPack(false, DotMPackType.Bool)));

            // bool: true
            _lookup.Add(0xC3,
                (b, s) => new DotMPack(true, DotMPackType.Bool),
                (b, s, c) => Task.FromResult(new DotMPack(true, DotMPackType.Bool)));

            // binary array, max 255
            _lookup.Add(0xC4, (b, s) =>
            {
                byte len = (byte)s.ReadByte();
                return new DotMPack(s.Read(len), DotMPackType.Binary);
            }, async (b, s, c) =>
            {
                byte len = await s.ReadByteAsync(c);
                return new DotMPack(await s.ReadAsync(len, c), DotMPackType.Binary);
            });

            // binary array, max 65535
            _lookup.Add(0xC5, (b, s) =>
            {
                int len = _convert.ToUInt16(s.Read(2), 0);
                return new DotMPack(s.Read(len), DotMPackType.Binary);
            }, async (b, s, c) =>
            {
                int len = _convert.ToUInt16((await s.ReadAsync(2, c)), 0);
                return new DotMPack(await s.ReadAsync(len, c), DotMPackType.Binary);
            });

            // binary max: 2^32-1                
            _lookup.Add(0xC6, (b, s) =>
            {
                uint len = _convert.ToUInt32(s.Read(4), 0);
                return new DotMPack(s.Read(len), DotMPackType.Binary);
            }, async (b, s, c) =>
            {
                uint len = _convert.ToUInt32((await s.ReadAsync(4, c)), 0);
                return new DotMPack(await s.ReadAsync(len, c), DotMPackType.Binary);
            });

            // note 0xC7, 0xC8, 0xC9, not used.

            // float 32 stores a floating point number in IEEE 754 single precision floating point number     
            _lookup.Add(0xCA, (b, s) =>
            {
                var raw = s.Read(4);
                return new DotMPack(_convert.ToSingle(raw, 0), DotMPackType.Single);
            }, async (b, s, c) =>
            {
                var raw = (await s.ReadAsync(4, c));
                return new DotMPack(_convert.ToSingle(raw, 0), DotMPackType.Single);
            });

            // float 64 stores a floating point number in IEEE 754 double precision floating point number        
            _lookup.Add(0xCB, (b, s) =>
            {
                var raw = s.Read(8);
                return new DotMPack(_convert.ToDouble(raw, 0), DotMPackType.Double);
            }, async (b, s, c) =>
            {
                var raw = (await s.ReadAsync(8, c));
                return new DotMPack(_convert.ToDouble(raw, 0), DotMPackType.Double);
            });

            // uint8   0xcc   xxxxxxxx
            _lookup.Add(0xCC,
                (b, s) => new DotMPack((byte)s.ReadByte(), DotMPackType.UInt),
                async (b, s, c) => new DotMPack((byte)await s.ReadByteAsync(c), DotMPackType.UInt));

            // uint16   0xcd xxxxxxxx xxxxxxxx   
            _lookup.Add(0xCD, (b, s) =>
            {
                var v = _convert.ToUInt16(s.Read(2), 0);
                return new DotMPack(v, DotMPackType.UInt);
            }, async (b, s, c) =>
            {
                var v = _convert.ToUInt16((await s.ReadAsync(2, c))
                    , 0);
                return new DotMPack(v, DotMPackType.UInt);
            });

            // uint32   0xce xxxxxxxx xxxxxxxx xxxxxxxx xxxxxxxx  
            _lookup.Add(0xCE, (b, s) =>
            {
                var v = _convert.ToUInt32(s.Read(4), 0);
                return new DotMPack(v, DotMPackType.UInt);
            }, async (b, s, c) =>
            {
                var v = _convert.ToUInt32((await s.ReadAsync(4, c))
                    , 0);
                return new DotMPack(v, DotMPackType.UInt);
            });

            // uint64   0xcF   xxxxxxxx (x4)
            _lookup.Add(0xCF, (b, s) =>
            {
                var v = _convert.ToUInt64(s.Read(8), 0);
                return new DotMPack(v, DotMPackType.UInt);
            }, async (b, s, c) =>
            {
                var v = _convert.ToUInt64((await s.ReadAsync(8, c)), 0);
                return new DotMPack(v, DotMPackType.UInt);
            });

            // array (uint16 length)
            _lookup.Add(0xDC, (b, s) =>
            {
                ushort len = _convert.ToUInt16(s.Read(2), 0);
                DotMPackArray array = new DotMPackArray();
                for (ushort i = 0; i < len; i++)
                {
                    array.Add(ParseFromStream(s));
                }
                return array;
            }, async (b, s, c) =>
            {
                ushort len = _convert.ToUInt16((await s.ReadAsync(2, c)), 0);
                DotMPackArray array = new DotMPackArray();
                for (ushort i = 0; i < len; i++)
                {
                    array.Add(await ParseFromStreamAsync(s, c));
                }
                return array;
            });

            // array (uint32 length)
            _lookup.Add(0xDD, (b, s) =>
            {
                uint len = _convert.ToUInt32(s.Read(4), 0);
                DotMPackArray array = new DotMPackArray();
                for (uint i = 0; i < len; i++)
                {
                    array.Add(ParseFromStream(s));
                }
                return array;
            }, async (b, s, c) =>
            {
                uint len = _convert.ToUInt32(await s.ReadAsync(4, c), 0);
                DotMPackArray array = new DotMPackArray();
                for (uint i = 0; i < len; i++)
                {
                    array.Add(await ParseFromStreamAsync(s, c));
                }
                return array;
            });

            // map (uint16 length)
            _lookup.Add(0xDE, (b, s) =>
            {
                ushort len = _convert.ToUInt16(s.Read(2), 0);
                DotMPackMap map = new DotMPackMap();
                for (ushort i = 0; i < len; i++)
                {
                    var flag = (byte)s.ReadByte();
                    var key = ReadString(flag, s);
                    var val = ParseFromStream(s);
                    map.Add(key, val);
                }
                return map;
            }, async (b, s, c) =>
            {
                ushort len = _convert.ToUInt16(await s.ReadAsync(2, c), 0);
                DotMPackMap map = new DotMPackMap();
                for (ushort i = 0; i < len; i++)
                {
                    var flag = await s.ReadByteAsync(c);
                    var key = await ReadStringAsync(flag, s, c);
                    var val = await ParseFromStreamAsync(s, c);
                    map.Add(key, val);
                }
                return map;
            });

            // map (uint32 length)
            _lookup.Add(0xDF, (b, s) =>
            {
                uint len = _convert.ToUInt32(s.Read(4), 0);
                DotMPackMap map = new DotMPackMap();
                for (uint i = 0; i < len; i++)
                {
                    var flag = (byte)s.ReadByte();
                    var key = ReadString(flag, s);
                    var val = ParseFromStream(s);
                    map.Add(key, val);
                }
                return map;
            }, async (b, s, c) =>
            {
                uint len = _convert.ToUInt32((await s.ReadAsync(4, c)), 0);
                DotMPackMap map = new DotMPackMap();
                for (uint i = 0; i < len; i++)
                {
                    var flag = await s.ReadByteAsync(c);
                    var key = await ReadStringAsync(flag, s, c);
                    var val = await ParseFromStreamAsync(s, c);
                    map.Add(key, val);
                }
                return map;
            });

            //  str family
            _lookup.AddRange(0xD9, 0xDB,
                (b, s) => new DotMPack(ReadString(b, s), DotMPackType.String), 
                async (b, s, c) => new DotMPack((await ReadStringAsync(b, s, c)), DotMPackType.String));

            // int8   0xD0   xxxxxxxx
            _lookup.Add(0xD0,
                (b, s) => new DotMPack((sbyte)s.ReadByte(), DotMPackType.SInt),
                async (b, s, c) => new DotMPack((sbyte)await s.ReadByteAsync(c), DotMPackType.SInt));

            // int16   0xd1 xxxxxxxx xxxxxxxx   
            _lookup.Add(0xD1, (b, s) =>
            {
                var v = _convert.ToInt16(s.Read(2), 0);
                return new DotMPack(v, DotMPackType.SInt);
            }, async (b, s, c) =>
            {
                var v = _convert.ToInt16((await s.ReadAsync(2, c))
                    , 0);
                return new DotMPack(v, DotMPackType.SInt);
            });

            // int32    0xD2 xxxxxxxx xxxxxxxx xxxxxxxx xxxxxxxx  
            _lookup.Add(0xD2, (b, s) =>
            {
                var v = _convert.ToInt32(s.Read(4), 0);
                return new DotMPack(v, DotMPackType.SInt);
            }, async (b, s, c) =>
            {
                var v = _convert.ToInt32((await s.ReadAsync(4, c))
                    , 0);
                return new DotMPack(v, DotMPackType.SInt);
            });

            // int64      0xD3 xxxxxxxx (x8)
            _lookup.Add(0xD3, (b, s) =>
            {
                var v = _convert.ToInt64(s.Read(8), 0);
                return new DotMPack(v, DotMPackType.SInt);
            }, async (b, s, c) =>
            {
                var v = _convert.ToInt64((await s.ReadAsync(8, c))
                    , 0);
                return new DotMPack(v, DotMPackType.SInt);
            });
        }

        public static DotMPack ParseFromStream(Stream stream)
        {
            byte selector = (byte)stream.ReadByte();
            return _lookup[selector].Read(selector, stream);
        }
        public static async Task<DotMPack> ParseFromStreamAsync(Stream stream, CancellationToken token)
        {
            byte selector = await stream.ReadByteAsync(token);
            return await _lookup[selector].ReadAsync(selector, stream, token);
        }

        private static string ReadString(byte flag, Stream ms)
        {
            //fixstr stores a byte array whose length is upto 31 bytes:
            //+--------+========+
            //|101XXXXX|  data  |
            //+--------+========+
            //
            //str 8 stores a byte array whose length is upto (2^8)-1 bytes:
            //+--------+--------+========+
            //|  0xd9  |YYYYYYYY|  data  |
            //+--------+--------+========+
            //
            //str 16 stores a byte array whose length is upto (2^16)-1 bytes:
            //+--------+--------+--------+========+
            //|  0xda  |ZZZZZZZZ|ZZZZZZZZ|  data  |
            //+--------+--------+--------+========+
            //
            //str 32 stores a byte array whose length is upto (2^32)-1 bytes:
            //+--------+--------+--------+--------+--------+========+
            //|  0xdb  |AAAAAAAA|AAAAAAAA|AAAAAAAA|AAAAAAAA|  data  |
            //+--------+--------+--------+--------+--------+========+
            //
            //where
            //* XXXXX is a 5-bit unsigned integer which represents N
            //* YYYYYYYY is a 8-bit unsigned integer which represents N
            //* ZZZZZZZZ_ZZZZZZZZ is a 16-bit big-endian unsigned integer which represents N
            //* AAAAAAAA_AAAAAAAA_AAAAAAAA_AAAAAAAA is a 32-bit big-endian unsigned integer which represents N
            //* N is the length of data   

            uint len = 0;
            if ((flag >= 0xA0) && (flag <= 0xBF))
                len = (byte)(flag - 0xA0);
            else if (flag == 0xD9)
                len = (byte)ms.ReadByte();
            else if (flag == 0xDA)
                len = _convert.ToUInt16(ms.Read(2), 0);
            else if (flag == 0xDB)
                len = _convert.ToUInt32(ms.Read(4), 0);

            return _encoding.GetString(ms.Read(len));
        }
        private static async Task<string> ReadStringAsync(byte flag, Stream ms, CancellationToken token)
        {
            //fixstr stores a byte array whose length is upto 31 bytes:
            //+--------+========+
            //|101XXXXX|  data  |
            //+--------+========+
            //
            //str 8 stores a byte array whose length is upto (2^8)-1 bytes:
            //+--------+--------+========+
            //|  0xd9  |YYYYYYYY|  data  |
            //+--------+--------+========+
            //
            //str 16 stores a byte array whose length is upto (2^16)-1 bytes:
            //+--------+--------+--------+========+
            //|  0xda  |ZZZZZZZZ|ZZZZZZZZ|  data  |
            //+--------+--------+--------+========+
            //
            //str 32 stores a byte array whose length is upto (2^32)-1 bytes:
            //+--------+--------+--------+--------+--------+========+
            //|  0xdb  |AAAAAAAA|AAAAAAAA|AAAAAAAA|AAAAAAAA|  data  |
            //+--------+--------+--------+--------+--------+========+
            //
            //where
            //* XXXXX is a 5-bit unsigned integer which represents N
            //* YYYYYYYY is a 8-bit unsigned integer which represents N
            //* ZZZZZZZZ_ZZZZZZZZ is a 16-bit big-endian unsigned integer which represents N
            //* AAAAAAAA_AAAAAAAA_AAAAAAAA_AAAAAAAA is a 32-bit big-endian unsigned integer which represents N
            //* N is the length of data   

            uint len = 0;
            if ((flag >= 0xA0) && (flag <= 0xBF))
                len = (byte)(flag - 0xA0);
            else if (flag == 0xD9)
                len = (byte)ms.ReadByte();
            else if (flag == 0xDA)
                len = _convert.ToUInt16(await ms.ReadAsync(2, token), 0);
            else if (flag == 0xDB)
                len = _convert.ToUInt32(await ms.ReadAsync(4, token), 0);

            return Encoding.UTF8.GetString(await ms.ReadAsync(len, token));
        }
    }
    internal class FamilyReader
    {
        private readonly Func<byte, Stream, DotMPack> _sync;
        private readonly Func<byte, Stream, CancellationToken, Task<DotMPack>> _async;

        public FamilyReader(Func<byte, Stream, DotMPack> sync, Func<byte, Stream, CancellationToken, Task<DotMPack>> async)
        {
            _sync = sync;
            _async = async;
        }

        public DotMPack Read(byte selector, Stream stream)
        {
            return _sync(selector, stream);
        }
        public Task<DotMPack> ReadAsync(byte selector, Stream stream, CancellationToken token)
        {
            return _async(selector, stream, token);
        }
    }

    internal class FamilyReaderLookup : Dictionary<byte, FamilyReader>
    {
        public void AddRange(byte rangeStart, byte rangeEnd, FamilyReader reader)
        {
            var test = Enumerable.Range(rangeStart, rangeEnd - rangeStart + 1).ToArray();
            foreach (var b in test.Select(i => (byte)i))
            {
                this.Add(b, reader);
            }
        }
        public void AddRange(byte rangeStart, byte rangeEnd, Func<byte, Stream, DotMPack> sync,
            Func<byte, Stream, CancellationToken, Task<DotMPack>> async)
        {
            AddRange(rangeStart, rangeEnd, new FamilyReader(sync, async));
        }
        public void Add(byte flag, Func<byte, Stream, DotMPack> sync,
            Func<byte, Stream, CancellationToken, Task<DotMPack>> async)
        {
            Add(flag, new FamilyReader(sync, async));
        }
    }
}
