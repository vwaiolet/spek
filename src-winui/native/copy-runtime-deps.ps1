param(
    [string]$OutputDir = "..\Spek.WinUI\bin\Debug\net10.0-windows10.0.19041.0\win-x64",
    [string]$MingwBin = "C:\msys64\mingw64\bin"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$NativeDll = Join-Path $ScriptDir "build\spek-native.dll"
$Objdump = Join-Path $MingwBin "objdump.exe"
if ([System.IO.Path]::IsPathRooted($OutputDir)) {
    $OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
} else {
    $OutputDir = [System.IO.Path]::GetFullPath((Join-Path $ScriptDir $OutputDir))
}

if (-not (Test-Path -LiteralPath $NativeDll)) {
    throw "Native DLL not found: $NativeDll"
}
if (-not (Test-Path -LiteralPath $Objdump)) {
    throw "objdump not found: $Objdump"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
Copy-Item -LiteralPath $NativeDll -Destination (Join-Path $OutputDir "spek-native.dll") -Force

$SystemDlls = @(
    "ADVAPI32.dll",
    "bcrypt.dll",
    "CRYPT32.dll",
    "GDI32.dll",
    "IMM32.dll",
    "KERNEL32.dll",
    "MF.dll",
    "MFPlat.DLL",
    "MFReadWrite.dll",
    "msvcrt.dll",
    "ole32.dll",
    "OLEAUT32.dll",
    "PROPSYS.dll",
    "SETUPAPI.dll",
    "SHELL32.dll",
    "SHLWAPI.dll",
    "USER32.dll",
    "VERSION.dll",
    "WINMM.dll",
    "WS2_32.dll",
    "api-ms-win-core-synch-l1-2-0.dll"
)

$Seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$Queue = [System.Collections.Generic.Queue[string]]::new()
$Queue.Enqueue((Join-Path $OutputDir "spek-native.dll"))

while ($Queue.Count -gt 0) {
    $Dll = $Queue.Dequeue()
    $Imports = & $Objdump -p $Dll |
        Select-String "DLL Name:" |
        ForEach-Object { ($_ -replace "^.*DLL Name:\s*", "").Trim() }

    foreach ($Name in $Imports) {
        if ($SystemDlls -contains $Name) {
            continue
        }
        if (-not $Seen.Add($Name)) {
            continue
        }

        $Source = Join-Path $MingwBin $Name
        if (Test-Path -LiteralPath $Source) {
            $Target = Join-Path $OutputDir $Name
            Copy-Item -LiteralPath $Source -Destination $Target -Force
            $Queue.Enqueue($Target)
        }
    }
}

Get-ChildItem -LiteralPath $OutputDir -Filter "*.dll" |
    Sort-Object Name |
    Select-Object Name, Length
