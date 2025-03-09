namespace frytech.AppleMusicTools.Downloader.Extensions;

internal static class StreamExtensions
{
    public static async Task<string> ReadAsStringAsync(this Stream stream)
    {
        using var reader = new StreamReader(stream);
        
        return await reader.ReadToEndAsync();
    }
    
    public static async Task<byte[]> ReadAsBytesAsync(this Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        return ms.ToArray();
    }

    public static async Task WriteToFileAsync(this Stream stream, string filePath, FileMode fileMode = FileMode.Create)
    {
        await using var fs = new FileStream(filePath, fileMode, FileAccess.Write);

        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
        
        await stream.CopyToAsync(fs);
        
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);
    }
}