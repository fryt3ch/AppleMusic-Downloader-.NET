using System.Text;
using frytech.AppleMusicTools.Widevine.Core;
using ProtoBuf;

namespace frytech.AppleMusicTools.Widevine;

public class CDM
{
    public Device Device { get; }
    
    private Session Session => _session ??= CreateSession();
    
    private Session? _session;
    
    private readonly string _initDataBase64;
    private readonly string? _certDataBase64;
    private readonly bool _offline;
    private readonly bool _raw;

    public CDM(Device device, string initDataBase64, string? certDataBase64 = null, bool offline = false, bool raw = false)
    {
        Device = device;
        
        _initDataBase64 = initDataBase64;
        _certDataBase64 = certDataBase64;
        _offline = offline;
        _raw = raw;
    }
    
    public CDM(byte[] clientIdBytes, byte[] privateKeyBytes, string initDataBase64, string? certDataBase64 = null, bool offline = false, bool raw = false)
        : this(new Device(clientIdBytes, privateKeyBytes), initDataBase64, certDataBase64, offline, raw)
    {
    }

    public byte[] GetChallenge()
    {
        return Session.GetLicenseRequest();
    }

    public void ProvideLicense(string licenseB64)
    {
        Session.ProvideLicense(Convert.FromBase64String(licenseB64));
    }

    public ContentKey[] GetKeys()
    {
        return Session.ContentKeys.ToArray();
    }

    private Session CreateSession()
    {
        var initData = CheckPssh(_initDataBase64);
        
        var sessionId = new byte[16];

        if (Device.IsAndroid)
        {
            var randHex = new StringBuilder();

            var rand = new Random();
            const string choice = "ABCDEF0123456789";
                
            for (var i = 0; i < 16; i++)
                randHex.Append(choice[rand.Next(16)]);

            const string counter = "01";
            const string rest = "00000000000000";
                
            sessionId = Encoding.ASCII.GetBytes(randHex + counter + rest);
        }
        else
        {
            var rand = new Random();
                
            rand.NextBytes(sessionId);
        }

        Session session;
            
        
        if (_raw)
        {
            session = new Session(sessionId, null, initData, Device, _offline);
        }
        else
        {
            var parsedInitData = ParseInitData(initData);
            
            session = new Session(sessionId, parsedInitData, null, Device, _offline);
        }
        
        if (!string.IsNullOrEmpty(_certDataBase64))
            session.SetServiceCertificate(Convert.FromBase64String(_certDataBase64));
        
        return session;
    }
    
    private static byte[] CheckPssh(string psshBase64)
    {
        if (psshBase64.Length % 4 != 0)
            psshBase64 = psshBase64.PadRight(psshBase64.Length + (4 - psshBase64.Length % 4), '=');

        var pssh = Convert.FromBase64String(psshBase64);

        if (pssh.Length < 30)
            return pssh;
        
        byte[] systemIdBytes = [237, 239, 139, 169, 121, 214, 74, 206, 163, 200, 39, 220, 213, 29, 33, 237];

        if (!pssh[12..28].SequenceEqual(systemIdBytes))
        {
            var newPssh = new List<byte>
            {
                0, 0, 0,
                (byte)(32 + pssh.Length)
            };
            newPssh.AddRange("pssh"u8.ToArray());
            newPssh.AddRange([0, 0, 0, 0]);
            newPssh.AddRange(systemIdBytes);
            newPssh.AddRange([0, 0, 0, 0]);
            newPssh[31] = (byte)pssh.Length;
            newPssh.AddRange(pssh);

            return newPssh.ToArray();
        }

        return pssh;
    }

    private static WidevineCencHeader ParseInitData(byte[] initData)
    {
        WidevineCencHeader cencHeader;

        try
        {
            cencHeader = Serializer.Deserialize<WidevineCencHeader>(new MemoryStream(initData[32..]));
        }
        catch
        {
            try
            {
                cencHeader = Serializer.Deserialize<WidevineCencHeader>(new MemoryStream(initData));
            }
            catch (Exception)
            {
                try
                {
                    //needed for HBO Max

                    var psshBox = PsshBox.FromByteArray(initData);
                    
                    cencHeader = Serializer.Deserialize<WidevineCencHeader>(new MemoryStream(psshBox.Data));
                }
                catch
                {
                    throw new InvalidOperationException();
                }
            }
        }

        return cencHeader;
    }
}