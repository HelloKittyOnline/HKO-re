using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Extractor {
    class SeanDatabase {
        public int ItemSize;
        public int ItemCount;
        public int[,] Items;
        public Dictionary<int, string> Strings = new Dictionary<int, string>();

        public SeanDatabase(byte[] data) : this(new MemoryStream(data)) { }

        public SeanDatabase(Stream stream) {
            var reader = new BinaryReader(stream);

            var head = reader.ReadBytes(4); // "SD01"
            if(head[0] != 'S' || head[1] != 'D' || head[2] != '0' || head[3] != '1') {
                throw new Exception("Invalid Sean Database");
            }

            ItemSize = reader.ReadInt32();
            ItemCount = reader.ReadInt32();

            var stringByteSize = reader.ReadInt32();

            Items = new int[ItemCount, ItemSize];
            for(int i = 0; i < ItemCount; i++) {
                for(int j = 0; j < ItemSize; j++) {
                    Items[i, j] = reader.ReadInt32();
                }
            }

            if(stream.Position == stream.Length)
                return; // no text?

            var root = stream.Position;
            var stringCount = reader.ReadInt32();
            for(int i = 0; i < stringCount; i++) {
                var pos = (int)(stream.Position - root);
                Strings[pos] = Helper.ReadCString(reader);
            }
        }

        public byte[] Save() {
            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);

            w.Write((byte)'S');
            w.Write((byte)'D');
            w.Write((byte)'0');
            w.Write((byte)'1');

            w.Write(ItemSize);
            w.Write(ItemCount);

            var count = Strings.Sum(x => x.Value.Length + 1);
            int padding = count % 4 == 0 ? 0 : 4 - (count % 4);
            w.Write(count + padding); // stringByteSize

            for(int i = 0; i < ItemCount; i++) {
                for(int j = 0; j < ItemSize; j++) {
                    w.Write(Items[i, j]);
                }
            }

            w.Write(Strings.Count);
            foreach(var s in Strings) {
                w.WriteCString(s.Value);
            }

            for(int i = 0; i < padding; i++) {
                w.Write((byte)0);
            }

            return ms.ToArray();
        }

        public string GetString(int i, int j) {
            Strings.TryGetValue(Items[i, j], out string val);
            return val;
        }
    }
}
