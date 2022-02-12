namespace Extractor {
    public struct NPCName {
        public int Id { get; set; }
        public string Name { get; set; }
        public string File { get; set; }

        public int MapId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Rotation { get; set; }

        public static NPCName[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new NPCName[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                items[i] = new NPCName {
                    Id = contents.Items[i, 0],
                    Name = contents.GetString(i, 1),
                    File = contents.GetString(i, 2),
                    MapId = contents.Items[i, 3],
                    X = contents.Items[i, 4],
                    Y = contents.Items[i, 5],
                    Rotation = contents.Items[i, 6]
                };
            }

            return items;
        }
    }
}