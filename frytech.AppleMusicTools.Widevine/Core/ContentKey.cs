using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace frytech.AppleMusicTools.Widevine.Core;

[Serializable]
public class ContentKey
{
    [JsonPropertyName("key_id")]
    public byte[] KeyId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("bytes")]
    public byte[] Bytes { get; set; }

    [NotMapped]
    [JsonPropertyName("permissions")]
    public IEnumerable<string> Permissions
    {
        get => PermissionsString.Split(",").ToArray();
        set => PermissionsString = string.Join(",", value);
    }

    [JsonIgnore]
    public string PermissionsString { get; set; }

    public override string ToString()
    {
        return $"{BitConverter.ToString(KeyId).Replace("-", "").ToLower()}:{BitConverter.ToString(Bytes).Replace("-", "").ToLower()}";
    }
}