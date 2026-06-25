# Spek WinUI

This is the Windows-only WinUI 3 front end for Spek. It uses the Windows App SDK
for the UI and NAudio for Windows audio decoding.

## Build

From this directory:

```powershell
dotnet restore .\Spek.WinUI.slnx
dotnet build .\Spek.WinUI\Spek.WinUI.csproj -c Debug -r win-x64
```

## Run

```powershell
dotnet run --project .\Spek.WinUI\Spek.WinUI.csproj -c Debug -r win-x64 -- .\path\to\audio.wav
```

## Publish

Build a portable Windows folder and ZIP:

```powershell
.\publish-winui.ps1
```

The output is written to `artifacts\Spek.WinUI-win-x64-portable`, with a ZIP
next to it.
By default the script builds the native FFmpeg core, publishes the WinUI app,
copies `spek-native.dll` plus its FFmpeg/MinGW dependency closure, and bundles
the .NET runtime. Use `-FrameworkDependent` if you prefer a smaller output that
requires the target PC to have the matching .NET runtime installed.

Publishing profiles:

- `.\publish-winui.ps1`: largest, most portable; bundles .NET and Windows App SDK.
- `.\publish-winui.ps1 -FrameworkDependent`: smaller; requires .NET on the target PC.
- `.\publish-winui.ps1 -FrameworkDependent -UseInstalledWindowsAppRuntime`: smallest; requires .NET and Windows App Runtime on the target PC.

## Features

The WinUI front end currently supports:

- opening audio files from the picker, command line, or drag-and-drop
- saving the rendered spectrogram as PNG
- stream, channel, palette, FFT size, window function, and dB range controls
- Preferences and About dialogs
- update checks through the same `help.spek.cc/version` endpoint used by Spek
- keyboard shortcuts for the original Spek spectrogram controls

Useful shortcuts:

- `Ctrl+O`: open
- `Ctrl+S`: save PNG
- `Ctrl+E`: preferences
- `F1`: Spek website
- `Shift+F1`: about
- `s` / `Shift+S`: next / previous stream
- `c` / `Shift+C`: next / previous channel
- `p` / `Shift+P`: next / previous palette
- `f` / `Shift+F`: next / previous window function
- `w` / `Shift+W`: larger / smaller FFT
- `u` / `Shift+U`: raise / lower upper dB limit
- `l` / `Shift+L`: raise / lower lower dB limit

The default publish profile is unpackaged and self-contained, so the ZIP can be
distributed without requiring a separate Windows App SDK or .NET runtime
installation.

## Native FFmpeg Core

The WinUI app first looks for `spek-native.dll` next to `Spek.WinUI.exe`. When it
is present, the app uses the original Spek FFmpeg/FFT pipeline through that DLL.
When it is missing, the app falls back to the managed NAudio analyzer.

See `native/README.md` for the native DLL build notes.
