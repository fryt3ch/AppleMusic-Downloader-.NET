using System.Text;
using System.Text.Json;
using frytech.AppleMusicTools.Downloader.Configuration;
using frytech.AppleMusicTools.Downloader.Extensions;
using frytech.AppleMusicTools.Widevine;
using frytech.AppleMusicTools.Widevine.Core;

namespace frytech.AppleMusicTools.Downloader.Core;

public sealed class Downloader
{
    private readonly AppleMusicClient _appleMusicClient;
    private readonly Device _device;
    private readonly SongTagger _songTagger;
    private readonly SongDecryptor _songDecryptor;

    public Downloader(AppleMusicClient appleMusicClient, Device device, DownloaderOptions options)
    {
        _appleMusicClient = appleMusicClient;
        _device = device;

        _songTagger = new SongTagger(options.FfmpegPath);
        _songDecryptor = new SongDecryptor(options.Mp4DecryptPath);
    }

    public async Task<MemoryStream> Download(string assetId, DownloadOptions? options = null)
    {
        options ??= new DownloadOptions();
        
        var webPlayback = await _appleMusicClient.GetWebPlayback(assetId);

        if (webPlayback.TryGetProperty("hls-playlist-url", out var playlistUrl))
            throw new NotSupportedException("Videos are not supported!");
        
        return await DownloadSong(assetId, webPlayback, options);
    }

    private async Task<MemoryStream> DownloadSong(string assetId, JsonElement webPlayback, DownloadOptions options)
    {
        var licenseUrl = webPlayback.GetProperty("hls-key-server-url").GetString()!;
        
        if (!webPlayback.TryGetProperty("assets", out var assets))
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
        
        await _songDecryptor.DecryptSongFile(songEncryptedFilePath, songDecryptedFilePath, songDecryptKey);
        
        File.Delete(songEncryptedFilePath);
        
        var songFilePath = Path.GetTempFileName();

        var tagsDictionary = CreateTagsDictionary(metadata);
        var artworkPath = options.IncludeArtwork ? await GetArtworkPath(asset) : null;
        
        await _songTagger.MuxAndTagSongFile(songDecryptedFilePath, songFilePath, tagsDictionary, artworkPath);
        
        File.Delete(songDecryptedFilePath);
        
        if (artworkPath is not null)
            File.Delete(artworkPath);
        
        var finalSongStream = new MemoryStream(await File.ReadAllBytesAsync(songFilePath));
        
        File.Delete(songFilePath);

        return finalSongStream;
    }

    private async Task<string> GetSongDecryptKey(string licenceUrl, string songId, string keyUri)
    {
        var certDataBase64 = await _appleMusicClient.GetLicense(licenceUrl, songId, keyUri, "CAQ=");

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
    
    private IDictionary<string, string> CreateTagsDictionary(JsonElement metadata)
    {
        return new Dictionary<string, string>()
        {
            ["artist"] = metadata.GetProperty("artistName").GetString()!,
            ["----:com.apple.iTunes:sortArtist"] = metadata.GetProperty("sort-artist").GetString()!,

            ["title"] = metadata.GetProperty("itemName").GetString()!,
            ["sort_name"] = metadata.GetProperty("sort-name").GetString()!,

            ["album"] = metadata.GetProperty("playlistName").GetString()!,
            ["----:com.apple.iTunes:sortAlbum"] = metadata.GetProperty("sort-album").GetString()!,

            ["composer"] = metadata.GetProperty("composerName").GetString()!,
            ["writer"] = metadata.GetProperty("composerName").GetString()!,
            ["sort_composer"] = metadata.GetProperty("sort-composer").GetString()!,

            ["album_artist"] = metadata.GetProperty("playlistArtistName").GetString()!,
            ["sort_album_artist"] = metadata.GetProperty("playlistArtistName").GetString()!,

            ["disk"] = $"{metadata.GetProperty("discNumber").GetInt32()}/{metadata.GetProperty("discCount").GetInt32()}",
            ["tracknum"] = $"{metadata.GetProperty("trackNumber").GetInt32()}/{metadata.GetProperty("trackCount").GetInt32()}",

            ["genre"] = metadata.GetProperty("genre").GetString()!,
            ["copyright"] = metadata.GetProperty("copyright").GetString()!,
            ["created"] = metadata.GetProperty("releaseDate").GetDateTime().ToString("yyyy-MM-dd"),
            ["----:com.apple.iTunes:compilation"] = metadata.GetProperty("compilation").GetBoolean() ? "yes" : "no",
            ["----:com.apple.iTunes:gapless"] = metadata.GetProperty("gapless").GetBoolean() ? "yes" : "no",
            ["rating"] = metadata.GetProperty("explicit").ToString() == "1" ? "4" : "0", // clean = 2
            ["media"] = "1", // normal
            ["online_info"] =$"https://music.apple.com/song/{metadata.GetProperty("itemId").GetString()}",
            //["url"] =$"https://music.apple.com/song/{metadata.GetProperty("itemId").GetString()}",
            ["publisher"] = "Apple Music",
            ["tool"] = "Apple Music",
            ["encoder"] = "frytech",
        };
    }

    private async Task<string> GetArtworkPath(JsonElement asset)
    {
        var artworkUrl = asset.GetProperty("artworkURL").GetString()!;

        var artworkStream = await _appleMusicClient.Client.GetStreamAsync(artworkUrl);

        var artworkPath = Path.GetTempFileName();
        await artworkStream.WriteToFileAsync(artworkPath);

        return artworkPath;
    }
}