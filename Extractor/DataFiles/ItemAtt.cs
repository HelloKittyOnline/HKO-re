namespace Extractor {
    struct ItemAtt {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int Price { get; set; }

        public string Icon { get; set; }
        public string Description { get; set; }
        public string CardImage { get; set; }

        public static ItemAtt[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new ItemAtt[contents.ItemCount - 1];
            for(int i = 1; i < contents.ItemCount; i++) {
                /*if(contents.Items[i, 1] == 0) { // no name
                    continue;
                }*/
                items[i - 1] = new ItemAtt {
                    Id = contents.Items[i, 0],
                    Name = contents.GetString(i, 1),
                    Type = contents.Items[i, 2],
                    Price = contents.Items[i, 5],
                    Icon = contents.GetString(i, 12),
                    Description = contents.GetString(i, 13),
                    CardImage = contents.GetString(i, 14),

                    // 21 always 0
                };
            }

            return items;
        }
    }
}