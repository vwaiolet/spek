using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Spek.WinUI.Models;
using Spek.WinUI.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace Spek.WinUI;

public sealed partial class MainWindow : Window
{
    private const string AppVersion = "0.8.5";

    private static readonly (string Code, string Name)[] AvailableLanguages =
    [
        ("", "System default"),
        ("bs", "Bosnian"),
        ("ca", "Catalan"),
        ("cs", "Czech"),
        ("da", "Danish"),
        ("de", "German"),
        ("el", "Greek"),
        ("en", "English"),
        ("eo", "Esperanto"),
        ("es", "Spanish"),
        ("fi", "Finnish"),
        ("fr", "French"),
        ("gl", "Galician"),
        ("he", "Hebrew"),
        ("hr", "Croatian"),
        ("hu", "Hungarian"),
        ("id", "Indonesian"),
        ("it", "Italian"),
        ("ja", "Japanese"),
        ("ko", "Korean"),
        ("lv", "Latvian"),
        ("nb", "Norwegian Bokmal"),
        ("nl", "Dutch"),
        ("nn", "Norwegian Nynorsk"),
        ("pl", "Polish"),
        ("pt_BR", "Brazilian Portuguese"),
        ("ru", "Russian"),
        ("sk", "Slovak"),
        ("sr@latin", "Serbian"),
        ("sv", "Swedish"),
        ("th", "Thai"),
        ("tr", "Turkish"),
        ("uk", "Ukrainian"),
        ("vi", "Vietnamese"),
        ("zh_CN", "Chinese Simplified"),
        ("zh_TW", "Chinese Traditional")
    ];

    private readonly SpectrogramAnalyzer analyzer = new();
    private readonly AppPreferences preferences = AppPreferences.Load();
    private readonly DispatcherTimer resizeTimer = new();
    private CancellationTokenSource? analysisCts;
    private SpectrogramOptions options = new();
    private SpectrogramResult? lastResult;
    private string? currentPath;
    private bool loaded;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Spek";
        ConfigureWindow();

        resizeTimer.Interval = TimeSpan.FromMilliseconds(350);
        resizeTimer.Tick += async (_, _) =>
        {
            resizeTimer.Stop();
            if (loaded && currentPath is not null)
            {
                await AnalyzeCurrentAsync();
            }
        };

