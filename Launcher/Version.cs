using System.Text.Json;
using System.Text.Json.Nodes;

namespace Launcher;

class Version {
    public string Name;
    public string Url;

    public int Major;
    public int Minor;
    public int Patch;

    public Version(string name, string url, int major, int minor, int patch) {
        Name = name;
        Url = url;
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public static async Task<Version?> GetLatest(HttpClient client) {
        var res = await client.GetAsync("https://api.github.com/repos/HelloKittyOnline/HKO-re/releases/latest");
        if(!res.IsSuccessStatusCode) {
            return null;
        }

        var str = await res.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonNode>(str);

        var name = data["name"].GetValue<string>();
        var tag = data["tag_name"].GetValue<string>();
        var url = data["html_url"].GetValue<string>();

        var parts = tag[1..].Split('.');

        return new Version(name, url, int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    public static Version GetCurrent() {
        return new Version("", "", 1, 1, 1);
    }

    private int ToInt() {
        return Major * 1000 * 1000 + Minor * 1000 + Patch; 
    }

    public static bool operator >(Version a, Version b) {
        return a.ToInt() > b.ToInt();
    }

    public static bool operator <(Version a, Version b) {
        return a.ToInt() < b.ToInt();
    }
}
