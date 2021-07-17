using System.Collections.Generic;
using System.IO;

namespace Extractor {
    static class SD01 {
        static void Extract(Stream stream, string name) {
            var reader = new BinaryReader(stream);

            var head = reader.ReadBytes(4); // "SD01"
            if (head[0] != 'S' || head[1] != 'D' || head[2] != '0' || head[3] != '1') {
                // not SD01 file
                return;
            }

            var a = reader.ReadInt32();
            var b = reader.ReadInt32();

            List<int> nonsense = new List<int>();

            for (int i = 0; i < a * b; i++) {
                nonsense.Add(reader.ReadInt32());
            }

            var c = reader.ReadInt32();

            if (stream.Position == stream.Length) {
                // no text?
                return;
            }

            List<string> strings = new List<string>();
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                strings.Add(Helper.ReadCString(reader));
            }

            File.WriteAllLines(name, strings);
        }
    }
}
