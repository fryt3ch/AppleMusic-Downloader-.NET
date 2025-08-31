using System.Diagnostics;

namespace frytech.AppleMusicTools.Downloader.Core;

internal class SongDecrypter
{
    private readonly string _mp4decryptPath;

    public SongDecrypter(string mp4decryptPath)
    {
        _mp4decryptPath = mp4decryptPath;
    }
    
    public async Task DecryptSongFile(string inputFilePath, string outputFilePath, string decryptKey)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _mp4decryptPath,
            ArgumentList = { "--key", decryptKey, inputFilePath, outputFilePath, },
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi)!;

        await proc.WaitForExitAsync();
    }
}