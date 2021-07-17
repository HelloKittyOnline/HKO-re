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

            stream.Write(data.ToArray(), 0, data.Count);
        }
    }
}
