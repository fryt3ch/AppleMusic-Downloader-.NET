using System.Diagnostics;

namespace frytech.AppleMusicTools.Downloader.Core;

internal sealed class SongTagger
{
    private readonly string _mp4TagPath;

    public SongTagger(string mp4tagPath)
    {
        _mp4TagPath = mp4tagPath;
    }
    
    public async Task TagSongFile(string inputFilePath, string outputFilePath, IDictionary<string, string> tags)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _mp4TagPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var tag in tags)
        {
            psi.ArgumentList.Add("--set");
            psi.ArgumentList.Add(tag.Key + $":{tag.Value}");
        }

        psi.ArgumentList.Add(inputFilePath);
        psi.ArgumentList.Add(outputFilePath);

        using var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync();
    }
}