# Third-Party Notices

This file describes third-party software distributed with the Windows WinUI
build of Spek. It is provided for attribution and licence-compliance tracking;
the actual licence texts are distributed alongside the application in
`LICENSE` and the `licenses` directory.

## Spek

Spek is distributed under the GNU General Public License, version 3. See
`LICENSE` and `lic/GPL`.

Spek is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
PARTICULAR PURPOSE. See the GNU General Public License for details.

The corresponding source for Spek, including the WinUI front end and
`spek-native.dll` wrapper, is the source tree that accompanies the release.

## Primary Runtime Components

| Component | Version | Licence / notice source |
| --- | --- | --- |
| .NET Runtime / Windows Desktop Runtime | 10.0.9 | Microsoft .NET Library terms; see `licenses/dotnet/LICENSE.txt` and `licenses/dotnet/ThirdPartyNotices.txt` |
| Microsoft Windows App SDK | 2.2.0 | Microsoft Windows App SDK terms; see copied NuGet `license.txt` and `NOTICE.txt` files under `licenses/nuget` |
| Microsoft WebView2 Loader | 1.0.3719.77 | NuGet `LICENSE.txt` and `NOTICE.txt`; copied under `licenses/nuget` |
| NAudio packages | 2.3.0 | MIT; see `lic/MIT` |
| System.Numerics.Tensors | 9.0.0 | MIT; see `lic/MIT` |
| FFmpeg via MSYS2 MinGW64 | 8.1.2-1 | GPL-3.0-or-later; see `lic/GPL` and MSYS2 package notices under `licenses/msys2` |

## NuGet Packages

These package versions were resolved by `src-winui/Spek.WinUI` at the time this
notice was prepared. Release ZIPs copy NuGet `license` and `NOTICE` files for
matching resolved packages; the copied folder may also include auxiliary
Windows App SDK package versions present in the local NuGet cache when they are
part of the build/runtime graph.

| Package | Version | Licence metadata |
| --- | --- | --- |
| Microsoft.Web.WebView2 | 1.0.3719.77 | package `LICENSE.txt` |
| Microsoft.Windows.AI.MachineLearning | 2.1.70 | package `license.txt` |
| Microsoft.Windows.SDK.BuildTools | 10.0.26100.4654 | Microsoft Windows SDK licence |
| Microsoft.Windows.SDK.BuildTools.MSIX | 1.7.251221100 | package `sdk_license.txt` |
| Microsoft.WindowsAppSDK | 2.2.0 | package `license.txt` and `NOTICE.txt` |
| Microsoft.WindowsAppSDK.AI | 2.2.3 | package `license.txt` and `NOTICE.txt` |
| Microsoft.WindowsAppSDK.Base | 2.0.3, 2.0.4 | package `license.txt` and `NOTICE.txt` |
| Microsoft.WindowsAppSDK.DWrite | 2.1.0 | package `license.txt` and `NOTICE.txt` |
| Microsoft.WindowsAppSDK.Foundation | 2.0.21, 2.1.0 | package `license.txt` and `NOTICE.txt` |
| Microsoft.WindowsAppSDK.InteractiveExperiences | 2.0.15 | package `license.txt` and `NOTICE.txt` |
| Microsoft.WindowsAppSDK.ML | 2.1.70 | package `license.txt` and `NOTICE.txt` |
| Microsoft.WindowsAppSDK.Runtime | 2.2.0 | package `license.txt` and `NOTICE.txt` |
| Microsoft.WindowsAppSDK.Widgets | 2.0.5 | package `license.txt` and `NOTICE.txt` |
| Microsoft.WindowsAppSDK.WinUI | 2.2.1 | package `license.txt` and `NOTICE.txt` |
| NAudio | 2.3.0 | MIT |
| NAudio.Asio | 2.3.0 | MIT |
| NAudio.Core | 2.3.0 | MIT |
| NAudio.Midi | 2.3.0 | MIT |
| NAudio.Wasapi | 2.3.0 | MIT |
| NAudio.WinForms | 2.3.0 | MIT |
| NAudio.WinMM | 2.3.0 | MIT |
| System.Numerics.Tensors | 9.0.0 | MIT |

