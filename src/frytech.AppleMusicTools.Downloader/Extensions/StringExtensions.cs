namespace frytech.AppleMusicTools.Downloader.Extensions;

internal static class StringExtensions
{
    public static string ToHexString(this byte[] bytes)
    {
        return "#" + BitConverter.ToString(bytes).Replace("-", "");
    }
}