using System.Drawing;

namespace Extractor {
    struct T_TextItem {
        public string str;
        public int idk;
        public Color color;

        public static T_TextItem[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new T_TextItem[contents.ItemCount - 1];
            for(int i = 1; i < contents.ItemCount; i++) {
                var outInd = contents.Items[i, 0];
                var str = contents.GetString(i, 1); // todo: utf8
                var idk = contents.Items[i, 2];

                var r = contents.Items[i, 3];
                var g = contents.Items[i, 4];
                var b = contents.Items[i, 5];

                items[i - 1] = new T_TextItem {
                    str = str,
                    color = Color.FromArgb(r, g, b),
                    idk = idk
                };
            }

            return items;
        }
    }
}