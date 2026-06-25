using Spek.WinUI.Models;

namespace Spek.WinUI.Services;

public static class PaletteRenderer
{
    public static byte[] CreatePalettePixels(SpekPalette palette, int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            double level = 1.0 - y / (double)Math.Max(1, height - 1);
            Rgb color = Map(palette, level);

            for (int x = 0; x < width; x++)
            {
                int offset = (y * width + x) * 4;
                pixels[offset] = color.B;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.R;
                pixels[offset + 3] = 255;
            }
        }

        return pixels;
    }

    public static Rgb Map(SpekPalette palette, double level)
    {
        level = Math.Clamp(level, 0.0, 1.0);
        return palette switch
        {
            SpekPalette.Spectrum => Spectrum(level),
            SpekPalette.Sox => Sox(level),
            SpekPalette.Mono => Mono(level),
            _ => Sox(level)
        };
    }

    private static Rgb Spectrum(double level)
    {
        level *= 0.6625;
        double r = 0.0;
        double g = 0.0;
        double b = 0.0;

        if (level < 0.15)
        {
            r = (0.15 - level) / (0.15 + 0.075);
            b = 1.0;
        }
        else if (level < 0.275)
        {
            g = (level - 0.15) / (0.275 - 0.15);
            b = 1.0;
        }
        else if (level < 0.325)
        {
            g = 1.0;
            b = (0.325 - level) / (0.325 - 0.275);
        }
        else if (level < 0.5)
        {
            r = (level - 0.325) / (0.5 - 0.325);
            g = 1.0;
        }
        else if (level < 0.6625)
        {
            r = 1.0;
            g = (0.6625 - level) / (0.6625 - 0.5);
        }

        double correction = level < 0.1 ? level / 0.1 : 1.0;
        return Rgb.FromUnit(r * correction, g * correction, b * correction);
    }

    private static Rgb Sox(double level)
    {
        double r = 0.0;
        if (level >= 0.13 && level < 0.73)
        {
            r = Math.Sin((level - 0.13) / 0.60 * Math.PI / 2.0);
        }
        else if (level >= 0.73)
        {
            r = 1.0;
        }

        double g = 0.0;
        if (level >= 0.6 && level < 0.91)
        {
            g = Math.Sin((level - 0.6) / 0.31 * Math.PI / 2.0);
        }
        else if (level >= 0.91)
        {
            g = 1.0;
        }

        double b = 0.0;
        if (level < 0.60)
        {
            b = 0.5 * Math.Sin(level / 0.6 * Math.PI);
        }
        else if (level >= 0.78)
        {
            b = (level - 0.78) / 0.22;
        }

        return Rgb.FromUnit(r, g, b);
    }

    private static Rgb Mono(double level)
    {
        byte value = ToByte(level);
        return new Rgb(value, value, value);
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value * 255.0), 0, 255);
    }

    public readonly record struct Rgb(byte R, byte G, byte B)
    {
        public static Rgb FromUnit(double r, double g, double b)
        {
            return new Rgb(ToByte(r), ToByte(g), ToByte(b));
        }
    }
}
