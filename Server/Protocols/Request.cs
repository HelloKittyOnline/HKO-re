using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Server.Protocols;

[AttributeUsage(AttributeTargets.Method)]
class Request : Attribute {
    private readonly int _major;
    private readonly int _minor;

    public delegate void ReceiveFunction(ref Req req, Client client);

    public Request(byte major, byte minor) {
        _major = major;
        _minor = minor;
    }

    public static Dictionary<int, ReceiveFunction> GetEndpoints() {
        var assemlby = Assembly.GetAssembly(typeof(Request));

        var functions = new Dictionary<int, ReceiveFunction>();

        foreach(var method in assemlby.GetTypes().SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))) {
            var att = method.GetCustomAttribute<Request>();
            if(att == null)
                continue;

            var key = (att._major << 8) | att._minor;
            functions[key] = method.CreateDelegate<ReceiveFunction>();
        }

        return functions;
    }
}

struct Req {
    private byte[] buffer;
    private int position;

    public Req(byte[] data) {
        buffer = data;
    }

    public ReadOnlySpan<byte> ReadBytes(int numBytes) {
        var span = new ReadOnlySpan<byte>(buffer, position, numBytes);
        position += numBytes;
        return span;
    }

    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));
    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2));
    public short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(ReadBytes(2));
    public byte ReadByte() => buffer[position++];

    public string ReadWString() {
        return Encoding.Unicode.GetString(ReadBytes(ReadUInt16())).TrimEnd('\0');
    }
    public string ReadString() {
        return PacketBuilder.Window1252.GetString(ReadBytes(ReadByte())).TrimEnd('\0');
    }

    public byte[] DecodeCrazy() {
        var size = ReadUInt16();
        var outSize = ReadUInt16();

        var startPos = position;

        var type = ReadByte();
        if(type == 0x82) {
            return ReadBytes(size - 1).ToArray();
        }

        if(type != 'B')
            throw new Exception("Invalid format");

        var output = new byte[outSize];
        var outPos = 0;

        var byteMask = (ReadByte() << 8) | ReadByte();
        int loopCounter = 0x10;

        while(position - startPos < size) {
            if(loopCounter == 0) {
                byteMask = (ReadByte() << 8) | ReadByte();
                loopCounter = 0x10;
            }
            if((byteMask & 0x8000) == 0) {
                output[outPos++] = ReadByte();
            } else {
                var a = ReadByte();
                var b = ReadByte();

                var offset = (ushort)((a << 4) | (b >> 4));

                if(offset == 0) {
                    int count = (ushort)(((b << 8) | ReadByte()) + 0x10);
                    var val = ReadByte();

                    output.AsSpan(outPos, count).Fill(val);
                    outPos += count;
                } else {
                    int count = (b & 0xF) + 3;

                    int off = outPos - offset;
                    for(int i = 0; i < count; i++) {
                        output[outPos++] = output[off + i];
                    }
                }
            }
            byteMask <<= 1;
            loopCounter--;
        }
        return output;
    }

}
