using System.Diagnostics;

namespace frytech.AppleMusicTools.Downloader.Core;

internal sealed class SongMuxer
{
    private readonly string _ffmpegPath;

    public SongMuxer(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }
    
    public async Task MuxSongFile(string inputFilePath, string outputFilePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(inputFilePath);

        // Copy audio without re-encoding, no video from original
        psi.ArgumentList.Add("-vn");
        psi.ArgumentList.Add("-c:a");
        psi.ArgumentList.Add("copy");
        
        // M4A output format
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("ipod");
        
        psi.ArgumentList.Add("-y");

        psi.ArgumentList.Add(outputFilePath);

        using var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync();
    }
}