using Org.BouncyCastle.Crypto;

namespace frytech.AppleMusicTools.Widevine.Core.Devices;

public abstract class WidevineDevice
{
    public ClientIdentification ClientId { get; }
    
    public AsymmetricCipherKeyPair Keys { get; }
    
    protected WidevineDevice(ClientIdentification clientId, AsymmetricCipherKeyPair keys)
    {
        ClientId = clientId;
        Keys = keys;
    }

    internal virtual byte[] GenerateSessionId()
    {
        var sessionId = new byte[16];
        
        var rand = new Random();
                
        rand.NextBytes(sessionId);

        return sessionId;
    }

    internal abstract byte[] Decrypt(byte[] data);
    
    internal abstract byte[] Sign(byte[] data);

    public static WidevineDevice CreateAndroid(byte[] clientIdBlobBytes, byte[] privateKeyBytes, byte[]? vmpBytes = null)
    {
        return new WidevineAndroidDevice(clientIdBlobBytes, privateKeyBytes, vmpBytes);
    }
}