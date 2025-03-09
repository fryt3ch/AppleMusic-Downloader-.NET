using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using frytech.AppleMusicTools.Downloader.Configuration;

namespace frytech.AppleMusicTools.Downloader.Core;

public class AppleMusicClient
{
    public HttpClient Client { get; }

    public AppleMusicClient(HttpClient client, AppleMusicClientOptions options)
    {
        Client = client;
        
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:135.0) Gecko/20100101 Firefox/135.0");
            
        client.DefaultRequestHeaders.Add("Referer", options.Url);
        client.DefaultRequestHeaders.Add("Origin", options.Url);
            
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
        client.DefaultRequestHeaders.Add("Media-User-Token", options.MediaToken);
    }
    
    public async Task<JsonElement> GetWebPlayback(string assetId)
    {
        var response = await Client.PostAsync("https://play.itunes.apple.com/WebObjects/MZPlay.woa/wa/webPlayback", JsonContent.Create(new Dictionary<string, object>
        {
            ["salableAdamId"] = assetId,
        }));

        response.EnsureSuccessStatusCode();

        var responseJsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());

        var songElement = responseJsonElement.GetProperty("songList").EnumerateArray().First();

        return songElement;
    }
    
    public async Task<string> GetLicense(string licenceUrl, string assetId, string keyUri, string challenge)
    {
        var response = await Client.PostAsync(licenceUrl, JsonContent.Create(new Dictionary<string, object>
        {
            ["adamId"] = assetId,
            ["challenge"] = challenge,
            ["isLibrary"] = false,
            ["key-system"] = "com.widevine.alpha",
            ["uri"] = keyUri,
            ["user-initiated"] = true
        }));
        
        response.EnsureSuccessStatusCode();
        
        var responseJsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        
        var certDataBase64 = responseJsonElement.GetProperty("license").GetString()!;

        return certDataBase64;
    }
}