namespace Extractor {
    public struct Teleport {
        public int Id { get; set; }
        public string name { get; set; }
        public string file { get; set; }

        public int fromMap { get; set; }
        public int fromX { get; set; }
        public int fromY { get; set; }

        public int toMap { get; set; }
        public int toX { get; set; }
        public int toY { get; set; }

        public int rotation { get; set; }
        // public int[] data { get; set; }

        public static Teleport[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new Teleport[contents.ItemCount - 1];
            for(int i = 1; i < contents.ItemCount; i++) {
                /*var dat = new int[contents.ItemSize];
                for(int j = 0; j < contents.ItemSize; j++) {
                    dat[j] = contents.Items[i, j];
                }*/

                items[i - 1] = new Teleport {
                    Id = contents.Items[i, 0],
                    fromMap = contents.Items[i, 1],
                    fromX = contents.Items[i, 2],
                    fromY = contents.Items[i, 3],
                    toMap = contents.Items[i, 4],
                    toX = contents.Items[i, 5],
                    toY = contents.Items[i, 6],
                    name = contents.GetString(i, 7),
                    file = contents.GetString(i, 9),
                    rotation = contents.Items[i, 10],
                    // data = dat
                };
            }

            return items;
        }
    }
}