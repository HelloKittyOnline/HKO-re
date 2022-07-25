namespace Server.Protocols {
    static class Achievement {
        public static void Handle(Client client) {
            var id = client.ReadByte();
            switch(id) {
                default:
                    client.LogUnknown(0x1B, id);
                    break;
            }
        }

        static void Send01(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x1B); // first switch
            b.WriteByte(0x01); // second switch

            b.WriteInt(0);
            b.WriteInt(0);

            b.Send(client);
        }

        static void Send03(Client client) {
            var b = new PacketBuilder();

            b.WriteByte(0x1B); // first switch
            b.WriteByte(0x03); // second switch

            b.WriteByte(0);

            b.Send(client);
        }

        enum Title {
            Challenger = 1,
            Rival = 2,
            Master_Farmer = 3,
            Master_Harvester = 4,
            The_Super_Rich = 5,
            The_Mole_nator = 6,
            The_Monster_Wrangler = 7,
            The_Pet_Collector = 8,
            The_Pet_Card_Connoisseur = 9,
            The_Stable_Master = 10,
            The_Serious = 11,
            The_Big_Spender = 12,
            The_Loyal = 13,
            The_Frugal = 14,
            The_Non_Fighter = 15,
        }

        static void SendSetTitle(Client client, int other, Title title) {
            var b = new PacketBuilder();

            b.WriteByte(0x1B); // first switch
            b.WriteByte(0x04); // second switch

            b.WriteInt(other);
            b.WriteByte((byte)title);

            b.Send(client);
        }
    }
}