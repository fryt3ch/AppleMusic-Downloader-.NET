namespace frytech.AppleMusicTools.Widevine.Models;

internal sealed record DerivedKeys
{
    public required byte[] Auth1 { get; init; }
    
    public required byte[] Auth2 { get; init; }
    
    public required byte[] Enc { get; init; }
}