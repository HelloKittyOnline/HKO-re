using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;

namespace Extractor {
    public class Man {
        public Ani[] Animations { get; set; }

        public Man(string path) {
            var file = File.OpenRead(path);
            var reader = new BinaryReader(file);

            var idk = reader.ReadInt16();

            var shorts = new ushort[104];
            for(int i = 0; i < shorts.Length; i++) {
                shorts[i] = reader.ReadUInt16();
            }

            // skip some stuff?
            // var idk = reader.ReadBytes(104 * 2);

            if(reader.ReadCString(16) == "Static") {
                throw new NotImplementedException();
            }

            reader.BaseStream.Position -= 16;

            var anis = new List<Ani>();

            while(file.Position < file.Length) {
                var d = ReadManAni(reader);
                if(d != null)
                    anis.Add(d);
            }

            Animations = anis.ToArray();
        }

        [SupportedOSPlatform("windows")]
        private static Bitmap MakeSheet(List<Bitmap> images) {
            int width = 0;
            int height = 0;

            foreach(var bitmap in images) {
                width += bitmap.Width;
                height = Math.Max(height, bitmap.Height);
            }

            var sheet = new Bitmap(width, height);

            using var g = Graphics.FromImage(sheet);

            int x = 0;
            foreach(var img in images) {
                g.DrawImageUnscaled(img, x, 0);
                x += img.Width;
            }

            return sheet;
        }

        private static Ani ReadManAni(BinaryReader reader) {
            var sectionName = reader.ReadCString(16);
            if(sectionName == "ANI_001") {
                return Ani.ReadStuff(reader);
            } else if(sectionName == "MA1") {
                // 1000a6f2
                // "檔案版本太舊,無法載入!" -> "The file version is too old and cannot be loaded!"
                throw new NotImplementedException();
            } else if(sectionName == "MA2") {
                // 1000a71e
                reader.BaseStream.Position -= 8;
                var data = reader.ReadBytes(0x40 * 4);
            } else {
                throw new NotImplementedException();
            }

            return null;
        }
    }
}
