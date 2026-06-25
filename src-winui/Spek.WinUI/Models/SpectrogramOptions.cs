namespace Spek.WinUI.Models;

public enum SpekPalette
{
    Spectrum,
    Sox,
    Mono
}

public enum SpekWindowFunction
{
    Hann,
    Hamming,
    BlackmanHarris
}

public sealed record SpectrogramOptions(
    SpekPalette Palette = SpekPalette.Sox,
    SpekWindowFunction WindowFunction = SpekWindowFunction.Hann,
    int FftBits = 11,
    int UpperRange = 0,
    int LowerRange = -120,
    int Stream = 0,
    int Channel = 0)
{
    public const int MinRange = -140;
    public const int MaxRange = 0;
    public const int MinFftBits = 8;
    public const int MaxFftBits = 14;

    public int FftSize => 1 << FftBits;
}
