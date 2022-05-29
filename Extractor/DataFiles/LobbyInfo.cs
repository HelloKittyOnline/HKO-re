namespace Extractor {
    [SeanItem(3)]
    class LobbyInfo {
        [SeanField(0)] public int Id { get; set; }
        [SeanField(1)] public string Address { get; set; }
        [SeanField(2)] public int Port { get; set; }
    }
}