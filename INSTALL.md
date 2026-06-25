# Installation

Spek is now distributed as a Windows-only WinUI 3 desktop app.

## Portable ZIP

Publish the app:

```powershell
powershell -ExecutionPolicy Bypass -File .\src-winui\publish-winui.ps1
```

Unzip `src-winui\artifacts\Spek.WinUI-win-x64-portable.zip` and run
`Spek.WinUI.exe`.

## Local Build

Install the .NET SDK and MSYS2 MinGW64 dependencies described in
`src-winui\native\README.md`, then run:

```powershell
dotnet restore .\src-winui\Spek.WinUI.slnx
dotnet build .\src-winui\Spek.WinUI\Spek.WinUI.csproj -c Debug -r win-x64
```
