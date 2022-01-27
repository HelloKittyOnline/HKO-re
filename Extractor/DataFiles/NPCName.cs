namespace Extractor {
    public struct NPCName {
        public int Id;
        public string name;
        public string file;

        public int map;
        public int x;
        public int y;
        public int r;

        public static NPCName[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new NPCName[contents.ItemCount - 1];
            for(int i = 1; i < contents.ItemCount; i++) {
                items[i - 1] = new NPCName {
                    Id = contents.Items[i, 0],
                    name = contents.GetString(i, 1),
                    file = contents.GetString(i, 2),
                    map = contents.Items[i, 3],
                    x = contents.Items[i, 4],
                    y = contents.Items[i, 5],
                    r = contents.Items[i, 6],
                };
            }

            return items;
        }
    }
}