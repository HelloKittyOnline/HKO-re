using System;
using System.IO;

namespace Extractor {
    class Map {
        public static void Extract(string path, string outPath) {
            Stream file = File.OpenRead(path);
            var reader = new BinaryReader(file);

            if(Helper.ReadZZZ(reader, out var data)) {
                file.Close();
                file = new MemoryStream(data);
                reader = new BinaryReader(file);
            }

            var title = reader.ReadCString(8);
            reader.BaseStream.Position -= 8;

            switch(title) {
                case "MAP 5.1": 
                    ReadMap51(reader, outPath);
                    break;
                case "MAP 5.0":
                    ReadMap50(reader);
                    break;
                default:
                    throw new Exception("Map Version too old");
            }
        }

        private static void ReadMap51(BinaryReader reader, string outPath) {
            var stuff = reader.ReadBytes(0x3FE8);

            // determined by some global
            int idk = false ? 300 : 0x30;

            var moreStuff = reader.ReadBytes(idk * BitConverter.ToInt32(stuff, 3 * 4) * BitConverter.ToInt32(stuff, 4 * 4) * 3 * 4);

            Read_Map_Att_Data_1(reader, BitConverter.ToInt32(stuff, 14 * 4));
            Read_Map_Att_Data_2(reader, BitConverter.ToInt32(stuff, 17 * 4));

            while(reader.BaseStream.Position < reader.BaseStream.Length) {
                var name = reader.ReadCString(0x10);
                reader.BaseStream.Position -= 0x10;

                switch(name) {
                    case "ANI_LINK":
                        Read_Map_Att_Data_3(reader);
                        break;
                    case "Animation":
                        ReadAnimation(reader);
                        break;
                    case "Man_Libary":
                        ReadManLibary(reader);
                        break;
                    case "Man_Link":
                        reader.ReadBytes(0x10);
                        break; // don't care
                    case "Attribute":
                        Read_Map_Att_Data_4(reader);
                        break;
                    case "PICTURE":
                        ReadPicture(reader, outPath);
                        break;
                    default:
                        throw new Exception($"Read [{name}] error of Read_Map51!");
                }
            }
        }

        private static void Read_Map_Att_Data_1(BinaryReader reader, int idk) {
            if(!Helper.ReadZZZ_ex(reader, out _)) {
                reader.ReadBytes(idk * 4 * 8);
            }
        }
        private static void Read_Map_Att_Data_2(BinaryReader reader, int idk) {
            if(!Helper.ReadZZZ_ex(reader, out _)) {
                reader.ReadBytes(idk * 4 * 0xd);
            }
        }
        private static void Read_Map_Att_Data_3(BinaryReader reader) {
            reader.ReadBytes(0x10); // ANI_LINK
            var idk = reader.ReadInt32();

            if(Helper.ReadZZZ_ex(reader, out var data))
                return;

            if(idk > 0) {
                reader.ReadBytes(0x27 * 4 * idk);
            }
        }
        private static void ReadAnimation(BinaryReader reader) {
            reader.ReadBytes(0x10); // Animation
            var idk = reader.ReadInt32();

            for(int i = 0; i < idk; i++) {
                reader.ReadBytes(0x66);
            }
        }
        private static void ReadManLibary(BinaryReader reader) {
            reader.ReadBytes(0x10); // Animation
            var idk = reader.ReadInt32();

            for(int i = 0; i < idk; i++) {
                reader.ReadBytes(0x75 * 4);
            }
        }
        private static void Read_Map_Att_Data_4(BinaryReader reader) {
            reader.ReadBytes(0x10); // Attribute

            if(!Helper.ReadZZZ_ex(reader, out var data)) {
                throw new NotImplementedException();
            }
        }
        private static void ReadPicture(BinaryReader reader, string outPath) {
            reader.ReadBytes(0x10); // Attribute
            var count = reader.ReadInt32();

            for(int i = 0; i < count; i++) {
                var (imageName, img) = Man.ReadTexture(reader);
                if(img == null)
                    continue;

                imageName = imageName.Replace("?", "_"); // replace missing chinese characters

                img.Save($"{outPath}/{Path.GetFileNameWithoutExtension(imageName)}.png");
            }
        }

        private static void ReadMap50(BinaryReader reader) {
            throw new NotImplementedException();
        }
    }
}
