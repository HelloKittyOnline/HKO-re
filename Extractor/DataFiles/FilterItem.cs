using System.Linq;
using System.Text;

namespace Extractor;

public struct FilterItem {
    public string Text { get; set; }
    public int Type { get; set; }

    public static FilterItem[] Load(byte[] contents) =>
        Encoding.UTF8.GetString(contents).Split("\r\n").Where(x => x != "").Select(x => x.Split("\t")).Select(x => new FilterItem {
            Text = x[0],
            Type = int.Parse(x[^1])
        }).ToArray();
}
