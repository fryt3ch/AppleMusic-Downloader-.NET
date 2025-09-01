using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace frytech.AppleMusicTools.Widevine.Models;

[Serializable]
public sealed record ContentKey
{
    [JsonPropertyName("key_id")]
    public required byte[] KeyId { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("bytes")]
    public required byte[] Bytes { get; set; }

    [NotMapped]
    [JsonPropertyName("permissions")]
    public required IEnumerable<string> Permissions
    {
        get => PermissionsString.Split(",");
        set => PermissionsString = string.Join(",", value);
    }

    [JsonIgnore]
    public string PermissionsString { get; set; } = null!;

    public override string ToString()
    {
        return $"{BitConverter.ToString(KeyId).Replace("-", "").ToLower()}:{BitConverter.ToString(Bytes).Replace("-", "").ToLower()}";
    }
}