        RegisterKeyboardAccelerators();
        UpdateOptionLabels();
    }

    private async void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        loaded = true;
        RenderPaletteOnly();

        string? initialPath = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault(File.Exists);
        if (initialPath is not null)
        {
            await OpenPathAsync(initialPath);
        }

        await CheckForUpdatesAsync();
    }

    private void ConfigureWindow()
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(1100, 720));

        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "spek.ico");
        if (File.Exists(iconPath))
        {
            appWindow.SetIcon(iconPath);
        }
    }

    private void RegisterKeyboardAccelerators()
    {
        AddKeyboardAccelerator(VirtualKey.O, VirtualKeyModifiers.Control, OpenFileAsync);
        AddKeyboardAccelerator(VirtualKey.S, VirtualKeyModifiers.Control, SaveFileAsync);
        AddKeyboardAccelerator(VirtualKey.E, VirtualKeyModifiers.Control, ShowPreferencesAsync);
        AddKeyboardAccelerator(VirtualKey.F1, (VirtualKeyModifiers)0, () =>
        {
            VisitWebsite();
            return Task.CompletedTask;
        });
        AddKeyboardAccelerator(VirtualKey.F1, VirtualKeyModifiers.Shift, ShowAboutAsync);

        AddKeyboardAccelerator(VirtualKey.C, (VirtualKeyModifiers)0, () => ChangeChannelAsync(1));
        AddKeyboardAccelerator(VirtualKey.C, VirtualKeyModifiers.Shift, () => ChangeChannelAsync(-1));
        AddKeyboardAccelerator(VirtualKey.F, (VirtualKeyModifiers)0, () => ChangeWindowFunctionAsync(1));
        AddKeyboardAccelerator(VirtualKey.F, VirtualKeyModifiers.Shift, () => ChangeWindowFunctionAsync(-1));
        AddKeyboardAccelerator(VirtualKey.L, (VirtualKeyModifiers)0, () => ChangeLowerRangeAsync(1));
        AddKeyboardAccelerator(VirtualKey.L, VirtualKeyModifiers.Shift, () => ChangeLowerRangeAsync(-1));
        AddKeyboardAccelerator(VirtualKey.P, (VirtualKeyModifiers)0, () => ChangePaletteAsync(1));
        AddKeyboardAccelerator(VirtualKey.P, VirtualKeyModifiers.Shift, () => ChangePaletteAsync(-1));
        AddKeyboardAccelerator(VirtualKey.S, (VirtualKeyModifiers)0, () => ChangeStreamAsync(1));
        AddKeyboardAccelerator(VirtualKey.S, VirtualKeyModifiers.Shift, () => ChangeStreamAsync(-1));
        AddKeyboardAccelerator(VirtualKey.U, (VirtualKeyModifiers)0, () => ChangeUpperRangeAsync(1));
        AddKeyboardAccelerator(VirtualKey.U, VirtualKeyModifiers.Shift, () => ChangeUpperRangeAsync(-1));
        AddKeyboardAccelerator(VirtualKey.W, (VirtualKeyModifiers)0, () => ChangeFftBitsAsync(1));
        AddKeyboardAccelerator(VirtualKey.W, VirtualKeyModifiers.Shift, () => ChangeFftBitsAsync(-1));
    }

    private void AddKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers modifiers, Func<Task> action)
    {
        KeyboardAccelerator accelerator = new()
        {
            Key = key,
            Modifiers = modifiers
        };
        accelerator.Invoked += async (_, args) =>
        {
            args.Handled = true;
            await action();
        };
        Root.KeyboardAccelerators.Add(accelerator);
    }

    private async void OnOpenClick(object sender, RoutedEventArgs e)
    {
        await OpenFileAsync();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        await SaveFileAsync();
    }

    private async Task OpenFileAsync()
    {
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.MusicLibrary
        };

        foreach (string extension in AudioFileFilters.Extensions)
        {
            picker.FileTypeFilter.Add(extension);
        }

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await OpenPathAsync(file.Path);
        }
    }

    private async Task SaveFileAsync()
    {
        if (lastResult is null)
        {
            return;
        }

        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            SuggestedFileName = Path.GetFileName(currentPath ?? "spectrogram") + ".png"
        };
        picker.FileTypeChoices.Add("PNG image", [".png"]);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        await SavePngAsync(file, lastResult);
        SetStatus($"Saved {file.Name}");
    }

    private async Task ShowPreferencesAsync()
    {
        ComboBox languageBox = new()
        {
            MinWidth = 260,
            ItemsSource = AvailableLanguages.Select(language => language.Name).ToList()
        };
        int languageIndex = Array.FindIndex(AvailableLanguages, language => language.Code == preferences.Language);
        languageBox.SelectedIndex = Math.Max(0, languageIndex);

        CheckBox updateCheckBox = new()
        {
            Content = "Check for updates",
            IsChecked = preferences.CheckForUpdates
        };

        StackPanel panel = new()
        {
            Spacing = 14
        };
        panel.Children.Add(new TextBlock { Text = "Language" });
        panel.Children.Add(languageBox);
        panel.Children.Add(updateCheckBox);

        ContentDialog dialog = new()
        {
            XamlRoot = Root.XamlRoot,
            Title = "Preferences",
            Content = panel,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            int selectedIndex = Math.Max(0, languageBox.SelectedIndex);
            preferences.Language = AvailableLanguages[selectedIndex].Code;
            preferences.CheckForUpdates = updateCheckBox.IsChecked == true;
            preferences.Save();
            SetStatus("Preferences saved");
        }
    }

    private async Task ShowAboutAsync()
    {
        StackPanel panel = new()
        {
            Spacing = 10
        };
        panel.Children.Add(new TextBlock
        {
            Text = "Acoustic spectrum analyser",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "WinUI 3 front end for Spek. Copyright (c) 2010-2013 Alexander Kojevnikov and contributors.",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Original Spek developers include Alexander Kojevnikov, Andreas Cadhalpun, Colin Watson, Daniel Hams, and contributors.",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "This program is free software under the GNU GPLv3 and comes with ABSOLUTELY NO WARRANTY. See LICENSE and THIRD-PARTY-NOTICES.md in the application folder.",
            TextWrapping = TextWrapping.Wrap
        });

        ContentDialog dialog = new()
        {
            XamlRoot = Root.XamlRoot,
            Title = $"Spek {AppVersion}",
            Content = panel,
            SecondaryButtonText = "Open notices",
            CloseButtonText = "OK"
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Secondary)
        {
            await OpenNoticeFileAsync();
        }
    }

    private static async Task OpenNoticeFileAsync()
    {
        string noticesPath = Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md");
        string licensePath = Path.Combine(AppContext.BaseDirectory, "LICENSE");
        string path = File.Exists(noticesPath) ? noticesPath : licensePath;
        if (!File.Exists(path))
        {
            return;
        }

        StorageFile file = await StorageFile.GetFileFromPathAsync(path);
        await Launcher.LaunchFileAsync(file);
    }

    private async Task CheckForUpdatesAsync()
    {
        if (!preferences.CheckForUpdates)
        {
            return;
        }

        int today = DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
        if (today - preferences.LastUpdateDay < 7)
        {
            return;
        }

        try
        {
            using HttpClient client = new()
            {
                Timeout = TimeSpan.FromSeconds(6)
            };
            string latest = (await client.GetStringAsync("http://help.spek.cc/version")).Trim();
            if (CompareVersions(latest, AppVersion) > 0)
            {
                UpdateInfoBar.IsOpen = true;
            }

            preferences.LastUpdateDay = today;
            preferences.Save();
        }
        catch
        {
        }
    }

    private void OnHelpClick(object sender, RoutedEventArgs e)
    {
        VisitWebsite();
    }

    private async void OnPreferencesClick(object sender, RoutedEventArgs e)
    {
        await ShowPreferencesAsync();
    }

    private async void OnAboutClick(object sender, RoutedEventArgs e)
    {
        await ShowAboutAsync();
    }

    private void OnVisitWebsiteClick(object sender, RoutedEventArgs e)
    {
        VisitWebsite();
    }

    private async void OnStreamClick(object sender, RoutedEventArgs e)
    {
        await ChangeStreamAsync(1);
    }

    private async void OnChannelClick(object sender, RoutedEventArgs e)
    {
        await ChangeChannelAsync(1);
    }

    private async void OnPaletteClick(object sender, RoutedEventArgs e)
    {
        await ChangePaletteAsync(1);
    }

    private async void OnWindowFunctionClick(object sender, RoutedEventArgs e)
    {
        await ChangeWindowFunctionAsync(1);
    }

    private async void OnFftDownClick(object sender, RoutedEventArgs e)
    {
        await ChangeFftBitsAsync(-1);
    }

    private async void OnFftUpClick(object sender, RoutedEventArgs e)
    {
        await ChangeFftBitsAsync(1);
    }

    private async void OnUpperRangeDownClick(object sender, RoutedEventArgs e)
    {
        await ChangeUpperRangeAsync(-1);
    }

    private async void OnUpperRangeUpClick(object sender, RoutedEventArgs e)
    {
        await ChangeUpperRangeAsync(1);
    }

    private async void OnLowerRangeDownClick(object sender, RoutedEventArgs e)
    {
        await ChangeLowerRangeAsync(-1);
    }

    private async void OnLowerRangeUpClick(object sender, RoutedEventArgs e)
    {
        await ChangeLowerRangeAsync(1);
    }

    private async void OnRootDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
        StorageFile? file = items.OfType<StorageFile>().FirstOrDefault();
        if (file is not null)
        {
            await OpenPathAsync(file.Path);
        }
    }

    private void OnRootDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private void OnSpectrogramHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!loaded || currentPath is null || Math.Abs(e.NewSize.Width - e.PreviousSize.Width) < 32)
        {
            return;
        }

        resizeTimer.Stop();
        resizeTimer.Start();
    }

    private async Task OpenPathAsync(string path)
    {
        if (!File.Exists(path))
        {
            SetStatus("File not found");
            return;
        }

        currentPath = path;
        options = options with { Stream = 0, Channel = 0 };
        string fileName = Path.GetFileName(path);
        string directory = Path.GetDirectoryName(path) ?? path;

        Title = $"Spek - {fileName}";
        TitleText.Text = fileName;
        DetailText.Text = directory;
        PlotFileName.Text = path;
        PlotDescription.Text = "Opening";
        EmptyState.Visibility = Visibility.Collapsed;
        SaveButton.IsEnabled = false;

        await AnalyzeCurrentAsync();
    }

    private async Task AnalyzeCurrentAsync()
    {
        if (currentPath is null)
        {
            return;
        }

        analysisCts?.Cancel();
        analysisCts?.Dispose();
        CancellationTokenSource cts = new();
        analysisCts = cts;

        SetBusy(true);
        SetStatus("Analyzing");

        try
        {
            int targetColumns = GetTargetColumns();
            SpectrogramResult result = await analyzer.AnalyzeAsync(currentPath, options, targetColumns, cts.Token);
            if (cts.IsCancellationRequested)
            {
                return;
            }

            lastResult = result;
            options = options with
            {
                Stream = result.Stream,
                Channel = result.Channel,
                UpperRange = result.UpperRange,
                LowerRange = result.LowerRange
            };
            await ApplyResultAsync(result);
            SetStatus("Loaded");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            lastResult = null;
            SaveButton.IsEnabled = false;
            SpectrogramImage.Source = null;
            EmptyState.Visibility = Visibility.Visible;
            PlotDescription.Text = ex.Message;
            SetStatus(ex.Message);
        }
        finally
        {
            if (analysisCts == cts)
            {
                analysisCts = null;
                SetBusy(false);
            }
        }
    }

    private async Task ApplyResultAsync(SpectrogramResult result)
    {
        SpectrogramImage.Source = await CreateBitmapAsync(result.Pixels, result.Width, result.Height);
        PaletteImage.Source = await CreateBitmapAsync(result.PalettePixels, result.PaletteWidth, result.PaletteHeight);
        EmptyState.Visibility = Visibility.Collapsed;

        PlotDescription.Text = result.Description;
        FreqTopText.Text = FormatFrequency(result.SampleRate / 2.0);
        FreqMidText.Text = FormatFrequency(result.SampleRate / 4.0);
        TimeMidText.Text = FormatDuration(TimeSpan.FromSeconds(result.Duration.TotalSeconds / 2.0));
        TimeEndText.Text = FormatDuration(result.Duration);
        DbTopText.Text = $"{result.UpperRange} dB";
        DbMidText.Text = $"{(result.UpperRange + result.LowerRange) / 2} dB";
        DbBottomText.Text = $"{result.LowerRange} dB";
        SaveButton.IsEnabled = true;

        UpdateOptionLabels();
    }

    private async void RenderPaletteOnly()
    {
        byte[] pixels = PaletteRenderer.CreatePalettePixels(
            options.Palette,
            12,
            Math.Max(128, 1 << (options.FftBits - 1)));
        PaletteImage.Source = await CreateBitmapAsync(pixels, 12, Math.Max(128, 1 << (options.FftBits - 1)));
    }

    private int GetTargetColumns()
    {
        double width = SpectrogramFrame.ActualWidth;
        if (double.IsNaN(width) || width < 1)
        {
            width = 960;
        }

        return Math.Clamp((int)Math.Round(width), 360, 1600);
    }

    private void UpdateOptionLabels()
    {
        int streams = lastResult?.Streams ?? 1;
        int stream = Math.Clamp(options.Stream, 0, Math.Max(0, streams - 1));
        int channels = lastResult?.Channels ?? 1;
        int channel = Math.Clamp(options.Channel, 0, Math.Max(0, channels - 1));
        StreamText.Text = $"Stream {stream + 1} / {streams}";
        StreamButton.IsEnabled = streams > 1 && currentPath is not null;
        ChannelText.Text = $"Channel {channel + 1} / {channels}";
        ChannelButton.IsEnabled = channels > 1 && currentPath is not null;
        PaletteText.Text = $"Palette {DisplayName(options.Palette)}";
        WindowFunctionText.Text = DisplayName(options.WindowFunction);
        FftText.Text = $"FFT {1 << options.FftBits}";
        UpperRangeText.Text = $"Upper {options.UpperRange} dB";
        LowerRangeText.Text = $"Lower {options.LowerRange} dB";
    }

    private async Task ChangeStreamAsync(int delta)
    {
        int streams = lastResult?.Streams ?? 1;
        if (streams <= 1)
        {
            return;
        }

        options = options with { Stream = Wrap(options.Stream + delta, streams), Channel = 0 };
        UpdateOptionLabels();
        await AnalyzeCurrentAsync();
    }

    private async Task ChangeChannelAsync(int delta)
    {
        int channels = lastResult?.Channels ?? 1;
        if (channels <= 1)
        {
            return;
        }

        options = options with { Channel = Wrap(options.Channel + delta, channels) };
        UpdateOptionLabels();
        await AnalyzeCurrentAsync();
    }

    private async Task ChangePaletteAsync(int delta)
    {
        SpekPalette[] palettes = Enum.GetValues<SpekPalette>();
        options = options with
        {
            Palette = palettes[Wrap(Array.IndexOf(palettes, options.Palette) + delta, palettes.Length)]
        };
        UpdateOptionLabels();
        RenderPaletteOnly();
        await AnalyzeCurrentAsync();
    }

    private async Task ChangeWindowFunctionAsync(int delta)
    {
        SpekWindowFunction[] functions = Enum.GetValues<SpekWindowFunction>();
        options = options with
        {
            WindowFunction = functions[
                Wrap(Array.IndexOf(functions, options.WindowFunction) + delta, functions.Length)]
        };
        UpdateOptionLabels();
        await AnalyzeCurrentAsync();
    }

    private async Task ChangeFftBitsAsync(int delta)
    {
        int next = Math.Clamp(
            options.FftBits + delta,
            SpectrogramOptions.MinFftBits,
            SpectrogramOptions.MaxFftBits);
        if (next == options.FftBits)
        {
            return;
        }

        options = options with { FftBits = next };
        UpdateOptionLabels();
        RenderPaletteOnly();
        await AnalyzeCurrentAsync();
    }

    private async Task ChangeUpperRangeAsync(int delta)
    {
        int next = delta > 0
            ? Math.Min(options.UpperRange + delta, SpectrogramOptions.MaxRange)
            : Math.Max(options.UpperRange + delta, options.LowerRange + 1);
        if (next == options.UpperRange)
        {
            return;
        }

        options = options with { UpperRange = next };
        UpdateOptionLabels();
        RenderPaletteOnly();
        await AnalyzeCurrentAsync();
    }

    private async Task ChangeLowerRangeAsync(int delta)
    {
        int next = delta > 0
            ? Math.Min(options.LowerRange + delta, options.UpperRange - 1)
            : Math.Max(options.LowerRange + delta, SpectrogramOptions.MinRange);
        if (next == options.LowerRange)
        {
            return;
        }

        options = options with { LowerRange = next };
        UpdateOptionLabels();
        RenderPaletteOnly();
        await AnalyzeCurrentAsync();
    }

    private static int Wrap(int value, int count)
    {
        return ((value % count) + count) % count;
    }

    private static void VisitWebsite()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.spek.cc/",
            UseShellExecute = true
        });
    }

    private void SetBusy(bool busy)
    {
        BusyOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
    }

    private static async Task<WriteableBitmap> CreateBitmapAsync(byte[] pixels, int width, int height)
    {
        WriteableBitmap bitmap = new(width, height);
        using Stream stream = bitmap.PixelBuffer.AsStream();
        await stream.WriteAsync(pixels);
        bitmap.Invalidate();
        return bitmap;
    }

    private static async Task SavePngAsync(StorageFile file, SpectrogramResult result)
    {
        using Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);
        stream.Size = 0;
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)result.Width,
            (uint)result.Height,
            96,
            96,
            result.Pixels);
        await encoder.FlushAsync();
    }

    private static string FormatDuration(TimeSpan value)
    {
        int totalSeconds = Math.Max(0, (int)Math.Round(value.TotalSeconds));
        int hours = totalSeconds / 3600;
        int minutes = totalSeconds / 60 % 60;
        int seconds = totalSeconds % 60;
        return hours > 0 ? $"{hours}:{minutes:00}:{seconds:00}" : $"{minutes}:{seconds:00}";
    }

    private static string FormatFrequency(double value)
    {
        return value >= 1000 ? $"{value / 1000.0:0.#} kHz" : $"{value:0} Hz";
    }

    private static int CompareVersions(string left, string right)
    {
        if (Version.TryParse(left, out Version? leftVersion) &&
            Version.TryParse(right, out Version? rightVersion))
        {
            return leftVersion.CompareTo(rightVersion);
        }

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static string DisplayName(SpekPalette palette)
    {
        return palette switch
        {
            SpekPalette.Spectrum => "Spectrum",
            SpekPalette.Sox => "SoX",
            SpekPalette.Mono => "Mono",
            _ => palette.ToString()
        };
    }

    private static string DisplayName(SpekWindowFunction windowFunction)
    {
        return windowFunction switch
        {
            SpekWindowFunction.Hann => "Hann",
            SpekWindowFunction.Hamming => "Hamming",
            SpekWindowFunction.BlackmanHarris => "Blackman-Harris",
            _ => windowFunction.ToString()
        };
    }
}
