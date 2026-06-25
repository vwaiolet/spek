# Spek

Spek is a Windows-only WinUI 3 acoustic spectrum analyser.

The user interface is built with the Windows App SDK. Audio analysis uses
`spek-native.dll`, a small C ABI wrapper over Spek's FFmpeg/FFT core. If the
native DLL is not present, the WinUI app can fall back to a managed NAudio
analyser.

## Project Layout

- `src-winui/Spek.WinUI`: WinUI 3 desktop app.
- `src-winui/native`: CMake project that builds `spek-native.dll`.
- `src`: native audio, FFT, palette, and pipeline core used by the DLL.
- `tests/samples`: audio fixtures used for manual validation.

The repository is trimmed to the Windows WinUI application, its native analysis
core, and sample audio files. Current distribution is ZIP-based.

## Requirements

- Windows 10 version 1809 or newer.
- .NET SDK compatible with `net10.0-windows10.0.19041.0`.
- MSYS2 MinGW64 with CMake, Ninja, pkgconf, and FFmpeg development packages for
  building the native DLL.

## Build

```powershell
dotnet restore .\src-winui\Spek.WinUI.slnx
dotnet build .\src-winui\Spek.WinUI\Spek.WinUI.csproj -c Debug -r win-x64
```

## Publish

```powershell
powershell -ExecutionPolicy Bypass -File .\src-winui\publish-winui.ps1
```

The publish script builds the native FFmpeg core, publishes the WinUI app,
copies the required native DLL dependency closure, and writes a portable ZIP to
`src-winui\artifacts`.
