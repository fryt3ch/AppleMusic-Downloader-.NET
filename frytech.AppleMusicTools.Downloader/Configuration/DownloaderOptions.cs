namespace frytech.AppleMusicTools.Downloader.Configuration;

public class DownloaderOptions
{
    public required string FfmpegPath { get; set; }

    public required string Mp4DecryptPath { get; set; }
    
    public required string Mp4TagPath { get; set; }
}