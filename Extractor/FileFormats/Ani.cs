using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

namespace Extractor;

public class AniFrame {
    public string Name;

    public int Width;
    public int Height;

    // saved as rgb565 with optional 5 bit alpha
    public byte[] Pixels;
    public byte[] Alpha;

    [SupportedOSPlatform("windows")]
    public unsafe Bitmap GetBitmap() {
        if(Width == 0 || Height == 0)
            return null;
        var bmp = new Bitmap(Width, Height);

        var data = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        var ptr = (int*)data.Scan0;

        fixed(byte* asd = Pixels) {
            var s = (short*)asd;

            var stride = data.Stride / 4;

            for(int i = 0; i < Width * Height; i++) {
                var val = s[i];

                var b = (val & 0b11111) << 3;
                val >>= 5;
                var g = (val & 0b111111) << 2;
                val >>= 6;
                var r = (val & 0b11111) << 3;

                var a = 255;
                if(Alpha != null)
                    a = Alpha[i] << 3;

                var x = i % Width;
                var y = i / Width;

                ptr[x + y * stride] = b | g << 8 | r << 16 | a << 24;
            };
        }

        bmp.UnlockBits(data);

        return bmp;
    }

    public unsafe int[] GetRgba() {
        if(Width == 0 || Height == 0)
            return null;

        var data = new int[Width * Height];

        fixed(byte* asd = Pixels) {
            var s = (short*)asd;

            for(int i = 0; i < Width * Height; i++) {
                var val = s[i];

                var b = (val & 0b11111) << 3;
                val >>= 5;
                var g = (val & 0b111111) << 2;
                val >>= 6;
                var r = (val & 0b11111) << 3;

                var a = 255;
                if(Alpha != null)
                    a = Alpha[i] << 3;

                data[i] = r | g << 8 | b << 16 | a << 24;
            };
        }

        return data;
    }
}

public class Ani {
    public AniFrame[] Frames;

    public static Ani Extract(string path) {
        Stream file = File.OpenRead(path);
        var reader = new BinaryReader(file);

        if(Helper.ReadCompressed(reader, out var data)) {
            file.Close();
            file = new MemoryStream(data);
            reader = new BinaryReader(file);
        }

        var head = reader.ReadBytes(4);
        reader.ReadInt32();
        reader.ReadInt16();

        if(head[0] == 'W' && head[1] == 'A' && head[2] == 'V' && head[3] == 'E') {
            // 1000aff4
            // list of sound files
            var count = reader.ReadInt16();
            var strings = reader.ReadBytes(40 * 20); // 40 * char[20]

            if(count != 0) {
                // 1000b020
                throw new NotImplementedException();
            }
        }

        var pixelFormat = reader.ReadInt16();
        // 0 = rgb555
        // 1 = rgb565
        if(pixelFormat != 1) {
            throw new NotImplementedException();
        }

        if(reader.ReadCString(16) == "ANI_001") {
            return ReadStuff(reader);
        } else {
            throw new NotImplementedException();
        }
    }

    public static Ani ReadStuff(BinaryReader reader) {
        reader.ReadInt32(); // always 0

        var frameStart = reader.ReadInt32();
        var frameEnd = reader.ReadInt32();

        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt16();
        reader.ReadInt16(); // always 0

        int frameCount = frameEnd - frameStart;

        reader.ReadBytes(128); // unused?

        var res = new List<AniFrame>();

        var name = reader.ReadCString(16);
        if(name == "ANI_FRAME_51") {
            for(int i = 0; i < frameCount; i++) {
                // mapData_ani
                // var asdf = reader.ReadBytes(13 * 4);

                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                var width = reader.ReadInt32();
                var height = reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();

                var tex = ReadFrame(reader);
                res.Add(tex);
            }
        } else {
            reader.BaseStream.Position -= 16;

            for(int i = 0; i < frameCount; i++) {
                var asdf = reader.ReadBytes(11 * 4);
                var tex = ReadFrame(reader);
                res.Add(tex);
            }
        }

        return new Ani { Frames = res.ToArray() };
    }

    public static AniFrame ReadFrame(BinaryReader reader) {
        byte[] normal;
        byte[] alpha = null;

        // read texture
        if(reader.ReadCString(10) == "PALETTE") {
            throw new NotImplementedException();
        } else {
            reader.BaseStream.Position -= 10;
        }

        var useRle = reader.ReadUInt16() == 54321;
        int normalSize = 0, alphaSize = 0;
        if(useRle) {
            normalSize = reader.ReadInt32();
            alphaSize = reader.ReadInt32();
        } else {
            reader.BaseStream.Position -= 2;
        }

        var imageName = reader.ReadBig5(0x50);

        var width = reader.ReadUInt16();
        var height = reader.ReadUInt16();

        int padding = (width >> 4) * (height >> 4) * 2;

        byte[] something;
        if(padding == 0 || !Helper.ReadCompressed(reader, out something)) {
            something = reader.ReadBytes(padding);
        }

        if(!Helper.ReadCompressed(reader, out normal)) {
            normal = reader.ReadBytes(normalSize);
        }
        if(useRle) {
            normal = RleDecompress(normal, width * height * 2);
        }

        if(reader.BaseStream.Position < reader.BaseStream.Length) {
            if(reader.ReadUInt16() == 12345) {
                if(!Helper.ReadCompressed(reader, out alpha)) {
                    alpha = reader.ReadBytes(alphaSize);
                }

                if(useRle) {
                    alpha = RleDecompress(alpha, width * height);
                }
            } else {
                reader.BaseStream.Position -= 2;
            }
        }

        return new AniFrame {
            Name = imageName,
            Width = width,
            Height = height,
            Pixels = normal,
            Alpha = alpha
        };
    }

    private static unsafe byte[] RleDecompress(byte[] data, int outSize) {
        var imgData = new byte[outSize];

        fixed(byte* data_b = data) {
            var data_s = (ushort*)data_b;

            fixed(byte* img_b = imgData) {
                var img_s = (ushort*)img_b;

                while(data_s < data_b + data.Length) {
                    var val = *data_s;
                    data_s++;

                    if(val == 12345) {
                        var count = *data_s;
                        data_s++;

                        val = *data_s;
                        data_s++;

                        for(int j = 0; j < count; j++) {
                            *img_s = val;
                            img_s++;
                        }
                    } else {
                        *img_s = val;
                        img_s++;
                    }
                }

            }
        }

        return imgData;
    }
}
