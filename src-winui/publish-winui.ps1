param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$Runtime = "win-x64",
    [string]$MsysRoot = "C:\msys64",
    [switch]$FrameworkDependent,
    [switch]$UseInstalledWindowsAppRuntime,
    [switch]$SkipNativeBuild,
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$Project = Join-Path $ScriptDir "Spek.WinUI\Spek.WinUI.csproj"
$NativeDir = Join-Path $ScriptDir "native"
$ArtifactsDir = Join-Path $ScriptDir "artifacts"
$Flavor = if ($FrameworkDependent -and $UseInstalledWindowsAppRuntime) {
    "runtime-dependent"
} elseif ($FrameworkDependent) {
    "compact"
} else {
    "portable"
}
$PublishDir = Join-Path $ArtifactsDir "Spek.WinUI-$Runtime-$Flavor"
$ZipPath = Join-Path $ArtifactsDir "Spek.WinUI-$Runtime-$Flavor.zip"
$Bash = Join-Path $MsysRoot "usr\bin\bash.exe"
$MingwBin = Join-Path $MsysRoot "mingw64\bin"
$MingwLicenses = Join-Path $MsysRoot "mingw64\share\licenses"

function Convert-ToMsysPath {
    param([string]$Path)

    $FullPath = [System.IO.Path]::GetFullPath($Path)
    if ($FullPath -match "^([A-Za-z]):\\(.*)$") {
        $Drive = $Matches[1].ToLowerInvariant()
        $Tail = $Matches[2] -replace "\\", "/"
        return "/$Drive/$Tail"
    }

    return $FullPath -replace "\\", "/"
}

function Copy-IfExists {
    param(
        [string]$LiteralPath,
        [string]$Destination
    )

    if (Test-Path -LiteralPath $LiteralPath) {
        Copy-Item -LiteralPath $LiteralPath -Destination $Destination -Recurse -Force
    }
}

function Copy-NuGetPackageNotices {
    param([string]$Destination)

    $PackagesRoot = Join-Path $env:USERPROFILE ".nuget\packages"
    if (-not (Test-Path -LiteralPath $PackagesRoot)) {
        return
    }

    $PackageNamePatterns = @(
        "microsoft.web.webview2",
        "microsoft.windows.ai.machinelearning",
        "microsoft.windows.sdk.*",
        "microsoft.windowsappsdk*",
        "naudio*",
        "system.numerics.tensors"
    )

    Get-ChildItem -LiteralPath $PackagesRoot -Directory |
        Where-Object {
            $PackageRootName = $_.Name
            $PackageNamePatterns | Where-Object { $PackageRootName -like $_ }
        } |
        ForEach-Object {
            $PackageName = $_.Name
            Get-ChildItem -LiteralPath $_.FullName -Directory | ForEach-Object {
                $PackageVersion = $_.Name
                $PackageDir = $_.FullName
                $NoticeFiles = Get-ChildItem -LiteralPath $PackageDir -File |
                    Where-Object { $_.Name -match "^(license|notice|sdk_license).*\.(txt|md)$" -or $_.Name -match "^(LICENSE|NOTICE).*\.(txt|md)$" }
                if ($NoticeFiles) {
                    $NoticeDir = Join-Path $Destination "$PackageName-$PackageVersion"
                    New-Item -ItemType Directory -Force -Path $NoticeDir | Out-Null
                    $NoticeFiles | ForEach-Object {
                        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $NoticeDir $_.Name) -Force
                    }
                }
            }
        }
}

function Copy-LicenseNotices {
    $LicensesDir = Join-Path $PublishDir "licenses"
    $NuGetNoticesDir = Join-Path $LicensesDir "nuget"
    $DotNetNoticesDir = Join-Path $LicensesDir "dotnet"
    $SpekLicensesDir = Join-Path $LicensesDir "spek"
    $MsysLicensesDir = Join-Path $LicensesDir "msys2"

    New-Item -ItemType Directory -Force -Path $LicensesDir | Out-Null
    Copy-IfExists (Join-Path $RepoRoot "LICENSE") (Join-Path $PublishDir "LICENSE")
    Copy-IfExists (Join-Path $RepoRoot "THIRD-PARTY-NOTICES.md") (Join-Path $PublishDir "THIRD-PARTY-NOTICES.md")
    Copy-IfExists (Join-Path $RepoRoot "CREDITS.md") (Join-Path $PublishDir "CREDITS.md")
    Copy-IfExists (Join-Path $RepoRoot "lic") $SpekLicensesDir

    $DotNetExe = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
    if ($DotNetExe) {
        $DotNetRoot = Split-Path -Parent $DotNetExe
        New-Item -ItemType Directory -Force -Path $DotNetNoticesDir | Out-Null
        Copy-IfExists (Join-Path $DotNetRoot "LICENSE.txt") (Join-Path $DotNetNoticesDir "LICENSE.txt")
        Copy-IfExists (Join-Path $DotNetRoot "ThirdPartyNotices.txt") (Join-Path $DotNetNoticesDir "ThirdPartyNotices.txt")
    }

    New-Item -ItemType Directory -Force -Path $NuGetNoticesDir | Out-Null
    Copy-NuGetPackageNotices $NuGetNoticesDir

    if (Test-Path -LiteralPath $MingwLicenses) {
        Copy-Item -LiteralPath $MingwLicenses -Destination $MsysLicensesDir -Recurse -Force
    }
}

if (-not (Test-Path -LiteralPath $Project)) {
    throw "Project not found: $Project"
}

if (-not $SkipNativeBuild) {
    if (-not (Test-Path -LiteralPath $Bash)) {
        throw "MSYS2 bash not found: $Bash"
    }

    $NativeMsysPath = Convert-ToMsysPath $NativeDir
    $NativeBuildCommand = "export PATH=/mingw64/bin:`$PATH; cd '$NativeMsysPath' && cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release && cmake --build build"
    & $Bash -lc $NativeBuildCommand
}

if (Test-Path -LiteralPath $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

dotnet restore $Project

$SelfContained = if ($FrameworkDependent) { "false" } else { "true" }
$WindowsAppSDKSelfContained = if ($UseInstalledWindowsAppRuntime) { "false" } else { "true" }
dotnet publish $Project `
    -c $Configuration `
    -r $Runtime `
    -o $PublishDir `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=$WindowsAppSDKSelfContained `
    -p:SelfContained=$SelfContained `
    -p:PublishSingleFile=false

$BuildOutputDir = Join-Path $ScriptDir "Spek.WinUI\bin\$Configuration\net10.0-windows10.0.19041.0\$Runtime"
$AppPri = Join-Path $BuildOutputDir "Spek.WinUI.pri"
if (Test-Path -LiteralPath $AppPri) {
    Copy-Item -LiteralPath $AppPri -Destination (Join-Path $PublishDir "Spek.WinUI.pri") -Force
}

& (Join-Path $NativeDir "copy-runtime-deps.ps1") -OutputDir $PublishDir -MingwBin $MingwBin | Out-Host

Copy-LicenseNotices

if (-not $SkipZip) {
    if (Test-Path -LiteralPath $ZipPath) {
        Remove-Item -LiteralPath $ZipPath -Force
    }

    Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $ZipPath -CompressionLevel Optimal
}

[pscustomobject]@{
    PublishDir = $PublishDir
    ZipPath = if ($SkipZip) { $null } else { $ZipPath }
    Runtime = $Runtime
    Configuration = $Configuration
    Flavor = $Flavor
    SelfContained = -not $FrameworkDependent
    WindowsAppSDKSelfContained = -not $UseInstalledWindowsAppRuntime
}
