using System;
using System.IO;

namespace Extractor {
    static class Ani {
        public static void Extract(string path, string outPath) {
            Stream file = File.OpenRead(path);
            var reader = new BinaryReader(file);

            if (Helper.ReadZZZ(reader, out var data)) {
                file.Close();
                file = new MemoryStream(data);
                reader = new BinaryReader(file);
            }

            if (reader.ReadUInt32() == 1163280727) { // WAVE
                reader.ReadBytes(0x32E - 4);
                Man.ExtractManAni(reader, outPath);
            } else {
                throw new Exception("Invalid file header");
            }
        }
    }
}
