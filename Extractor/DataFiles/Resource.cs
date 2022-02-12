namespace Extractor {
    public struct Resource {
        public int Id { get; set; }
        public int MapId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public short NameId { get; set; }
        public short Count { get; set; }
        // public byte Rotation { get; set; }

        public int Tool1 { get; set; }
        public int Tool2 { get; set; }

        public byte Type1 { get; set; }
        public byte Type2 { get; set; }

        // public int HarvestTime { get; set; }
        public int LootTable { get; set; }

        public static Resource[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new Resource[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                items[i] = new Resource {
                    Id = contents.Items[i, 0],
                    MapId = contents.Items[i, 1],
                    X = contents.Items[i, 2],
                    Y = contents.Items[i, 3],
                    NameId = (short)contents.Items[i, 4],
                    Count = (short)contents.Items[i, 5],
                    LootTable = contents.Items[i, 6],
                    Tool1 = (byte)contents.Items[i, 7],
                    Tool2 = (byte)contents.Items[i, 8],
                    Type1 = (byte)contents.Items[i, 9],
                    Type2 = (byte)contents.Items[i, 13]
                };
            }

            return items;
        }
    }
}