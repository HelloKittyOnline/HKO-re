using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Server {
    class PacketBuilder {
        private List<byte> data = Encoding.ASCII.GetBytes("^%*\0\0").ToList();

        public void Add(byte v) {
            data.Add(v);
        }

        public void Add(short v) {
            data.AddRange(BitConverter.GetBytes(v));
        }

        public void Add(int v) {
            data.AddRange(BitConverter.GetBytes(v));
        }

        public void AddString(string str, int pre = 1) {
            switch (pre) {
                case 1:
                    if (str.Length > 255) {
                        throw new ArgumentOutOfRangeException("string too long");
                    }
                    data.Add((byte)str.Length);
                    break;
                case 2:
                    if (str.Length > 65535) {
                        throw new ArgumentOutOfRangeException("string too long");
                    }
                    Add((short)str.Length);
                    break;
                case 4:
                    if (str.Length > 65535) {
                        throw new ArgumentOutOfRangeException("string too long");
                    }
                    Add(str.Length);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("invalid pre size");
            }
            data.AddRange(Encoding.ASCII.GetBytes(str));
        }

        public void AddWstring(string str) {
            var dat = Encoding.Unicode.GetBytes(str);

            if (dat.Length > 65535) {
                throw new ArgumentOutOfRangeException("string too long");
            }
            Add((short)dat.Length);
            data.AddRange(dat);
        }

        public void Send(Stream stream) {
            // update data length
            var length = data.Count - 5;
            data[3] = (byte)(length & 0xFF);
            data[4] = (byte)(length >> 8);

#if DEBUG
            Console.WriteLine($"S -> C: {data[5]:X2}_{data[6]:X2}");
#endif

            stream.Write(data.ToArray(), 0, data.Count);
        }

        public void EncodeCrazy(byte[] data) {
            Add((short)(data.Length + 1));
            Add((short)data.Length);

            if (data.Length == 0) return;

            // don't bother encoding just use raw
            Add((byte)0x82);

            foreach(var t in data) Add(t);
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
                    if (loopCounter == 0) {
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

                        if (copyCount == 0) {
                            copyCount = (ushort)(((b << 8) | req.ReadByte()) + 0x10);
                            var copy = req.ReadByte();
                            read += 2;

                            for(int i = 0; i < copyCount; i++) {
                                output.Add(copy);
                            }
                        } else {
                            int sVar3 = (b & 0xF) + 3;

                            int off = output.Count;
                            for (int i = 0; i < sVar3; i++) {
                                output.Add(output[off - copyCount + i]);
                            }
                        }
                    }
                    byteMask <<= 1;
                    loopCounter--;
                }
                return output.ToArray();
            }

            return null;
        }
    }
}
