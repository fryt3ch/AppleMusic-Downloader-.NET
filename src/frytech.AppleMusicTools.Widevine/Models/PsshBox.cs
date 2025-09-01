namespace frytech.AppleMusicTools.Widevine.Models;

internal sealed class PsshBox
{
    private static readonly byte[] Header = "pssh"u8.ToArray();

    public List<byte[]> KIDs { get; }
    
    public byte[] Data { get; }

    private PsshBox(List<byte[]> kids, byte[] data)
    {
        KIDs = kids;
        Data = data;
    }

    public static PsshBox FromByteArray(byte[] psshBox)
    {
        using var stream = new MemoryStream(psshBox);

        stream.Seek(4, SeekOrigin.Current);
        var header = new byte[4];
        stream.ReadExactly(header, 0, 4);

        if (!header.SequenceEqual(Header))
            throw new Exception("Not a PSSH box");

        stream.Seek(20, SeekOrigin.Current);
        var kidCountBytes = new byte[4];
        stream.ReadExactly(kidCountBytes, 0, 4);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(kidCountBytes);
        
        var kidCount = BitConverter.ToUInt32(kidCountBytes);

        var kids = new List<byte[]>();
        for (var i = 0; i < kidCount; i++)
        {
            var kid = new byte[16];
            
            stream.ReadExactly(kid);
            kids.Add(kid);
        }

        var dataLengthBytes = new byte[4];
        stream.ReadExactly(dataLengthBytes);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(dataLengthBytes);
        
        var dataLength = BitConverter.ToUInt32(dataLengthBytes);

        if (dataLength == 0)
            return new PsshBox(kids, []);

        var data = new byte[dataLength];
        stream.ReadExactly(data);

        return new PsshBox(kids, data);
    }
}