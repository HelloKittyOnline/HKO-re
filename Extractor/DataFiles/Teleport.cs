namespace Extractor {
    public struct Teleport {
        public int Id { get; set; }
        public string name { get; set; }
        public string file { get; set; }

        public int FromMap { get; set; }
        public int fromX { get; set; }
        public int fromY { get; set; }

        public int toMap { get; set; }
        public int toX { get; set; }
        public int toY { get; set; }

        public int rotation { get; set; }

        public static Teleport[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new Teleport[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                items[i] = new Teleport {
                    Id = contents.Items[i, 0],
                    FromMap = contents.Items[i, 1],
                    fromX = contents.Items[i, 2],
                    fromY = contents.Items[i, 3],
                    toMap = contents.Items[i, 4],
                    toX = contents.Items[i, 5],
                    toY = contents.Items[i, 6],
                    name = contents.GetString(i, 7),
                    file = contents.GetString(i, 9),
                    rotation = contents.Items[i, 10],
                };
            }

            return items;
        }
    }
}