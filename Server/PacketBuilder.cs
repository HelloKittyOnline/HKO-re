using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Serilog.Events;

namespace Server;

interface IWriteAble {
    public void Write(ref PacketBuilder b);
}

struct PacketBuilder {
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
        item.Write(ref this);
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

    private void UpdateLength() {
        Debug.Assert(!CompressMode);
        buffer.TryGetBuffer(out var buf); // normal GetBuffer returns unused space

        // update data length
        var dataLength = buf.Count - 5;
        buf[3] = (byte)(dataLength & 0xFF);
        buf[4] = (byte)(dataLength >> 8);
    }

    public void Send(Client client) {
        UpdateLength();
        buffer.TryGetBuffer(out var buf); // normal GetBuffer returns unused space

        if(Logging.Logger.IsEnabled(LogEventLevel.Verbose) && buf.Count >= 7 && !(buf[5] == 0x00 && buf[6] == 0x63)) {
            Logging.Logger.Verbose("[{username}_{userID}] S -> C: {major:X2}_{minor:X2} {data}", client.Username, client.DiscordId, buf[5], buf[6], buf.AsMemory());
        }

        client.Send(buf);
    }

    public void Send(IEnumerable<Client> clients) {
        UpdateLength();
        buffer.TryGetBuffer(out var buf);

        var doLog = Logging.Logger.IsEnabled(LogEventLevel.Verbose) && buf.Count >= 7 && !(buf[5] == 0x00 && buf[6] == 0x63);

        foreach(var client in clients) {
            if(doLog) {
                Logging.Logger.Verbose("[{username}_{userID}] S -> C: {major:X2}_{minor:X2} {data}", client.Username, client.DiscordId, buf[5], buf[6], buf.AsMemory());
            }
            client.Send(buf);
        }
    }

    public void Send(Span<Client> clients) {
        UpdateLength();
        buffer.TryGetBuffer(out var buf);

        var doLog = Logging.Logger.IsEnabled(LogEventLevel.Verbose) && buf.Count >= 7 && !(buf[5] == 0x00 && buf[6] == 0x63);

        foreach(var client in clients) {
            if(doLog) {
                Logging.Logger.Verbose("[{username}_{userID}] S -> C: {major:X2}_{minor:X2} {data}", client.Username, client.DiscordId, buf[5], buf[6], buf.AsMemory());
            }
            client.Send(buf);
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
}
