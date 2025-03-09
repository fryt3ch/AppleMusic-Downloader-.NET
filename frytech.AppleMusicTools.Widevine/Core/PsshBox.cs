namespace frytech.AppleMusicTools.Widevine.Core;

internal class PsshBox
{
    private static readonly byte[] Header = new byte[] { 0x70, 0x73, 0x73, 0x68 };

    public List<byte[]> KIDs { get; set; } = new List<byte[]>();
    public byte[] Data { get; set; }

    PsshBox(List<byte[]> kids, byte[] data)
    {
        KIDs = kids;
        Data = data;
    }

    public static PsshBox FromByteArray(byte[] psshbox)
    {
        using var stream = new MemoryStream(psshbox);

        stream.Seek(4, SeekOrigin.Current);
        byte[] header = new byte[4];
        stream.Read(header, 0, 4);

        if (!header.SequenceEqual(Header))
            throw new Exception("Not a PSSH box");

        stream.Seek(20, SeekOrigin.Current);
        byte[] kidCountBytes = new byte[4];
        stream.Read(kidCountBytes, 0, 4);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(kidCountBytes);
        uint kidCount = BitConverter.ToUInt32(kidCountBytes);

        List<byte[]> kids = new List<byte[]>();
        for (int i = 0; i < kidCount; i++)
        {
            byte[] kid = new byte[16];
            stream.Read(kid);
            kids.Add(kid);
        }

        byte[] dataLengthBytes = new byte[4];
        stream.Read(dataLengthBytes);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(dataLengthBytes);
        uint dataLength = BitConverter.ToUInt32(dataLengthBytes);

        if (dataLength == 0)
            return new PsshBox(kids, null);

        byte[] data = new byte[dataLength];
        stream.Read(data);

        return new PsshBox(kids, data);
    }
}