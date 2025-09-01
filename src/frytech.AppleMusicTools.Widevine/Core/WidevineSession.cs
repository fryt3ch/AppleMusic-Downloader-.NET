using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using frytech.AppleMusicTools.Widevine.Core.Devices;
using frytech.AppleMusicTools.Widevine.Models;
using frytech.AppleMusicTools.Widevine.Utils;
using ProtoBuf;

namespace frytech.AppleMusicTools.Widevine.Core;

internal sealed class WidevineSession
{
    public byte[] SessionId { get; set; }
    
    public byte[]? InitData { get; set; }
    
    public WidevineCencHeader? ParsedInitData { get; set; }
    
    public bool Offline { get; set; }
    
    public WidevineDevice Device { get; set; }
    
    public byte[] SessionKey { get; set; }
    
    public DerivedKeys DerivedKeys { get; set; }
    
    public byte[] LicenseRequestBytes { get; set; }
    
    public SignedLicense License { get; set; }
    
    public SignedDeviceCertificate ServiceCertificate { get; set; }
    
    public bool PrivacyMode { get; set; }
    
    public List<ContentKey> ContentKeys { get; set; } = [];

    public WidevineSession(byte[] sessionId, WidevineCencHeader? parsedInitData, byte[]? initData, WidevineDevice device, bool offline)
    {
        SessionId = sessionId;
        InitData = initData;
        ParsedInitData = parsedInitData;
        Offline = offline;
        Device = device;
    }
    
    public byte[] GetLicenseRequest()
    {
        dynamic licenseRequest;
        var requestTime = uint.Parse((DateTime.Now - DateTime.UnixEpoch).TotalSeconds.ToString(CultureInfo.InvariantCulture)
            .Split(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)[0]);

        if (ParsedInitData is not null)
        {
            licenseRequest = new SignedLicenseRequest
            {
                Type = SignedLicenseRequest.MessageType.LicenseRequest,
                Msg = new LicenseRequest
                {
                    Type = LicenseRequest.RequestType.New,
                    KeyControlNonce = 1093602366,
                    ProtocolVersion = ProtocolVersion.Current,
                    RequestTime = requestTime,
                    ContentId = new LicenseRequest.ContentIdentification
                    {
                        CencId = new LicenseRequest.ContentIdentification.Cenc
                        {
                            LicenseType = Offline ? LicenseType.Offline : LicenseType.Default,
                            RequestId = SessionId,
                            Pssh = ParsedInitData
                        }
                    }
                }
            };
        }
        else
        {
            licenseRequest = new SignedLicenseRequestRaw
            {
                Type = SignedLicenseRequestRaw.MessageType.LicenseRequest,
                Msg = new LicenseRequestRaw
                {
                    Type = LicenseRequestRaw.RequestType.New,
                    KeyControlNonce = 1093602366,
                    ProtocolVersion = ProtocolVersion.Current,
                    RequestTime = requestTime,
                    ContentId = new LicenseRequestRaw.ContentIdentification
                    {
                        CencId = new LicenseRequestRaw.ContentIdentification.Cenc
                        {
                            LicenseType = Offline ? LicenseType.Offline : LicenseType.Default,
                            RequestId = SessionId,
                            Pssh = InitData
                        }
                    }
                }
            };
        }

        if (PrivacyMode)
        {
            var encryptedClientIdProto = new EncryptedClientIdentification();

            using var clientIdStream = new MemoryStream();
            Serializer.Serialize(clientIdStream, Device.ClientId);
            var data = CryptoUtils.AddPKCS7Padding(clientIdStream.ToArray(), 16);

            var aes = Aes.Create();
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(aes.Key, aes.IV), CryptoStreamMode.Write);
            cryptoStream.Write(data, 0, data.Length);
            encryptedClientIdProto.EncryptedClientId = memoryStream.ToArray();

            using var rsa = new RSACryptoServiceProvider();
            rsa.ImportRSAPublicKey(ServiceCertificate.DeviceCertificate.PublicKey, out _);
            encryptedClientIdProto.EncryptedPrivacyKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA1);
            encryptedClientIdProto.EncryptedClientIdIv = aes.IV;
            encryptedClientIdProto.ServiceId = Encoding.UTF8.GetString(ServiceCertificate.DeviceCertificate.ServiceId);
            encryptedClientIdProto.ServiceCertificateSerialNumber = ServiceCertificate.DeviceCertificate.SerialNumber;

