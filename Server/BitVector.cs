namespace Server;

internal class BitVector : IWriteAble {
    private byte[] Bytes { get; }

    public BitVector(int size) {
        Bytes = new byte[size];
    }

    public BitVector(byte[] bytes) {
        Bytes = bytes;
    }

    public bool this[int i] {
        get => (Bytes[i >> 3] & (1 << (i & 7))) != 0;
        set {
            if(value)
                Bytes[i >> 3] |= (byte)(1 << (i & 7));
            else
                Bytes[i >> 3] &= (byte)~(1 << (i & 7));
        }
    }

    public void Write(ref PacketBuilder b) => b.Write(Bytes);
}
