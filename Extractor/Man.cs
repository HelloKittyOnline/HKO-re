using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Extractor {
    static class Man {
        private static byte[] RleDecompress(byte[] data, int outSize) {
            var img = new byte[outSize];

            int pos = 0;
            int i = 0;
            while (i < data.Length) {
                if (i + 2 <= data.Length && BitConverter.ToUInt16(data, i) == 0x3039) {
                    i += 2;

                    var count = BitConverter.ToUInt16(data, i);
                    i += 2;

                    for (int j = 0; j < count; j++) {
                        img[pos++] = data[i];
                        img[pos++] = data[i + 1];
                    }

                    i += 2;
                } else {
                    img[pos++] = data[i++];
                    img[pos++] = data[i++];
                }
            }

            return img;
        }

        private static Bitmap RenderImg(byte[] normal, byte[] alpha, int width, int height) {
            if (width == 0 || height == 0)
                return null;
            Bitmap bmp = new Bitmap(width, height);

            for (int i = 0; i < width * height; i++) {
                var val = BitConverter.ToUInt16(normal, i * 2);

                var b = (val & 0b11111) << 3;
                val >>= 5;
                var g = (val & 0b111111) << 2;
                val >>= 6;
                var r = (val & 0b11111) << 3;

                int a = 255;
                if (alpha != null) a = alpha[i] << 3;

                bmp.SetPixel(i % width, i / width, Color.FromArgb(a, r, g, b));
            }

            return bmp;
        }

        private static Bitmap MakeSheet(List<Bitmap> images) {
            int width = 0;
            int height = 0;

            foreach (var bitmap in images) {
                width += bitmap.Width;
                height = Math.Max(height, bitmap.Height);
            }

            Bitmap sheet = new Bitmap(width, height);

            using Graphics g = Graphics.FromImage(sheet);

            int x = 0;
            foreach (var img in images) {
                g.DrawImageUnscaled(img, x, 0);
                x += img.Width;
            }

            return sheet;
        }

        public static void Extract(string path, string outPath) {
            var file = File.OpenRead(path);
            var reader = new BinaryReader(file);

            // skip some stuff?
            reader.ReadBytes(0xD2);

            while (file.Position < file.Length) {
                ExtractManAni(reader, outPath);
            }
        }

        public static void ExtractManAni(BinaryReader reader, string outPath) {
            int numImages = 0;

            var sectionName = reader.ReadCString(0x10);
            if (sectionName == "ANI_001") {
                var data = reader.ReadBytes(0x98);

                numImages = BitConverter.ToInt32(data, 8) - BitConverter.ToInt32(data, 4);
            } else if (sectionName == "MA1") {
                // "檔案版本太舊,無法載入!" -> "The file version is too old and cannot be loaded!"
                throw new NotImplementedException();
            } else if (sectionName == "MA2") {
                reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                return;
            }

            int gapSize;
            if (reader.ReadCString(0x10) == "ANI_FRAME_51") {
                gapSize = 0x34;
            } else {
                reader.BaseStream.Position -= 0x10;
                gapSize = 0x2C;
            }

            // var images = new List<Bitmap>();
            // string name = null;

            for (int i = 0; i < numImages; i++) {
                reader.ReadBytes(gapSize);

                var (imageName, img) = ReadTexture(reader);
                if (img == null) continue;

                imageName = imageName.Replace("?", "_"); // replace missing chinese characters
                // name ??= imageName;

                img.Save($"{outPath}/{Path.GetFileNameWithoutExtension(imageName)}.png");
                // images.Add(img);
            }

            // outPath = $"{outPath}/{Path.GetFileNameWithoutExtension(name)}.png";
            // if (!File.Exists(outPath) && images.Count != 0) makeSheet(images).Save(outPath);
            // saveGif($"D:/Daten/Desktop/extract/{Path.GetFileNameWithoutExtension(name)}.gif", images);
        }

        public static (string, Bitmap) ReadTexture(BinaryReader reader) {
            byte[] normal;
            byte[] alpha = null;

            // read texture
            if (reader.ReadCString(10) == "PALETTE") {
                throw new NotImplementedException();
            } else {
                reader.BaseStream.Position -= 10;
            }

            var useRle = reader.ReadUInt16() == 0xD431;
            int normalSize = 0, alphaSize = 0;
            if (useRle) {
                normalSize = reader.ReadInt32();
                alphaSize = reader.ReadInt32();
            } else {
                reader.BaseStream.Position -= 2;
            }

            var imageName = reader.ReadCString(0x50);

            var width = reader.ReadUInt16();
            var height = reader.ReadUInt16();

            int padding = (width >> 4) * (height >> 4) * 2;

            if (padding == 0 || !Helper.ReadZZZ(reader, out _)) {
                reader.ReadBytes(padding);
            }

            if (!Helper.ReadZZZ(reader, out normal)) {
                normal = reader.ReadBytes(normalSize);
            }

            if (reader.BaseStream.Position < reader.BaseStream.Length) {
                if (reader.ReadUInt16() == 0x3039) {
                    if (!Helper.ReadZZZ(reader, out alpha)) {
                        alpha = reader.ReadBytes(alphaSize);
                    }

                    if (useRle) {
                        alpha = RleDecompress(alpha, width * height);
                    }
                } else {
                    reader.BaseStream.Position -= 2;
                }
            }

            if (imageName == "?w???I.bmp") return ("???I.bmp", null);

            if (useRle) {
                normal = RleDecompress(normal, width * height * 2);
            }

            return (imageName, RenderImg(normal, alpha, width, height));
        }
    }
}
