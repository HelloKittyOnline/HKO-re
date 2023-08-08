﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Server;

interface IWriteAble {
    public void Write(PacketBuilder b);
}

class PacketBuilder {
    public static readonly Encoding Window1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252);

    private MemoryStream buffer;
    private BinaryWriter writer;

    private bool CompressMode = false;
    private int CompressPos = 0;

    public PacketBuilder() {
        buffer = new MemoryStream(); // could use Microsoft.IO.RecyclableMemoryStream for better performance
        writer = new BinaryWriter(buffer);

        WriteByte((byte)'^');
        WriteByte((byte)'%');
        WriteByte((byte)'*');
        WriteShort(0);
    }

    public PacketBuilder(byte major, byte minor) : this() {
        WriteByte(major); // first switch
        WriteByte(minor); // second switch
    }

    public void Write(byte[] buffer) {
        writer.Write(buffer);
    }

    public void Write<T>(T item) where T : IWriteAble {
        item.Write(this);
    }

    public void WriteByte(byte v) {
        writer.Write(v);
    }
    public void WriteByte(bool v) {
        writer.Write(v);
    }

    public void WriteShort(short v) {
        writer.Write(v);
    }
    public void WriteUShort(ushort v) {
        writer.Write(v);
    }

    public void WriteInt(int v) {
        writer.Write(v);
    }
    public void Write0(int bytes) {
        Span<byte> buffer = stackalloc byte[Math.Min(bytes, 1024)];

        while(bytes > 0) {
            var len = Math.Min(bytes, 1024);
            writer.Write(buffer[..len]);
            bytes -= len;
        }
    }

    public void WriteString(string str, int pre) {
        switch(pre) {
            case 1:
                if(str.Length > 255) {
                    throw new ArgumentOutOfRangeException(nameof(str), "string too long");
                }
                WriteByte((byte)str.Length);
                break;
            case 2:
                if(str.Length > 65535) {
                    throw new ArgumentOutOfRangeException(nameof(str), "string too long");
                }
                WriteShort((short)str.Length);
                break;
            case 4:
                if(str.Length > 65535) {
                    throw new ArgumentOutOfRangeException(nameof(str), "string too long");
                }
                WriteInt(str.Length);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pre), "invalid pre size");
        }

        writer.Write(Window1252.GetBytes(str));
    }

    // writes length prefixed and padded Window1252 string
    public void WritePadString(string str, int length) {
        var bytes = Window1252.GetBytes(str);

        WriteByte((byte)bytes.Length);
        Write(bytes);
        Write0(length - bytes.Length - 1);
    }

    // writes length prefixed utf-16 string
    public void WriteWString(string str) {
        var dat = Encoding.Unicode.GetBytes(str);

        if(dat.Length > 65535) {
            throw new ArgumentOutOfRangeException("str", "string too long");
        }
        WriteShort((short)dat.Length);
        writer.Write(dat);
    }

    // writes utf-16 string and pads to length
    public void WritePadWString(string str, int bytes) {
        var raw = Encoding.Unicode.GetBytes(str);

        if(raw.Length + 2 > bytes) { // keep space for null terminator
            throw new ArgumentOutOfRangeException(nameof(str), "string too long");
        }
        writer.Write(raw);
        Write0(bytes - raw.Length);
    }

    public void Send(Client client) {
        Debug.Assert(!CompressMode);
        var buf = buffer.GetBuffer();

        // update data length
        var dataLength = buffer.Position - 5;
        buf[3] = (byte)(dataLength & 0xFF);
        buf[4] = (byte)(dataLength >> 8);

#if DEBUG
        if(dataLength >= 2 && !(buf[5] == 0x00 && buf[6] == 0x63))
            client.Logger.LogTrace("[{userID}] S -> C: {:X2}_{:X2}", client.DiscordId, buf[5], buf[6]);
#endif
        lock(client.Stream) {
            client.Stream.Write(buf, 0, (int)buffer.Position);
        }
    }

    public void Send(IEnumerable<Client> clients) {
        Debug.Assert(!CompressMode);
        var buf = buffer.GetBuffer();

        // update data length
        var dataLength = buffer.Position - 5;
        buf[3] = (byte)(dataLength & 0xFF);
        buf[4] = (byte)(dataLength >> 8);

        foreach(var client in clients) {
#if DEBUG
            if(dataLength >= 2 && !(buf[5] == 0x00 && buf[6] == 0x63))
                client.Logger.LogTrace("[{userID}] S -> C: {:X2}_{:X2}", client.DiscordId, buf[5], buf[6]);
#endif

            lock(client.Stream) {
                client.Stream.Write(buf, 0, (int)buffer.Position);
            }
        }
    }

    public void Send(Span<Client> clients) {
        Debug.Assert(!CompressMode);
        var buf = buffer.GetBuffer();

        // update data length
        var dataLength = buffer.Position - 5;
        buf[3] = (byte)(dataLength & 0xFF);
        buf[4] = (byte)(dataLength >> 8);

        foreach(var client in clients) {
#if DEBUG
            if(dataLength >= 2 && !(buf[5] == 0x00 && buf[6] == 0x63))
                client.Logger.LogTrace("[{userID}] S -> C: {:X2}_{:X2}", client.DiscordId, buf[5], buf[6]);
#endif

            lock(client.Stream) {
                client.Stream.Write(buf, 0, (int)buffer.Position);
            }
        }
    }

    public void BeginCompress() {
        Debug.Assert(!CompressMode, "Already in compression mode");
        CompressMode = true;
        CompressPos = (int)buffer.Position;

        WriteShort(0); // placeholder for length
        WriteShort(0);
        WriteByte(0x82); // don't bother encoding just use raw
    }

    public long CompressSize => buffer.Position - CompressPos - 5;

    public void EndCompress() {
        Debug.Assert(CompressMode, "Have to be in compression mode");
        CompressMode = false;

        var pos = buffer.Position;
        var len = pos - CompressPos - 5;

        Debug.Assert(len <= ushort.MaxValue);

        if(len == 0) {
            writer.Seek(-1, SeekOrigin.Current);
        } else {
            writer.Seek(CompressPos, SeekOrigin.Begin);
            writer.Write((short)(len + 1));
            writer.Write((short)len);
            writer.Seek((int)pos, SeekOrigin.Begin);
        }
    }

    public void EncodeCrazy(byte[] data) {
        WriteShort((short)(data.Length + 1));
        WriteShort((short)data.Length);

        if(data.Length == 0)
            return;

        // don't bother encoding just use raw
        WriteByte(0x82);

        writer.Write(data);
    }

    public static byte[] DecodeCrazy(BinaryReader req) {
        var size = req.ReadUInt16();
        var outSize = req.ReadUInt16();

        int read = 0;

        var type = req.ReadByte();
        read += 1;

        if(type == 0x82) {
            return req.ReadBytes(size - 1);
        }
        if(type == 'B') {
            // TODO: replace with array?
            var output = new List<byte>(outSize);

            var byteMask = (req.ReadByte() << 8) | req.ReadByte();
            read += 2;

            int loopCounter = 0x10;

            while(read < size) {
                if(loopCounter == 0) {
                    byteMask = (req.ReadByte() << 8) | req.ReadByte();
                    read += 2;
                    loopCounter = 0x10;
                }
                if((byteMask & 0x8000) == 0) {
                    output.Add(req.ReadByte());
                    read += 1;
                } else {
                    var a = req.ReadByte();
                    var b = req.ReadByte();
                    read += 2;

                    var copyCount = (ushort)((a << 4) | (b >> 4));

                    if(copyCount == 0) {
                        copyCount = (ushort)(((b << 8) | req.ReadByte()) + 0x10);
                        var copy = req.ReadByte();
                        read += 2;

                        for(int i = 0; i < copyCount; i++) {
                            output.Add(copy);
                        }
                    } else {
                        int sVar3 = (b & 0xF) + 3;

                        int off = output.Count;
                        for(int i = 0; i < sVar3; i++) {
                            output.Add(output[off - copyCount + i]);
                        }
                    }
                }
                byteMask <<= 1;
                loopCounter--;
            }
            return output.ToArray();
        }

        throw new Exception("Invalid format");
    }
}
