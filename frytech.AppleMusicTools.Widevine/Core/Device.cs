using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using ProtoBuf;

namespace frytech.AppleMusicTools.Widevine.Core;

public class Device
{
    public ClientIdentification ClientId { get; }
    
    public virtual bool IsAndroid => true;
    
    public AsymmetricCipherKeyPair Keys { get; }

    public Device(byte[] clientIdBlobBytes, byte[] privateKeyBytes, byte[]? vmpBytes = null)
        : this(clientIdBlobBytes, privateKeyBytes)
    {
        if (vmpBytes is not null)
            ClientId.FileHashes = Serializer.Deserialize<FileHashes>(new MemoryStream(vmpBytes));
    }
    
    public Device(byte[] clientIdBlobBytes, byte[] privateKeyBytes)
    {
        ClientId = Serializer.Deserialize<ClientIdentification>(new MemoryStream(clientIdBlobBytes));

        using var reader = new StringReader(Encoding.UTF8.GetString(privateKeyBytes));

        var pemObject = new PemReader(reader).ReadObject();

        if (pemObject is AsymmetricCipherKeyPair acKeys)
        {
            Keys = acKeys;
        }
        else if (pemObject is RsaPrivateCrtKeyParameters rsaPrivateKeys)
        {
            var rsaPublicKeys = new RsaKeyParameters(isPrivate: false, rsaPrivateKeys.Modulus, rsaPrivateKeys.PublicExponent);
                
            Keys = new AsymmetricCipherKeyPair(rsaPublicKeys, rsaPrivateKeys);
        }
        else
        {
            throw new InvalidOperationException("Invalid key format");
        }
    }

    internal virtual byte[] Decrypt(byte[] data)
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

    internal virtual byte[] Sign(byte[] data)
    {
        var eng = new PssSigner(new RsaEngine(), new Sha1Digest());

        eng.Init(true, Keys.Private);
        eng.BlockUpdate(data, 0, data.Length);
        
        return eng.GenerateSignature();
    }
}