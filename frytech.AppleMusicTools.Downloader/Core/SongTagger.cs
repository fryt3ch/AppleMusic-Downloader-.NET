using System.Diagnostics;

namespace frytech.AppleMusicTools.Downloader.Core;

internal sealed class SongTagger
{
    private readonly string _mp4BoxPath;

    public SongTagger(string mp4BoxPath)
    {
        _mp4BoxPath = mp4BoxPath;
    }
    
    public async Task TagSongFile(string filePath, IDictionary<string, string> tags)
    {
        foreach (var kvp in tags)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _mp4BoxPath,
                ArgumentList = { filePath, "-itags", $"{kvp.Key}={kvp.Value}", },
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            
            using var proc = Process.Start(psi)!;

            await proc.WaitForExitAsync();
        }
    }
}