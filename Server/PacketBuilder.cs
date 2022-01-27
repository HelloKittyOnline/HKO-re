using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Server {
    class PacketBuilder {
        // private List<byte> data = Encoding.ASCII.GetBytes("^%*\0\0").ToList();
        private MemoryStream buffer;
        private BinaryWriter writer;

        private bool CompressMode = false;
        private int CompressPos = 0;

        public PacketBuilder() {
            buffer = new MemoryStream();
            writer = new BinaryWriter(buffer);

            WriteByte((byte)'^');
            WriteByte((byte)'%');
            WriteByte((byte)'*');
            WriteShort(0);
        }

        public void WriteByte(byte v) {
            writer.Write(v);
        }

        public void WriteShort(short v) {
            writer.Write(v);
        }

        public void WriteInt(int v) {
            writer.Write(v);
        }
        public void Write0(int bytes) {
            for (int i = 0; i < bytes; i++) {
                writer.Write((byte)0);
            }
        }

        public void AddString(string str, int pre) {
            switch(pre) {
                case 1:
                    if(str.Length > 255) {
                        throw new ArgumentOutOfRangeException("string too long");
                    }
                    WriteByte((byte)str.Length);
                    break;
                case 2:
                    if(str.Length > 65535) {
                        throw new ArgumentOutOfRangeException("string too long");
                    }
                    WriteShort((short)str.Length);
                    break;
                case 4:
                    if(str.Length > 65535) {
                        throw new ArgumentOutOfRangeException("string too long");
                    }
                    WriteInt(str.Length);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("invalid pre size");
            }
            writer.Write(Encoding.UTF8.GetBytes(str));
        }

        public void AddWstring(string str) {
            var dat = Encoding.Unicode.GetBytes(str);

            if(dat.Length > 65535) {
                throw new ArgumentOutOfRangeException("string too long");
            }
            WriteShort((short)dat.Length);
            writer.Write(dat);
        }

        public void Send(Stream stream) {
            var buf = buffer.GetBuffer();

            // update data length
            var dataLength = buffer.Position - 5;
            buf[3] = (byte)(dataLength & 0xFF);
            buf[4] = (byte)(dataLength >> 8);

#if DEBUG
            if(dataLength >= 2) Console.WriteLine($"S -> C: {buf[5]:X2}_{buf[6]:X2}");
#endif
            lock(stream) {
                stream.Write(buf, 0, (int)buffer.Position);
            }
        }

        public void BeginCompress() {
            if(CompressMode) throw new Exception("Already in compression mode");
            CompressMode = true;
            CompressPos = (int)buffer.Position;

            WriteShort(0); // placeholder for length
            WriteShort(0);
            WriteByte(0x82); // don't bother encoding just use raw
        }

        public void EndCompress() {
            if(!CompressMode) throw new Exception("Have to be in compression mode");
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

            if(data.Length == 0) return;

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
}
