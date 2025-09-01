using System.Text;
using System.Text.Json;
using frytech.AppleMusicTools.Downloader.Configuration;
using frytech.AppleMusicTools.Downloader.Extensions;
using frytech.AppleMusicTools.Widevine;
using frytech.AppleMusicTools.Widevine.Core;

namespace frytech.AppleMusicTools.Downloader.Core;

public sealed class AppleMusicContentDownloader
{
    private readonly AppleMusicClient _appleMusicClient;
    private readonly Device _device;
    
    private readonly SongDecrypter _songDecrypter;
    private readonly SongMuxer _songMuxer;
    private readonly SongTagger _songTagger;

    public AppleMusicContentDownloader(AppleMusicClient appleMusicClient, Device device, AppleMusicContentDownloaderOptions options)
    {
        _appleMusicClient = appleMusicClient;
        _device = device;

        _songDecrypter = new SongDecrypter(options.Mp4DecryptPath);
        _songMuxer = new SongMuxer(options.FfmpegPath);
        _songTagger = new SongTagger(options.Mp4TagPath);
    }

    public async Task<MemoryStream> DownloadContent(string assetId, ContentDownloadOptions? options = null)
    {
        options ??= new ContentDownloadOptions();
        
        var webPlayback = await _appleMusicClient.GetWebPlayback(assetId);

        if (!webPlayback.TryGetProperty("songList", out var songListElement) || songListElement.GetArrayLength() == 0)
            throw new InvalidOperationException("No song was found!");

        var songElement = songListElement[0];
        
        if (songElement.TryGetProperty("hls-playlist-url", out _))
            throw new NotSupportedException("Videos are not supported!");
        
        return await DownloadSong(assetId, songElement, options);
    }

    private async Task<MemoryStream> DownloadSong(string assetId, JsonElement songElement, ContentDownloadOptions options)
    {
        var licenseUrl = songElement.GetProperty("hls-key-server-url").GetString()!;
        
        if (!songElement.TryGetProperty("assets", out var assets))
            throw new InvalidOperationException("Something went wrong!");
        
        var asset = assets.EnumerateArray()
            .Where(x => x.TryGetProperty("flavor", out var flavor) && flavor.GetString() == "28:ctrp256")
            .FirstOrDefault();

        if (asset.ValueKind is not JsonValueKind.Object)
            throw new InvalidOperationException("Something went wrong!");
        
        var assetsUrl = asset.GetProperty("URL").GetString()!;
        var metadata = asset.GetProperty("metadata")!;

        var assetStream = await _appleMusicClient.Client.GetStreamAsync(assetsUrl);
        
        var m3u8 = M3U8Parser.MediaPlaylist.LoadFromText(await assetStream.ReadAsStringAsync());
        
        var psshKeyUri = m3u8.MediaSegments.First().Key.Uri;

        var songDecryptKey = await GetSongDecryptKey(licenseUrl, assetId, psshKeyUri);
        
        var songDownloadUri = string.Join('/', assetsUrl.Split('/').SkipLast(1).Append(m3u8.Map.Uri));
        
        var songDownloadStream = await _appleMusicClient.Client.GetStreamAsync(songDownloadUri);

        var songEncryptedFilePath = Path.GetTempFileName();
        
        await songDownloadStream.WriteToFileAsync(songEncryptedFilePath);
        
        var songDecryptedFilePath = Path.GetTempFileName();
        
        await _songDecrypter.DecryptSongFile(songEncryptedFilePath, songDecryptedFilePath, songDecryptKey);
        
        File.Delete(songEncryptedFilePath);
        
        var songFilePath = Path.GetTempFileName();
        
        await _songMuxer.MuxSongFile(songDecryptedFilePath, songFilePath);
        
        File.Delete(songDecryptedFilePath);
        
        var taggedSongFilePath = Path.GetTempFileName();

        var artworkPath = options.IncludeArtwork ? await GetArtworkPath(asset) : null;
        var tagsDictionary = CreateTagsDictionary(metadata, artworkPath);
        
        await _songTagger.TagSongFile(songFilePath, taggedSongFilePath, tagsDictionary);
        
        if (artworkPath is not null)
            File.Delete(artworkPath);
        
        File.Delete(songFilePath);

        songFilePath = taggedSongFilePath;
        
        var finalSongStream = new MemoryStream(await File.ReadAllBytesAsync(songFilePath));
        
        File.Delete(songFilePath);

        return finalSongStream;
    }

