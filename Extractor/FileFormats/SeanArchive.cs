using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Extractor {
    public static class SeanArchive {
        public class Item {
            public string Name;
            public byte[] Contents;
        }

        public static Item[] Extract(string path) {
            using var file = File.OpenRead(path);
            return Extract(file);
        }

        public static Item[] Extract(byte[] data) {
            return Extract(new MemoryStream(data));
        }

        public static Item[] Extract(Stream stream) {
            var reader = new BinaryReader(stream);

            var head = reader.ReadBytes(4); // "SAR1"
            if(head[0] != 'S' || head[1] != 'A' || head[2] != 'R' || head[3] != '1') {
                throw new Exception("Not a sdb file");
            }
            reader.ReadInt32(); // version = 1
            var fileCount = reader.ReadInt32();
            reader.ReadInt32(); // total size of file names

            var files = new string[fileCount];
            for(int i = 0; i < fileCount; i++) {
                files[i] = Helper.ReadCString(reader);
            }

            // pad to 4 bytes
            while(stream.Position % 4 != 0) {
                reader.ReadByte();
            }

            var ret = new Item[fileCount];

            for(int i = 0; i < fileCount; i++) {
                var size = reader.ReadInt32();
                var data = reader.ReadBytes(size);

                ret[i] = new Item {
                    Name = files[i],
                    Contents = Helper.ExtractZlib(data)
                };
            }

            return ret;
        }
        public static byte[] Create(Item[] files) {
            var ms = new MemoryStream(0);
            var writer = new BinaryWriter(ms);

            writer.Write(new[] { 'S', 'A', 'R', '1' });
            writer.Write(1); // version
            writer.Write(files.Length);
            var size = files.Sum(x => x.Name.Length + 1);
            while(size % 4 != 0) {
                size++;
            }
            writer.Write(size); // total size of file names

            foreach(var file in files) {
                writer.WriteCString(file.Name);
            }

            while(ms.Position % 4 != 0) {
                writer.Write((byte)0);
            }

            foreach(var f in files) {
                var temp = new MemoryStream(0);
                var stream = new ZLibStream(temp, CompressionMode.Compress, true);
                stream.Write(f.Contents);
                stream.Close();

                var buf = temp.ToArray();

                writer.Write(buf.Length);
                writer.Write(buf);
            }

            while(ms.Position % 4 != 0) {
                writer.Write((byte)0);
            }

            return ms.ToArray();
        }
    }
}
