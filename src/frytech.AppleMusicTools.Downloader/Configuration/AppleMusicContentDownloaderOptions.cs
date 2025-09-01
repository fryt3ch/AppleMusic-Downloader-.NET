using frytech.AppleMusicTools.Downloader.Core;

namespace frytech.AppleMusicTools.Downloader.Configuration;

/// <summary>
/// Options for <see cref="AppleMusicContentDownloader"/>.
/// </summary>
public sealed record AppleMusicContentDownloaderOptions
{
    /// <summary>
    /// Path for <c>ffmpeg</c> executable.
    /// </summary>
    /// <seealso href="https://ffmpeg.org/"/>
    public required string FfmpegPath { get; init; }

    /// <summary>
    /// Path for <c>mp4decrypt</c> executable.
    /// </summary>
    /// <seealso href="https://www.bento4.com/documentation/mp4decrypt/"/>
    public required string Mp4DecryptPath { get; init; }
    
    /// <summary>
    /// Path for <c>mp4tag</c> executable.
    /// </summary>
    /// <seealso href="https://www.bento4.com/documentation/mp4tag/"/>
    public required string Mp4TagPath { get; init; }
}