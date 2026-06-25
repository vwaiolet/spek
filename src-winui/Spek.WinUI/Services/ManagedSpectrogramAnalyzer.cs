using NAudio.Wave;
using Spek.WinUI.Models;

namespace Spek.WinUI.Services;

public sealed class ManagedSpectrogramAnalyzer
{
    public Task<SpectrogramResult> AnalyzeAsync(
        string path,
        SpectrogramOptions options,
        int targetColumns,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => Analyze(path, options, targetColumns, cancellationToken), cancellationToken);
    }

    private static SpectrogramResult Analyze(
        string path,
        SpectrogramOptions options,
        int targetColumns,
        CancellationToken cancellationToken)
    {
        using AudioFileReader reader = new(path);

        int channels = Math.Max(1, reader.WaveFormat.Channels);
        int channel = Math.Clamp(options.Channel, 0, channels - 1);
        int sampleRate = reader.WaveFormat.SampleRate;
        TimeSpan duration = reader.TotalTime;
        int columns = Math.Clamp(targetColumns, 360, 1600);
        int fftBits = Math.Clamp(options.FftBits, SpectrogramOptions.MinFftBits, SpectrogramOptions.MaxFftBits);
        int fftSize = 1 << fftBits;
        int bands = fftSize / 2 + 1;

        byte[] pixels = new byte[columns * bands * 4];
        double[] window = CreateWindow(options.WindowFunction, fftSize);
        double[] real = new double[fftSize];
        double[] imaginary = new double[fftSize];
        float[] buffer = new float[fftSize * channels];
        double normalization = fftSize * (double)fftSize;

        for (int column = 0; column < columns; column++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Array.Clear(real);
            Array.Clear(imaginary);
            Array.Clear(buffer);

            double centerSeconds = duration.TotalSeconds <= 0
                ? 0
                : (column + 0.5) * duration.TotalSeconds / columns;
            double startSeconds = Math.Max(0, centerSeconds - fftSize / (2.0 * sampleRate));
            reader.CurrentTime = TimeSpan.FromSeconds(startSeconds);

            int floatsRead = ReadWindow(reader, buffer);
            int framesRead = floatsRead / channels;

            for (int i = 0; i < fftSize && i < framesRead; i++)
            {
                int offset = i * channels + channel;
                if (offset < floatsRead)
                {
                    real[i] = buffer[offset] * window[i];
                }
            }

            FastFourierTransform.Transform(real, imaginary);

            for (int band = 0; band < bands; band++)
            {
                double magnitude = (real[band] * real[band] + imaginary[band] * imaginary[band]) / normalization;
                double db = 10.0 * Math.Log10(Math.Max(magnitude, 1.0e-20));
                double level = (Math.Clamp(db, options.LowerRange, options.UpperRange) - options.LowerRange) /
                    (double)(options.UpperRange - options.LowerRange);
                PaletteRenderer.Rgb color = PaletteRenderer.Map(options.Palette, level);
                int y = bands - band - 1;
                int pixelOffset = (y * columns + column) * 4;
                pixels[pixelOffset] = color.B;
                pixels[pixelOffset + 1] = color.G;
                pixels[pixelOffset + 2] = color.R;
                pixels[pixelOffset + 3] = 255;
            }
        }

        byte[] palettePixels = PaletteRenderer.CreatePalettePixels(options.Palette, 12, bands);
        string description = BuildDescription(path, reader, channel, fftSize, options.WindowFunction);

        return new SpectrogramResult(
            path,
            description,
            columns,
            bands,
            pixels,
            12,
            bands,
            palettePixels,
            duration,
            sampleRate,
            1,
            0,
            channels,
            channel,
            fftSize,
            options.WindowFunction,
            options.Palette,
            options.UpperRange,
            options.LowerRange);
    }

    private static int ReadWindow(AudioFileReader reader, float[] buffer)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = reader.Read(buffer, offset, buffer.Length - offset);
            if (read <= 0)
            {
                break;
            }

            offset += read;
        }

        return offset;
    }

    private static double[] CreateWindow(SpekWindowFunction function, int fftSize)
    {
        double[] window = new double[fftSize];
        double divisor = Math.Max(1, fftSize - 1);

        for (int i = 0; i < fftSize; i++)
        {
            double c = 2.0 * Math.PI * i / divisor;
            window[i] = function switch
            {
                SpekWindowFunction.Hann => 0.5 * (1.0 - Math.Cos(c)),
                SpekWindowFunction.Hamming => 0.53836 - 0.46164 * Math.Cos(c),
                SpekWindowFunction.BlackmanHarris => 0.35875 - 0.48829 * Math.Cos(c) +
                    0.14128 * Math.Cos(2.0 * c) - 0.01168 * Math.Cos(3.0 * c),
                _ => 1.0
            };
        }

        return window;
    }

    private static string BuildDescription(
        string path,
        AudioFileReader reader,
        int channel,
        int fftSize,
        SpekWindowFunction windowFunction)
    {
        string format = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(format))
        {
            format = "Audio";
        }

        string windowName = windowFunction switch
        {
            SpekWindowFunction.BlackmanHarris => "Blackman-Harris",
            _ => windowFunction.ToString()
        };

        return string.Join(", ",
            format,
            $"{reader.WaveFormat.SampleRate} Hz",
            $"channel {channel + 1} / {Math.Max(1, reader.WaveFormat.Channels)}",
            $"W:{fftSize}",
            $"F:{windowName}");
    }
}
