using System.Diagnostics;

namespace frytech.AppleMusicTools.Downloader.Core;

internal sealed class SongMuxer
{
    private readonly string _mp4BoxPath;

    public SongMuxer(string mp4BoxPath)
    {
        _mp4BoxPath = mp4BoxPath;
    }
    
    public async Task MuxSongFile(string inputFilePath, string outputFilePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _mp4BoxPath,
            ArgumentList = { "-add", inputFilePath, "-new", outputFilePath, },
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi)!;

        await proc.WaitForExitAsync();
    }
}