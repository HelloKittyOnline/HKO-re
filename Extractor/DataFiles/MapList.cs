using System.Diagnostics;

namespace Extractor {
    public struct MapList {
        public int Id { get; set; }
        public string Name { get; set; }
        public string File { get; set; }
        public string Ost { get; set; }

        public static MapList[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new MapList[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                Debug.Assert(contents.Items[i, 0] == i);

                items[i] = new MapList {
                    Id = contents.Items[i, 0],
                    Name = contents.GetString(i, 6),
                    File = contents.GetString(i, 7),
                    Ost = contents.GetString(i, 8)
                };
            }

            return items;
        }
    }
}