            licenseRequest.Msg.EncryptedClientId = encryptedClientIdProto;
        }
        else
        {
            licenseRequest.Msg.ClientId = Device.ClientId;
        }
        
        using (var memoryStream = new MemoryStream())
        {
            Serializer.Serialize(memoryStream, licenseRequest.Msg);
            var data = memoryStream.ToArray();
            LicenseRequestBytes = data;

            licenseRequest.Signature = Device.Sign(data);
        }

        byte[] requestBytes;
        using (var memoryStream = new MemoryStream())
        {
            Serializer.Serialize(memoryStream, licenseRequest);
            requestBytes = memoryStream.ToArray();
        }
        
        return requestBytes;
    }
    
    public void ProvideLicense(byte[] license)
    {
        if (LicenseRequestBytes is null)
            throw new Exception("Generate a license request first");

        if (ContentKeys.Any())
            return;

        License = Serializer.Deserialize<SignedLicense>(new MemoryStream(license));

        try
        {
            var sessionKey = Device.Decrypt(License.SessionKey);

            if (sessionKey.Length != 16)
                throw new InvalidOperationException("Unable to decrypt session key");

            SessionKey = sessionKey;
        }
        catch
        {
            throw new InvalidOperationException("Unable to decrypt session key");
        }
        
        DerivedKeys = DeriveKeys(LicenseRequestBytes, SessionKey);

        byte[] licenseBytes;
        using (var memoryStream = new MemoryStream())
        {
            Serializer.Serialize(memoryStream, License.Msg);
            licenseBytes = memoryStream.ToArray();
        }
        
        var hmacHash = CryptoUtils.GetHMACSHA256Digest(licenseBytes, DerivedKeys.Auth1);

        if (!hmacHash.SequenceEqual(License.Signature))
            throw new InvalidOperationException("License signature mismatch");

        foreach (var key in License.Msg.Keys)
        {
            var type = key.Type.ToString();

            if (type == "Signing")
                continue;

            var keyId = key.Id ?? Encoding.ASCII.GetBytes(key.Type.ToString());
            var encryptedKey = key.Key;
            var iv = key.Iv;
            
            using var memoryStream = new MemoryStream();
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(DerivedKeys.Enc, iv), CryptoStreamMode.Write);
            cryptoStream.Write(encryptedKey, 0, encryptedKey.Length);
            var decryptedKey = memoryStream.ToArray();

            var permissions = new List<string>();
            
            if (type == "OperatorSession")
            {
                foreach (var perm in key._OperatorSessionKeyPermissions.GetType().GetProperties())
                {
                    if ((uint)perm.GetValue(key._OperatorSessionKeyPermissions)! == 1)
                        permissions.Add(perm.Name);
                }
            }
            
            ContentKeys.Add(new ContentKey
            {
                KeyId = keyId,
                Type = type,
                Bytes = decryptedKey,
                Permissions = permissions
            });
        }
    }
    
    public bool TrySetServiceCertificate(byte[] certData)
    {
        SignedMessage signedMessage;

        try
        {
            signedMessage = Serializer.Deserialize<SignedMessage>(new MemoryStream(certData));
        }
        catch
        {
            signedMessage = new SignedMessage();
        }

        SignedDeviceCertificate serviceCertificate;
        
        try
        {
            try
            {
                serviceCertificate = Serializer.Deserialize<SignedDeviceCertificate>(new MemoryStream(signedMessage.Msg));
            }
            catch
            {
                serviceCertificate = Serializer.Deserialize<SignedDeviceCertificate>(new MemoryStream(certData));
            }
        }
        catch
        {
            return false;
        }

        ServiceCertificate = serviceCertificate;
        PrivacyMode = true;

        return true;
    }
    
    private static DerivedKeys DeriveKeys(byte[] message, byte[] key)
    {
        var encKeyBase = "ENCRYPTION"u8.ToArray().Concat(new byte[] { 0x0, }).Concat(message).Concat(new byte[] { 0x0, 0x0, 0x0, 0x80 }).ToArray();
        var authKeyBase = "AUTHENTICATION"u8.ToArray().Concat(new byte[] { 0x0, }).Concat(message).Concat(new byte[] { 0x0, 0x0, 0x2, 0x0 }).ToArray();

        var encKey = new byte[] { 0x01 }.Concat(encKeyBase).ToArray();
        var authKey1 = new byte[] { 0x01 }.Concat(authKeyBase).ToArray();
        var authKey2 = new byte[] { 0x02 }.Concat(authKeyBase).ToArray();
        var authKey3 = new byte[] { 0x03 }.Concat(authKeyBase).ToArray();
        var authKey4 = new byte[] { 0x04 }.Concat(authKeyBase).ToArray();

        var encCmacKey = CryptoUtils.GetCMACDigest(encKey, key);
        var authCmacKey1 = CryptoUtils.GetCMACDigest(authKey1, key);
        var authCmacKey2 = CryptoUtils.GetCMACDigest(authKey2, key);
        var authCmacKey3 = CryptoUtils.GetCMACDigest(authKey3, key);
        var authCmacKey4 = CryptoUtils.GetCMACDigest(authKey4, key);

        var authCmacCombined1 = authCmacKey1.Concat(authCmacKey2).ToArray();
        var authCmacCombined2 = authCmacKey3.Concat(authCmacKey4).ToArray();

        return new DerivedKeys
        {
            Auth1 = authCmacCombined1,
            Auth2 = authCmacCombined2,
            Enc = encCmacKey
        };
    }
}