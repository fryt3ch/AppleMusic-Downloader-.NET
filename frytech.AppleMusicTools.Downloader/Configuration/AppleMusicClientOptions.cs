namespace frytech.AppleMusicTools.Downloader.Configuration;

public class AppleMusicClientOptions
{
    public string Url { get; set; } = "https://music.apple.com";
    
    public required string ApiToken { get; set; }
    
    public required string MediaToken { get; set; }
}