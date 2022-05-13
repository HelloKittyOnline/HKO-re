using System.Text.Json;
using Extractor;

namespace Server {
    class NpcData : NPCName {
        public int Action1 { get; set; }
        public int Action2 { get; set; }
        public int Action3 { get; set; }
        public int Action4 { get; set; }

        public static NpcData[] Load(string path) {
            return JsonSerializer.Deserialize<NpcData[]>(System.IO.File.ReadAllText(path));
        }
    }

    class MapData {
        public Teleport[] Teleporters { get; set; }
        public NpcData[] Npcs { get; set; }
        public Extractor.Resource[] Resources { get; set; }
    }
}