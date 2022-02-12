namespace Extractor {
    struct MobAtt {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int LootTable { get; set; }
        public int Hp { get; set; }

        public string File { get; set; }

        public static MobAtt[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new MobAtt[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                items[i] = new MobAtt {
                    Id = contents.Items[i, 0],
                    Name = contents.GetString(i, 1),
                    Level = contents.Items[i, 2],
                    Hp = contents.Items[i, 3],
                    LootTable = contents.Items[i, 9],
                    File = contents.GetString(i, 14),
                    // Range = contents.Items[i, 21],
                };
            }

            return items;
        }
    }
}