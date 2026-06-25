using System.Runtime.InteropServices;
using Spek.WinUI.Models;

namespace Spek.WinUI.Services;

public sealed class NativeSpectrogramAnalyzer
{
    private const int OptionsVersion = 1;
    private const string NativeLibraryName = "spek-native";
    private static bool resolverRegistered;
    private static string? nativeDllPath;

    private NativeSpectrogramAnalyzer()
    {
    }

    public static NativeSpectrogramAnalyzer? TryCreate()
    {
        string dllPath = Path.Combine(AppContext.BaseDirectory, "spek-native.dll");
        if (!File.Exists(dllPath))
        {
            return null;
        }

        nativeDllPath = dllPath;
        EnsureResolver();
        NativeLibrary.Load(dllPath);
        return new NativeSpectrogramAnalyzer();
    }

    private static void EnsureResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        NativeLibrary.SetDllImportResolver(typeof(NativeSpectrogramAnalyzer).Assembly, (libraryName, _, _) =>
        {
            if (libraryName == NativeLibraryName && nativeDllPath is not null)
            {
                return NativeLibrary.Load(nativeDllPath);
            }

            return IntPtr.Zero;
        });
        resolverRegistered = true;
    }

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
        cancellationToken.ThrowIfCancellationRequested();

        NativeOptions nativeOptions = new()
        {
            Version = OptionsVersion,
            TargetColumns = Math.Clamp(targetColumns, 360, 1600),
            Stream = Math.Max(0, options.Stream),
            Channel = Math.Max(0, options.Channel),
            FftBits = Math.Clamp(options.FftBits, SpectrogramOptions.MinFftBits, SpectrogramOptions.MaxFftBits),
            UpperRange = options.UpperRange,
            LowerRange = options.LowerRange,
            Palette = (int)options.Palette,
            WindowFunction = (int)options.WindowFunction
        };

        IntPtr pathPtr = Marshal.StringToCoTaskMemUTF8(path);
        NativeResult nativeResult = default;

        try
        {
            int code = NativeMethods.Analyze(pathPtr, ref nativeOptions, out nativeResult);
            if (code != 0)
            {
                string error = Marshal.PtrToStringUTF8(nativeResult.Error) ??
                    $"Native analyzer failed with code {code}.";
                throw new InvalidOperationException(error);
            }

            byte[] pixels = CopyBytes(nativeResult.Pixels, nativeResult.Width * nativeResult.Height * 4);
            byte[] palettePixels = CopyBytes(
                nativeResult.PalettePixels,
                nativeResult.PaletteWidth * nativeResult.PaletteHeight * 4);
            string description = Marshal.PtrToStringUTF8(nativeResult.Description) ?? Path.GetFileName(path);

            cancellationToken.ThrowIfCancellationRequested();

            return new SpectrogramResult(
                path,
                description,
                nativeResult.Width,
                nativeResult.Height,
                pixels,
                nativeResult.PaletteWidth,
                nativeResult.PaletteHeight,
                palettePixels,
                TimeSpan.FromSeconds(nativeResult.DurationSeconds),
                nativeResult.SampleRate,
                Math.Max(1, nativeResult.Streams),
                Math.Max(0, nativeResult.Stream),
                Math.Max(1, nativeResult.Channels),
                Math.Max(0, nativeResult.Channel),
                nativeResult.FftSize,
                options.WindowFunction,
                options.Palette,
                nativeResult.UpperRange,
                nativeResult.LowerRange);
        }
        catch (DllNotFoundException ex)
        {
            throw new NativeAnalyzerUnavailableException("spek-native.dll was not found.", ex);
        }
        catch (EntryPointNotFoundException ex)
        {
            throw new NativeAnalyzerUnavailableException("spek-native.dll does not expose the expected API.", ex);
        }
        catch (BadImageFormatException ex)
        {
            throw new NativeAnalyzerUnavailableException("spek-native.dll does not match this process architecture.", ex);
        }
        finally
        {
            try
            {
                NativeMethods.FreeResult(ref nativeResult);
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }

            Marshal.FreeCoTaskMem(pathPtr);
        }
    }

    private static byte[] CopyBytes(IntPtr source, int count)
    {
        if (source == IntPtr.Zero || count <= 0)
        {
            return [];
        }

        byte[] bytes = new byte[count];
        Marshal.Copy(source, bytes, 0, count);
        return bytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeOptions
    {
        public int Version;
        public int TargetColumns;
        public int Stream;
        public int Channel;
        public int FftBits;
        public int UpperRange;
        public int LowerRange;
        public int Palette;
        public int WindowFunction;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeResult
    {
        public int Version;
        public int Width;
        public int Height;
        public IntPtr Pixels;
        public int PaletteWidth;
        public int PaletteHeight;
        public IntPtr PalettePixels;
        public double DurationSeconds;
        public int SampleRate;
        public int Streams;
        public int Stream;
        public int Channels;
        public int Channel;
        public int FftSize;
        public int UpperRange;
        public int LowerRange;
        public IntPtr Description;
        public IntPtr Error;
    }

    private static partial class NativeMethods
    {
        [DllImport(NativeLibraryName, EntryPoint = "spek_native_analyze_utf8", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Analyze(IntPtr path, ref NativeOptions options, out NativeResult result);

        [DllImport(NativeLibraryName, EntryPoint = "spek_native_free_result", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeResult(ref NativeResult result);
    }
}
