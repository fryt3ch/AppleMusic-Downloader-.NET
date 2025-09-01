using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using ProtoBuf;

namespace frytech.AppleMusicTools.Widevine.Core.Devices;

internal sealed class WidevineAndroidDevice : WidevineDevice
{
    public WidevineAndroidDevice(byte[] clientIdBlobBytes, byte[] privateKeyBytes, byte[]? vmpBytes = null)
        : base(LoadClientId(clientIdBlobBytes, vmpBytes), LoadPrivateKey(privateKeyBytes))
    {
    }

    internal override byte[] GenerateSessionId()
    {
        var randHex = new StringBuilder();

        var rand = new Random();
        const string choice = "ABCDEF0123456789";
                
        for (var i = 0; i < 16; i++)
            randHex.Append(choice[rand.Next(16)]);

        const string counter = "01";
        const string rest = "00000000000000";
                
        return Encoding.ASCII.GetBytes(randHex + counter + rest);
    }

    internal override byte[] Decrypt(byte[] data)
    {
        var eng = new OaepEncoding(new RsaEngine());
        
        eng.Init(false, Keys.Private);

        var length = data.Length;
        var blockSize = eng.GetInputBlockSize();

        var plainText = new List<byte>();

        for (var chunkPosition = 0; chunkPosition < length; chunkPosition += blockSize)
        {
            var chunkSize = Math.Min(blockSize, length - chunkPosition);
            
            plainText.AddRange(eng.ProcessBlock(data, chunkPosition, chunkSize));
        }

        return plainText.ToArray();
    }

    internal override byte[] Sign(byte[] data)
    {
        var eng = new PssSigner(new RsaEngine(), new Sha1Digest());

        eng.Init(true, Keys.Private);
        eng.BlockUpdate(data, 0, data.Length);
        
        return eng.GenerateSignature();
    }
    
    private static ClientIdentification LoadClientId(byte[] clientIdBlobBytes, byte[]? vmpBytes)
    {
        using var clientIdMs = new MemoryStream(clientIdBlobBytes);
        var clientId = Serializer.Deserialize<ClientIdentification>(clientIdMs);

        if (vmpBytes is not null)
        {
            using var vmpBytesMs = new MemoryStream(vmpBytes);
            clientId.FileHashes = Serializer.Deserialize<FileHashes>(vmpBytesMs);
        }

        return clientId;
    }
    
    private static AsymmetricCipherKeyPair LoadPrivateKey(byte[] privateKeyBytes)
    {
        using var reader = new StringReader(Encoding.UTF8.GetString(privateKeyBytes));
        var pemObject = new PemReader(reader).ReadObject();

        return pemObject switch
        {
            AsymmetricCipherKeyPair acKeys => acKeys,
            RsaPrivateCrtKeyParameters rsaPrivateKeys => new AsymmetricCipherKeyPair(
                new RsaKeyParameters(false, rsaPrivateKeys.Modulus, rsaPrivateKeys.PublicExponent),
                rsaPrivateKeys
            ),
            _ => throw new InvalidOperationException("Invalid key format")
        };
    }
}