## MSYS2 MinGW64 Runtime Packages

The native analyser DLL is built with MSYS2 MinGW64 and FFmpeg. The portable ZIP
copies the MinGW/FFmpeg DLL dependency closure next to `Spek.WinUI.exe`. MSYS2
licence files available on the build machine are copied to `licenses/msys2`.

| Package | Version | Licence metadata |
| --- | --- | --- |
| mingw-w64-x86_64-aom | 3.14.1-1 | BSD-2-Clause |
| mingw-w64-x86_64-brotli | 1.2.0-1 | MIT |
| mingw-w64-x86_64-bzip2 | 1.0.8-3 | custom |
| mingw-w64-x86_64-cairo | 1.18.4-4 | LGPL-2.1-or-later OR MPL-1.1 |
| mingw-w64-x86_64-dav1d | 1.5.3-1 | BSD-2-Clause |
| mingw-w64-x86_64-expat | 2.8.1-2 | MIT |
| mingw-w64-x86_64-ffmpeg | 8.1.2-1 | GPL-3.0-or-later |
| mingw-w64-x86_64-fontconfig | 2.18.1-1 | custom |
| mingw-w64-x86_64-freetype | 2.14.3-1 | GPL-2.0-or-later OR FTL |
| mingw-w64-x86_64-fribidi | 1.0.16-1 | LGPL-2.1-or-later |
| mingw-w64-x86_64-gcc-libs | 16.1.0-5 | GPL-3.0-or-later WITH GCC-exception-3.1 AND LGPL-2.1-or-later |
| mingw-w64-x86_64-gdk-pixbuf2 | 2.44.6-1 | LGPL-2.1-or-later |
| mingw-w64-x86_64-gettext-runtime | 1.0-1 | GPL-3.0-or-later AND LGPL-2.1-or-later |
| mingw-w64-x86_64-glib2 | 2.88.1-1 | LGPL-2.1-or-later |
| mingw-w64-x86_64-gmp | 6.3.0-2 | LGPL3 / GPL |
| mingw-w64-x86_64-gnutls | 3.8.13-2 | GPL-3.0-or-later / LGPL-2.1-or-later |
| mingw-w64-x86_64-graphite2 | 1.3.15-1 | LGPL-2.1-or-later |
| mingw-w64-x86_64-gsm | 1.0.24-1 | custom |
| mingw-w64-x86_64-harfbuzz | 14.2.1-1 | MIT |
| mingw-w64-x86_64-highway | 1.4.0-3 | Apache-2.0 |
| mingw-w64-x86_64-jbigkit | 2.1-5 | GPL-2.0 |
| mingw-w64-x86_64-lame | 3.100-3 | LGPL |
| mingw-w64-x86_64-lcms2 | 2.19.1-1 | MIT AND GPL-3.0-or-later |
| mingw-w64-x86_64-lerc | 4.1.0-1 | Apache-2.0 |
| mingw-w64-x86_64-libbluray | 1.4.1-1 | LGPL-2.1-or-later |
| mingw-w64-x86_64-libdatrie | 0.2.14-1 | LGPL |
| mingw-w64-x86_64-libdeflate | 1.25-1 | MIT |
| mingw-w64-x86_64-libffi | 3.6.0-1 | MIT |
| mingw-w64-x86_64-libgme | 0.6.5-2 | LGPL |
| mingw-w64-x86_64-libiconv | 1.19-1 | LGPL-2.1-or-later; documentation GPL-3.0-or-later |
| mingw-w64-x86_64-libidn2 | 2.3.8-4 | GPL-2.0-or-later / LGPL-3.0-or-later |
| mingw-w64-x86_64-libjpeg-turbo | 3.1.4.1-3 | BSD-like |
| mingw-w64-x86_64-libjxl | 0.11.2-4 | BSD-3-Clause |
| mingw-w64-x86_64-liblc3 | 1.1.3-1 | Apache-2.0 |
| mingw-w64-x86_64-libmodplug | 0.8.9.0-5 | Public Domain |
| mingw-w64-x86_64-libogg | 1.3.6-1 | BSD-3-Clause |
| mingw-w64-x86_64-libpng | 1.6.58-1 | custom |
| mingw-w64-x86_64-librsvg | 2.62.3-1 | LGPL-2.1-or-later |
| mingw-w64-x86_64-libsoxr | 0.1.3-5 | LGPL |
| mingw-w64-x86_64-libssh | 0.12.0-3 | LGPL-2.1-or-later |
| mingw-w64-x86_64-libtasn1 | 4.21.0-1 | GPL3 / LGPL |
| mingw-w64-x86_64-libthai | 0.1.30-1 | LGPL-2.1-or-later |
| mingw-w64-x86_64-libtheora | 1.2.0-1 | BSD-3-Clause |
| mingw-w64-x86_64-libtiff | 4.7.1-1 | MIT |
| mingw-w64-x86_64-libunistring | 1.4.2-1 | LGPL-3.0-or-later OR GPL-3.0-or-later |
| mingw-w64-x86_64-libva | 2.23.0-1 | MIT |
| mingw-w64-x86_64-libvorbis | 1.3.7-2 | custom |
| mingw-w64-x86_64-libvpl | 2.16.0-1 | MIT |
| mingw-w64-x86_64-libvpx | 1.16.0-1 | BSD-3-Clause |
| mingw-w64-x86_64-libwebp | 1.6.0-1 | BSD-3-Clause |
| mingw-w64-x86_64-libwinpthread | 14.0.0.r98.g19f5121a2-1 | MIT AND BSD-3-Clause-Clear |
| mingw-w64-x86_64-libx264 | 0.165.r3222.b35605a-2 | custom |
| mingw-w64-x86_64-libxml2 | 2.15.3-1 | MIT |
| mingw-w64-x86_64-nettle | 3.10.2-1 | GPL-2.0-or-later / LGPL-3.0-or-later |
| mingw-w64-x86_64-opencore-amr | 0.1.6-1 | Apache |
| mingw-w64-x86_64-openjpeg2 | 2.5.4-2 | BSD-2-Clause |
| mingw-w64-x86_64-openssl | 3.6.3-1 | Apache-2.0 |
| mingw-w64-x86_64-opus | 1.6.1-1 | BSD-3-Clause |
| mingw-w64-x86_64-p11-kit | 0.26.2-1 | BSD-3-Clause |
| mingw-w64-x86_64-pango | 1.57.1-1 | LGPL-2.1 |
| mingw-w64-x86_64-pcre2 | 10.47-1 | BSD-3-Clause |
| mingw-w64-x86_64-pixman | 0.46.4-3 | MIT |
| mingw-w64-x86_64-rav1e | 0.8.1-1 | BSD-2-Clause |
| mingw-w64-x86_64-rtmpdump | 2.6-1 | GPL2 / LGPL2.1 |
| mingw-w64-x86_64-speex | 1.2.1-1 | BSD |
| mingw-w64-x86_64-srt | 1.5.5-1 | MPL-2.0 |
| mingw-w64-x86_64-svt-av1 | 4.1.0-1 | BSD-3-Clause-Clear |
| mingw-w64-x86_64-x265 | 4.2-2 | GPL |
| mingw-w64-x86_64-xvidcore | 1.3.7-5 | GPL |
| mingw-w64-x86_64-xz | 5.8.3-1 | 0BSD AND LGPL-2.1-or-later AND GPL-2.0-or-later |
| mingw-w64-x86_64-zlib | 1.3.2-2 | Zlib |
| mingw-w64-x86_64-zstd | 1.5.7-2 | BSD-3-Clause OR GPL-2.0-or-later |
| mingw-w64-x86_64-zvbi | 0.2.44-2 | package metadata |

## Source Locations

- Spek source: the repository source tree distributed with this release.
- Microsoft notices and source-offer details: copied from the resolved NuGet
  packages and .NET installation into `licenses`.
- MSYS2 package metadata and sources: https://packages.msys2.org/.
