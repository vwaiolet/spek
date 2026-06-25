# Spek native core

This folder builds `spek-native.dll`, a small C ABI wrapper over the original
Spek FFmpeg/FFT pipeline. The WinUI app will use this DLL when it is present in
the application output folder, then fall back to the managed NAudio analyzer if
the DLL is missing.

## Build

Install MSYS2, then install the MinGW64 toolchain and FFmpeg development
libraries:

```powershell
winget install --id MSYS2.MSYS2 --exact --accept-package-agreements --accept-source-agreements
C:\msys64\usr\bin\bash.exe -lc "pacman -Syu --noconfirm"
C:\msys64\usr\bin\bash.exe -lc "pacman -S --needed --noconfirm mingw-w64-x86_64-toolchain mingw-w64-x86_64-cmake mingw-w64-x86_64-ninja mingw-w64-x86_64-pkgconf mingw-w64-x86_64-ffmpeg"
```

Build the DLL:

```powershell
C:\msys64\usr\bin\bash.exe -lc "export PATH=/mingw64/bin:$PATH; cd /c/Users/kkh99/Downloads/codes/spek/src-winui/native && cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release && cmake --build build"
```

Copy the resulting `spek-native.dll` and required FFmpeg DLLs next to
`Spek.WinUI.exe`.

This helper copies `spek-native.dll` and its MinGW/FFmpeg dependency closure to
the Debug WinUI output folder:

```powershell
.\copy-runtime-deps.ps1
```
