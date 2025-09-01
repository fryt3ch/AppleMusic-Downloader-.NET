using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace frytech.AppleMusicTools.Widevine.Utils;

internal static class CryptoUtils
{
    public static byte[] GetHMACSHA256Digest(byte[] data, byte[] key)
    {
        return new HMACSHA256(key).ComputeHash(data);
    }

    public static byte[] GetCMACDigest(byte[] data, byte[] key)
    {
        var cipher = new AesEngine();
        var mac = new CMac(cipher, 128);

        var keyParam = new KeyParameter(key);

        mac.Init(keyParam);
        mac.BlockUpdate(data, 0, data.Length);

        var outBytes = new byte[16];

        mac.DoFinal(outBytes, 0);
        
        return outBytes;
    }
    
    public static byte[] AddPKCS7Padding(byte[] data, int k)
    {
        var m = k - data.Length % k;

        var padding = new byte[m];
        Array.Fill(padding, (byte)m);

        var paddedBytes = new byte[data.Length + padding.Length];
        Buffer.BlockCopy(data, 0, paddedBytes, 0, data.Length);
        Buffer.BlockCopy(padding, 0, paddedBytes, data.Length, padding.Length);

        return paddedBytes;
    }
}