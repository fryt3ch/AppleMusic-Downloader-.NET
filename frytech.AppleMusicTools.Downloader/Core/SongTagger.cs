using System.Diagnostics;

namespace frytech.AppleMusicTools.Downloader.Core;

internal sealed class SongTagger
{
    private readonly string _ffmpegPath;

    public SongTagger(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }
    
    public async Task MuxAndTagSongFile(string inputFilePath, string outputFilePath, IDictionary<string, string> tags, string? coverFilePath = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Input files
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(inputFilePath);

        if (!string.IsNullOrEmpty(coverFilePath))
        {
            psi.ArgumentList.Add("-i");
            psi.ArgumentList.Add(coverFilePath);
        }

        // Copy audio without re-encoding, no video from original
        psi.ArgumentList.Add("-vn");
        psi.ArgumentList.Add("-c:a");
        psi.ArgumentList.Add("copy");

        // Add metadata
        foreach (var kvp in tags)
        {
            psi.ArgumentList.Add("-metadata");
            psi.ArgumentList.Add($"{kvp.Key}={kvp.Value}");
        }

        // If cover art, map it and set disposition
        if (!string.IsNullOrEmpty(coverFilePath))
        {
            psi.ArgumentList.Add("-map");
            psi.ArgumentList.Add("0"); // audio
            psi.ArgumentList.Add("-map");
            psi.ArgumentList.Add("1"); // cover image
            psi.ArgumentList.Add("-disposition:v:0");
            psi.ArgumentList.Add("attached_pic");
        }
        
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("ipod");
        
        psi.ArgumentList.Add("-y");

        psi.ArgumentList.Add(outputFilePath);

        using var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync();
    }
}