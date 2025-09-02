# Apple Music Downloader .NET Library

The **Apple Music Downloader** is a .NET library designed for educational purposes, allowing developers to integrate Apple Music content downloading capabilities into their applications. It uses the Widevine Content Decryption Module (CDM) to download songs, with potential for future expansion to other content types.

> **Disclaimer**: This library is intended for **educational purposes only**. Ensure compliance with Apple Music's terms of service and applicable laws when using this library.

## Features
- Download songs from Apple Music using Widevine CDM.
- Supports metadata tagging and album artwork inclusion (configurable).
- Integrates with external tools for decryption, tagging, and audio muxing.
- Currently supports song downloads, with potential for expansion to other media types.

## Prerequisites

### External Tools
The following tools are required for the library to function:
- **Bento4 SDK** ([GitHub](https://github.com/axiomatic-systems/Bento4)):
  - `mp4decrypt`: Decrypts protected media content.
  - `mp4tag`: Tags the output file with metadata and album artwork (configurable).
- **FFmpeg** ([ffmpeg.org](https://ffmpeg.org)): Muxes decrypted song files into the `.m4a` format.
- Ensure these tools are installed and accessible via your system's PATH or configured paths in the application settings.

### Apple Music Tokens
You need an **API Token** and **Media Token** to authenticate with Apple Music. Refer to the [Apple Music Tokens Guide](docs/APPLE_MUSIC_TOKENS_README.md) for instructions on obtaining these tokens.

### Widevine Keys
Widevine device keys (`client_id` and `private_key`) are required for content decryption. See the [Widevine Key Dumping Guide](docs/WIDEVINE_README.md) for details on acquiring these keys.

## Installation

1. Clone or download the repository containing the Apple Music Downloader library.
2. Add the library to your .NET project:
   - **NuGet**: [Ready package](https://www.nuget.org/packages/frytech.AppleMusicTools.Downloader/)
   - **Project Reference**: Add the library projects to your solution and reference it.

## Configuration

Configure the library with Apple Music tokens, Widevine keys, and external tool paths e.g. in `appsettings.json`:

```json
{
  "AppleMusic": {
    "ApiToken": "your_api_token_here",
    "MediaToken": "your_media_token_here"
  },
  "AppleMusicDownloader": {
    "DeviceClientIdFilePath": "path/to/client_id.bin",
    "DevicePrivateKeyFilePath": "path/to/private_key.pem",
    "FfmpegPath": "path/to/ffmpeg",
    "Mp4DecryptPath": "path/to/mp4decrypt",
    "Mp4TagPath": "path/to/mp4tag"
  }
}
```

## Usage

The library provides core components (`AppleMusicClient`, `WidevineDevice`, `AppleMusicContentDownloader`) for downloading Apple Music songs. Below is an example of integrating it into your application.

### Example: Setting Up Services

Configure the dependency injection container to include the library's services:

```csharp
var builder = HostApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Bind and validate application settings
services.AddOptions<AppSettings>()
    .Bind(configuration)
    .ValidateOnStart();

// Configure Downlaoder HTTP client
services.AddHttpClient("Downloader");

// Configure AppleMusicClient
services.AddSingleton<AppleMusicClient>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    return new AppleMusicClient(httpClientFactory.CreateClient("Downloader"), new AppleMusicClientOptions()
    {
        ApiToken = settings.AppleMusic.ApiToken,
        MediaToken = settings.AppleMusic.MediaToken
    });
});

// Configure WidevineDevice (Android one)
services.AddSingleton<WidevineDevice>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;
    var clientIdBytes = File.ReadAllBytes(settings.AppleMusicDownloader.DeviceClientIdFilePath);
    var privateKeyBytes = File.ReadAllBytes(settings.AppleMusicDownloader.DevicePrivateKeyFilePath);

    return WidevineDevice.CreateAndroid(clientIdBytes, privateKeyBytes);
});

// Configure AppleMusicContentDownloader
services.AddSingleton(provider =>
{
    var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;
    var client = provider.GetRequiredService<AppleMusicClient>();
    var device = provider.GetRequiredService<WidevineDevice>();

    return new AppleMusicContentDownloader(client, device, new AppleMusicContentDownloaderOptions()
    {
        FfmpegPath = settings.AppleMusicDownloader.FfmpegPath,
        Mp4DecryptPath = settings.AppleMusicDownloader.Mp4DecryptPath,
        Mp4TagPath = settings.AppleMusicDownloader.Mp4TagPath
    });
});

var app = builder.Build();
app.Run();
```

### Example: Downloading a Song

```csharp
var downloader = services.GetRequiredService<AppleMusicContentDownloader>();

var artistName = "Aerosmith";
var songName = "Dream On";
var assetId = "1442858650";

var fileName = $"{artistName} - {songName}.m4a";

// Download the song and get the file stream
var songFile = await _downloader.DownloadContent(assetId, new ContentDownloadOptions()
{
    IncludeArtwork = true, // Saves cover to song file
});

// Save the file to disk
using (var fileStream = File.Create(fileName))
    await songFile.CopyToAsync(fileStream);

Console.WriteLine($"Song downloaded: {fileName}");
```

In this example we download song ``https://music.apple.com/kz/song/dream-on/1442858650`` using `AppleMusicContentDownloader`.

## Troubleshooting
- **Missing Tools**: Verify that `mp4decrypt`, `mp4tag`, and `FFmpeg` are installed and their paths are correctly configured.
- **Authentication Errors**: Ensure valid Apple Music tokens; regenerate if expired (see [Apple Music Tokens Guide](docs/APPLE_MUSIC_TOKENS_README.md)).
- **Widevine Errors**: Confirm that Widevine keys are valid (see [Widevine Key Dumping Guide](docs/WIDEVINE_README.md)).
- **File Not Found**: Check that paths to Widevine keys and external tools are accessible.

## License
This library is provided for **educational purposes only**. The developers are not responsible for misuse or violations of Apple Music's terms of service. Use at your own risk and ensure compliance with all applicable laws.
