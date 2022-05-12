using Extractor;
using Resource = Extractor.Resource;

namespace Server {
    class MapData {
        public Teleport[] Teleporters { get; set; }
        public NPCName[] Npcs { get; set; }
        public Resource[] Resources { get; set; }
    }
}