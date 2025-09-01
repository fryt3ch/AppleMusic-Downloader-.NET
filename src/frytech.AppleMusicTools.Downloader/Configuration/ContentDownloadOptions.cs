using System.ComponentModel;

namespace frytech.AppleMusicTools.Downloader.Configuration;

/// <summary>
/// Options for downloading content.
/// </summary>
public sealed record ContentDownloadOptions
{
    /// <summary>
    /// Should include artwork (cover) in the content?
    /// </summary>
    [DefaultValue(true)]
    public bool IncludeArtwork { get; init; } = true;
}