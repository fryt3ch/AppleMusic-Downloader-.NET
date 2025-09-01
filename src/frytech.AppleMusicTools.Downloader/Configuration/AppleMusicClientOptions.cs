using frytech.AppleMusicTools.Downloader.Core;

namespace frytech.AppleMusicTools.Downloader.Configuration;

/// <summary>
/// Options for <see cref="AppleMusicClient"/>.
/// </summary>
public sealed record AppleMusicClientOptions
{
    /// <summary>
    /// Url of Apple Music API.
    /// </summary>
    public string Url { get; init; } = "https://music.apple.com";
    
    /// <summary>
    /// API Token from Apple Music.
    /// </summary>
    /// <remarks>
    /// To get one - go to <see href="https://music.apple.com"/>, open console and execute: <code>MusicKit.getInstance().developerToken</code>
    /// </remarks>
    public required string ApiToken { get; init; }
    
    /// <summary>
    /// Media Token from Apple Music.
    /// </summary>
    /// <remarks>
    /// To get one - go to <see href="https://music.apple.com"/>, login to your account and grab cookie <c>media-user-token</c> value.
    /// </remarks>
    public required string MediaToken { get; init; }
}