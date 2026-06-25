namespace Spek.WinUI.Models;

public sealed record SpectrogramResult(
    string FilePath,
    string Description,
    int Width,
    int Height,
    byte[] Pixels,
    int PaletteWidth,
    int PaletteHeight,
    byte[] PalettePixels,
    TimeSpan Duration,
    int SampleRate,
    int Streams,
    int Stream,
    int Channels,
    int Channel,
    int FftSize,
    SpekWindowFunction WindowFunction,
    SpekPalette Palette,
    int UpperRange,
    int LowerRange);