    private async Task<string> GetSongDecryptKey(string licenceUrl, string songId, string keyUri)
    {
        var certDataBase64 = await _appleMusicClient.GetLicense(licenceUrl, songId, keyUri, challenge: "CAQ=");

        string pssh;
        
        using (var ms = new MemoryStream())
        {
            var dataPssh = new WidevineCencHeader
            {
                algorithm = WidevineCencHeader.Algorithm.Aesctr
            };
            
            dataPssh.KeyIds.Add(Encoding.UTF8.GetBytes(keyUri.Split(',').Skip(1).First()));
            
            ProtoBuf.Serializer.Serialize(ms, dataPssh);
            
            pssh = Convert.ToBase64String(ms.ToArray());
        }
        
        var cdm = new CDM(_device, pssh, certDataBase64);
        
        var challenge = Convert.ToBase64String(cdm.GetChallenge());
        
        var license = await _appleMusicClient.GetLicense(licenceUrl, songId, keyUri, challenge);

        cdm.ProvideLicense(license);

        return cdm.GetKeys().First().ToString();
    }
    
    private IDictionary<string, string> CreateTagsDictionary(JsonElement metadata, string? coverPath = null)
    {
        var tags = new Dictionary<string, string>()
        {
            ["Artist:S"] = metadata.GetProperty("artistName").GetString()!,
            ["Name:S"] = metadata.GetProperty("itemName").GetString()!,
            
            ["Album:S"] = metadata.GetProperty("playlistName").GetString()!,
            ["AlbumArtist:S"] = metadata.GetProperty("playlistArtistName").GetString()!,
            
            ["Composer:S"] = metadata.GetProperty("composerName").GetString()!,
            ["Writer:S"] = metadata.GetProperty("composerName").GetString()!,

            ["Disc:B"] = BuildBinaryAtom(metadata.GetProperty("discNumber").GetInt32(), metadata.GetProperty("discCount").GetInt32()).ToHexString(),
            ["Track:B"] = BuildBinaryAtom(metadata.GetProperty("trackNumber").GetInt32(), metadata.GetProperty("trackCount").GetInt32()).ToHexString(),

            ["GenreName:S"] = metadata.GetProperty("genre").GetString()!,
            ["Copyright:S"] = metadata.GetProperty("copyright").GetString()!,
            ["Date:S"] = metadata.GetProperty("releaseDate").GetDateTime().ToString("yyyy-MM-dd"),
            ["Compilation:I8"] = metadata.GetProperty("compilation").GetBoolean() ? "1" : "0",
            ["IsGapless:I8"] = metadata.GetProperty("gapless").GetBoolean() ? "1" : "0",
            ["Rating:I8"] = metadata.GetProperty("explicit").ToString() == "1" ? "4" : "0", // clean = 2
            ["Comment:S"] = $"https://music.apple.com/song/{metadata.GetProperty("itemId").GetString()!}",
            ["StoreFrontID:I32"] = metadata.GetProperty("itemId").GetString()!,
            ["Tool:S"] = "Apple Music Downloader by frytech",
        };

        if (!string.IsNullOrWhiteSpace(coverPath))
            tags["Cover:JPEG"] = coverPath;

        return tags;
    }

    private async Task<string> GetArtworkPath(JsonElement asset)
    {
        var artworkUrl = asset.GetProperty("artworkURL").GetString()!;
        var artworkStream = await _appleMusicClient.Client.GetStreamAsync(artworkUrl);

        var artworkPath = Path.GetTempFileName();
        await artworkStream.WriteToFileAsync(artworkPath);

        return artworkPath;
    }
    
    private static byte[] BuildBinaryAtom(int number, int total)
    {
        return [0x00, 0x00, 0x00, (byte)number, 0x00, (byte)total, 0x00, 0x00];
    }
}