using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Ionic.Zlib;

namespace Extractor {
    static class Helper {
        static Encoding big5 = CodePagesEncodingProvider.Instance.GetEncoding(950); // Chinese big5 encoding

        public static byte[] ExtractZlib(byte[] data) {
            var memStream = new MemoryStream(data);
            var stream = new ZlibStream(memStream, CompressionMode.Decompress);
            var ms = new MemoryStream();
            stream.CopyTo(ms);

            return ms.ToArray();
        }

        public static string ReadCString(this BinaryReader reader) {
            List<byte> buffer = new List<byte>();
            while (true) {
                var c = reader.ReadByte();
                if (c == 0) break;
                buffer.Add(c);
            }
            return big5.GetString(buffer.ToArray());
        }
        public static string ReadCString(this BinaryReader reader, int length) {
            return CstrToString(reader.ReadBytes(length));
        }

        // only used by sean database/archive
        public static void WriteCString(this BinaryWriter writer, string s) {
            foreach (var t in s) {
                writer.Write((byte)t);
            }
            writer.Write((byte)0);
        }

        public static void WriteCString(this BinaryWriter writer, string s, int length) {
            var bytes = big5.GetBytes(s);

            if (bytes.Length + 1 > length) {
                throw new ArgumentOutOfRangeException(nameof(s), "string to big for given window");
            }
            
            writer.Write(bytes);
            writer.Write((byte)0); // null terminator

            for (int i = bytes.Length + 1; i < length; i++) {
                writer.Write((byte)0);
            }
        }

        private static string CstrToString(byte[] data) {
            int inx = Array.FindIndex(data, 0, x => x == 0); //search for 0

            if (inx >= 0)
                return big5.GetString(data, 0, inx);

            return big5.GetString(data);
        }

        public static bool ReadZZZ(BinaryReader reader, out byte[] output) {
            if (reader.ReadInt32() == 0x5A5A5A) {
                var len = reader.ReadInt32();

                output = ExtractZlib(reader.ReadBytes(len));
                return true;
            } else {
                reader.BaseStream.Position -= 4;
                output = null;
                return false;
            }
        }

        public static bool ReadZZZ_ex(BinaryReader reader, out byte[] output) {
            if (reader.ReadInt32() == 0x5A5A5A) {
                var len = reader.ReadInt32();
                var uncompressedLen = reader.ReadInt32();

                output = ExtractZlib(reader.ReadBytes(len));
                return true;
            } else {
                reader.BaseStream.Position -= 4;
                output = null;
                return false;
            }
        }

        public static void SaveGif(string path, List<Bitmap> Bitmaps) {
            // Gdi+ constants absent from System.Drawing.
            const int PropertyTagFrameDelay = 0x5100;
            const int PropertyTagLoopCount = 0x5101;
            const short PropertyTagTypeLong = 4;
            const short PropertyTagTypeShort = 3;

            const int UintBytes = 4;

            //...

            var gifEncoder = GetEncoder(ImageFormat.Gif);
            // Params of the first frame.
            var encoderParams1 = new EncoderParameters(1);
            encoderParams1.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
            // Params of other frames.
            var encoderParamsN = new EncoderParameters(1);
            encoderParamsN.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);
            // Params for the finalizing call.
            var encoderParamsFlush = new EncoderParameters(1);
            encoderParamsFlush.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag, (long)EncoderValue.Flush);

            // PropertyItem for the frame delay (apparently, no other way to create a fresh instance).
            var frameDelay = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            frameDelay.Id = PropertyTagFrameDelay;
            frameDelay.Type = PropertyTagTypeLong;
            // Length of the value in bytes.
            frameDelay.Len = Bitmaps.Count * UintBytes;
            // The value is an array of 4-byte entries: one per frame.
            // Every entry is the frame delay in 1/100-s of a second, in little endian.
            frameDelay.Value = new byte[Bitmaps.Count * UintBytes];
            // E.g., here, we're setting the delay of every frame to 1 second.
            var frameDelayBytes = BitConverter.GetBytes((uint)10);
            for (int j = 0; j < Bitmaps.Count; ++j) {
                Array.Copy(frameDelayBytes, 0, frameDelay.Value, j * UintBytes, UintBytes);
            }

            // PropertyItem for the number of animation loops.
            var loopPropertyItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            loopPropertyItem.Id = PropertyTagLoopCount;
            loopPropertyItem.Type = PropertyTagTypeShort;
            loopPropertyItem.Len = 2;
            // 0 means to animate forever.
            loopPropertyItem.Value = BitConverter.GetBytes((ushort)0);

            using var stream = new FileStream(path, FileMode.Create);
            bool first = true;
            Bitmap firstBitmap = null;

            // Bitmaps is a collection of Bitmap instances that'll become gif frames.
            foreach (var bitmap in Bitmaps) {
                if (first) {
                    firstBitmap = bitmap;
                    firstBitmap.SetPropertyItem(frameDelay);
                    firstBitmap.SetPropertyItem(loopPropertyItem);
                    firstBitmap.Save(stream, gifEncoder, encoderParams1);
                    first = false;
                } else {
                    firstBitmap.SaveAdd(bitmap, encoderParamsN);
                }
            }
            firstBitmap.SaveAdd(encoderParamsFlush);
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format) {
            return ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}
