using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace Extractor {
    public class Map {
        public int baseWidth;
        public int baseHeight;
        public int tileWidth;
        public int tileHeight;
        public int w2;
        public int h2;
        public int viewX;
        public int viewY;
        public int viewWidth;
        public int viewHeight;

        public int ICON_MAX;
        public int Icon_count;
        public int ANI_MAX;
        public int Ani_count;
        public int ANI_LINK_MAX;
        public int Ani_Link_count;

        public int number_of_objects;
        public int mapType;

        public Tile[,] tileMap; // tileWidth * tileHeight
        public Icon[] icons;
        public Ani[] anis;
        public AniLink[] aniLinks;
        public short[,,] collisionMap; // w2 x h2 x 2
        public AniFrame[] pictures;

        public struct Tile {
            public int imageId;
            public int xOffset;
            public int yOffset;
        }

        public struct Icon {
            public int id;
            public int top;
            public int left;
            public int bottom;
            public int right;
            public int pictureId;
            public int x;
            public int y;
        }

        public struct Ani {
            public byte flags { get; set; }
            public int top { get; set; }
            public int left { get; set; }
            public int bottom { get; set; }
            public int right { get; set; }
            public int pictureId { get; set; }
            public int x { get; set; }
            public int y { get; set; }
            public byte tickId { get; set; }
        }

        public struct AniLink {
            public int id;
            public int data_id_1;
            public int data_id_2;
            public int v3;
            public int posX;
            public int posY;
            public int v4;
            public int v5;
            public int v6;
            public int v7;
            public int v8;
        }

        public Map(string path) {
            Stream file = File.OpenRead(path);
            var reader = new BinaryReader(file);

            if(Helper.ReadCompressed(reader, out var data)) {
                // never happens
                file.Close();
                file = new MemoryStream(data);
                reader = new BinaryReader(file);
            }

            var title = reader.ReadCString(10);

            switch(title) {
                case "MAP 5.1":
                    ReadMap51(reader);
                    break;
                case "MAP 5.0":
                    throw new NotImplementedException(); // never actually happens
                default:
                    throw new Exception("Map Version too old");
            }
        }

        private void ReadMap51(BinaryReader reader) {
            reader.ReadInt16();

            baseWidth = reader.ReadInt32();
            baseHeight = reader.ReadInt32();
            tileWidth = reader.ReadInt32();
            tileHeight = reader.ReadInt32();
            w2 = reader.ReadInt32();
            h2 = reader.ReadInt32();
            viewX = reader.ReadInt32();
            viewY = reader.ReadInt32();
            viewWidth = reader.ReadInt32();
            viewHeight = reader.ReadInt32();

            reader.ReadInt32(); // pointer to tile data

            ICON_MAX = reader.ReadInt32();
            Icon_count = reader.ReadInt32();

            reader.ReadInt32(); // pointer to icon data

            ANI_MAX = reader.ReadInt32();
            Ani_count = reader.ReadInt32();

            reader.ReadInt32(); // pointer to ani data

            ANI_LINK_MAX = reader.ReadInt32();
            Ani_Link_count = reader.ReadInt32();

            reader.ReadInt32();

            var soundFileCount = reader.ReadInt16(); // always 0
            var soundFileData = reader.ReadBytes(200 * 80);

            Debug.Assert(soundFileCount == 0);
            Debug.Assert(soundFileData.All(x => x == 0));

            reader.ReadInt16(); // padding

            number_of_objects = reader.ReadInt32();
            reader.ReadInt32();

            mapType = reader.ReadInt32();

            int hMul;
            int wMul;
            if(mapType == 1) {
                wMul = 20;
                hMul = 15;
            } else {
                wMul = 8;
                hMul = 6;
            }

            Debug.Assert(tileWidth == wMul * baseWidth);
            Debug.Assert(tileHeight == hMul * baseHeight);

            reader.ReadBytes(0x3FE8 - (int)reader.BaseStream.Position);

            tileMap = new Tile[tileWidth, tileHeight];
            for(int i = 0; i < tileHeight; i++) {
                for(int j = 0; j < tileWidth; j++) {
                    tileMap[j, i] = new Tile {
                        imageId = reader.ReadInt32(),
                        xOffset = reader.ReadInt32(),
                        yOffset = reader.ReadInt32()
                    };
                }
            }

            Read_Map_Att_Data_1(reader, ICON_MAX, Icon_count); // always empty
            Read_Map_Att_Data_2(reader, ANI_MAX, Ani_count);

            while(reader.BaseStream.Position < reader.BaseStream.Length) {
                var name = reader.ReadCString(0x10);

                switch(name) {
                    case "ANI_LINK":
                        Read_Ani_Link(reader);
                        break;
                    case "Animation":
                        ReadAnimation(reader);
                        break;
                    case "Man_Libary":
                        ReadManLibary(reader);
                        break;
                    case "Man_Link":
                        if(number_of_objects > 0) {
                            throw new NotImplementedException();
                        }
                        break;
                    case "Attribute":
                        ReadAttribute(reader);
                        break;
                    case "PICTURE":
                        ReadPicture(reader);
                        break;
                    default:
                        throw new Exception($"Read [{name}] error of Read_Map51!");
                }
            }
        }

        [SupportedOSPlatform("windows")]
        public Bitmap RenderBaseMap() {
            if(pictures.Length == 0)
                return null;

            var bmp = new Bitmap(tileWidth * 80, tileHeight * 80);
            var g = Graphics.FromImage(bmp);

            for(int y = 0; y < tileHeight; y++) {
                for(int x = 0; x < tileWidth; x++) {
                    var tile = tileMap[x, y];

                    var img = pictures[tile.imageId];
                    var sub = img.GetBitmap();

                    g.DrawImage(sub, x * 80, y * 80, new Rectangle {
                        X = tile.xOffset * 80,
                        Y = tile.yOffset * 80,
                        Width = 80,
                        Height = 80
                    }, GraphicsUnit.Pixel);

                    sub.Dispose();
                }
            }

            return bmp;
        }

        [SupportedOSPlatform("windows")]
        public Bitmap RenderFullMap() {
            var bmp = RenderBaseMap();
            var g = Graphics.FromImage(bmp);

            foreach(var aniLink in aniLinks) {
                var ani = anis[aniLink.data_id_1];

                var image = pictures[ani.pictureId];

                g.DrawImageUnscaled(image.GetBitmap(), ani.x + aniLink.posX, ani.y + aniLink.posY);
            }

            // TODO: icons?
            /*
            foreach (var icon in icons) {
                var image = pictures[icon.pictureId];
                g.DrawImageUnscaled(image.Image, icon.x, icon.y);
            }
            */

            return bmp;
        }

        private void Read_Map_Att_Data_1(BinaryReader reader, int iconMax, int iconCount) {
            if(!Helper.ReadCompressed_ex(reader, out var data)) {
                data = reader.ReadBytes(iconMax * 4 * 8);
            }

            var mem = new MemoryStream(data);
            var r = new BinaryReader(mem);

            icons = new Icon[iconCount];
            for(int i = 0; i < iconCount; i++) {
                icons[i] = new Icon {
                    id = r.ReadInt32(),
                    top = r.ReadInt32(),
                    left = r.ReadInt32(),
                    bottom = r.ReadInt32(),
                    right = r.ReadInt32(),
                    pictureId = r.ReadInt32(),
                    x = r.ReadInt32(),
                    y = r.ReadInt32(),
                };
            }
        }

        private void Read_Map_Att_Data_2(BinaryReader reader, int iconMax, int aniCount) {
            if(!Helper.ReadCompressed_ex(reader, out var data)) {
                data = reader.ReadBytes(iconMax * 4 * 13);
            }

            var mem = new MemoryStream(data);
            var r = new BinaryReader(mem);

            anis = new Ani[aniCount];
            for(int i = 0; i < aniCount; i++) {
                byte flags = r.ReadByte();
                r.ReadBytes(3); // padding
                int top = r.ReadInt32();
                int left = r.ReadInt32();
                int bottom = r.ReadInt32();
                int right = r.ReadInt32();
                int pictureId = r.ReadInt32();
                int x = r.ReadInt32();
                int y = r.ReadInt32();
                byte tickId = r.ReadByte();
                r.ReadBytes(3); // padding

                r.ReadInt32(); // always -1
                r.ReadInt16(); // always 0
                r.ReadInt32(); // always 0
                r.ReadInt16(); // always 0
                r.ReadInt32(); // always 0

                anis[i] = new Ani {
                    flags = flags,
                    top = top,
                    left = left,
                    bottom = bottom,
                    right = right,
                    pictureId = pictureId,
                    x = x,
                    y = y,
                    tickId = tickId
                };
            }
        }

        private void Read_Ani_Link(BinaryReader reader) {
            var count = reader.ReadInt32();

            if(Helper.ReadCompressed_ex(reader, out var data)) {
                reader = new BinaryReader(new MemoryStream(data));
            }

            aniLinks = new AniLink[count];
            for(int i = 0; i < count; i++) {
                var id = reader.ReadInt32(); // ani id

                var data_id_1 = reader.ReadInt32();
                var data_id_2 = reader.ReadInt32();
                var v3 = reader.ReadInt32();
                var posX = reader.ReadInt32();
                var posY = reader.ReadInt32();
                var v4 = reader.ReadInt16();
                var v5 = reader.ReadInt16();
                var v6 = reader.ReadInt32();
                var v7 = reader.ReadInt16();
                var v8 = reader.ReadInt16();

                var rest = reader.ReadBytes(30 * 4); // idk

                aniLinks[i] = new AniLink {
                    data_id_1 = data_id_1,
                    data_id_2 = data_id_2,
                    v3 = v3,
                    posX = posX,
                    posY = posY,
                    v4 = v4,
                    v5 = v5,
                    v6 = v6,
                    v7 = v7,
                    v8 = v8
                };
            }
        }

        private static void ReadAnimation(BinaryReader reader) {
            var count = reader.ReadInt32();

            for(int i = 0; i < count; i++) {
                var name = reader.ReadCString(80);

                var idk1 = reader.ReadInt32();
                var idk2 = reader.ReadInt32();
                var idk3 = reader.ReadInt32();
                var idk4 = reader.ReadInt32();
                var idk5 = reader.ReadInt32();
                var idk6 = reader.ReadInt16();
            }
        }

        private static void ReadManLibary(BinaryReader reader) {
            var count = reader.ReadInt32();

            for(int i = 0; i < count; i++) {
                var id = reader.ReadInt32();

                var shorts = new short[104];
                for(int j = 0; j < 104; j++) {
                    shorts[j] = reader.ReadInt16();
                }

                reader.ReadInt16();
                reader.ReadInt16();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();

                for(int j = 0; j < 30; j++)
                    reader.ReadInt16();

                for(int j = 0; j < 11; j++)
                    reader.ReadInt32();

                reader.ReadInt16();
                reader.ReadInt16();

                reader.ReadInt32();
                reader.ReadInt32();

                var v2 = reader.ReadBytes(120);
            }
        }

        private void ReadAttribute(BinaryReader reader) {
            if(Helper.ReadCompressed_ex(reader, out var data)) {
                reader = new BinaryReader(new MemoryStream(data));
            }

            collisionMap = new short[baseWidth * 20, baseHeight * 30, 2];
            for(int y = 0; y < baseHeight * 30; y++) {
                for(int x = 0; x < baseWidth * 20; x++) {
                    collisionMap[x, y, 0] = reader.ReadInt16();
                    collisionMap[x, y, 1] = reader.ReadInt16();
                }
            }
        }

        private void ReadPicture(BinaryReader reader) {
            var count = reader.ReadInt32();

            pictures = new AniFrame[count];

            for(int i = 0; i < count; i++) {
                pictures[i] = Extractor.Ani.ReadFrame(reader);
            }
        }
    }
}