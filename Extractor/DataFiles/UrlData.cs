namespace Extractor {
    struct UrlData {
        public int Id { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
        public string Name { get; set; }

        public static UrlData[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new UrlData[contents.ItemCount];
            for(int i = 0; i < contents.ItemCount; i++) {
                items[i] = new UrlData {
                    Id = contents.Items[i, 0],
                    Path = contents.GetString(i, 1),
                    Hash = contents.GetString(i, 2),
                    Name = contents.GetString(i, 3),
                };
            }

            return items;
        }
    }
}