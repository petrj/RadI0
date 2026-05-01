# RadI0

<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0.png" width="800" alt="RadI0"/>

**A .NET 10 Software-Defined Radio (SDR) receiver for DAB+ and FM broadcasts**

RadI0 is a cross-platform software-defined radio receiver for DAB+ and FM radio broadcasts, built with .NET 10. It uses an RTL-SDR dongle to capture and demodulate radio signals, providing a terminal-based interface for tuning, playing, recording, and streaming audio.

## Supported Platforms

| Linux (arm) | Linux (arm64) | Linux (x64) | Windows (arm64) | Windows (x64) | Windows (x86) |
| --- | --- | --- | --- | --- | --- |
| [![Linux ARM](https://img.shields.io/badge/Linux-arm-red?logo=linux&logoColor=white)](https://github.com/petrj/RadI0/releases/latest/download/RadI0.v0.0.8.0.linux-arm.tar.xz) | [![Linux ARM64](https://img.shields.io/badge/Linux-arm64-brightgreen?logo=linux&logoColor=white)](https://github.com/petrj/RadI0/releases/latest/download/RadI0.v0.0.8.0.linux-arm64.tar.xz) [![RaspberryPi](https://img.shields.io/badge/RaspberryPi-compatible-red?logo=raspberrypi&logoColor=white)](https://www.raspberrypi.org/) | [![Linux x64](https://img.shields.io/badge/Linux-x64-brightgreen?logo=linux&logoColor=white)](https://github.com/petrj/RadI0/releases/latest/download/RadI0.v0.0.8.0.linux-x64.tar.xz) | [![Windows ARM64](https://img.shields.io/badge/Windows-arm64-blue?logo=windows&logoColor=white)](https://github.com/petrj/RadI0/releases/latest/download/RadI0.v0.0.8.0.win-arm64.7z) | [![Windows x64](https://img.shields.io/badge/Windows-x64-blue?logo=windows&logoColor=white)](https://github.com/petrj/RadI0/releases/latest/download/RadI0.v0.0.8.0.win-x64.7z) | [![Windows x86](https://img.shields.io/badge/Windows-x86-blue?logo=windows&logoColor=white)](https://github.com/petrj/RadI0/releases/latest/download/RadI0.v0.0.8.0.win-x86.7z) |

## Table of Contents
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Usage](#usage)
- [NuGet packages used by RadI0](#nuget-packages-used-by-radi0)
- [Build from Source](#build--install-from-source)
- [License](#license)

## Features

### DAB+ Radio
- OFDM Demodulator (Fast Fourier Transform)
- Viterbi convolution decoding
- Reed–Solomon forward error correction
- FIC channel data parsing
- ADTS stream demodulation
- AAC recording
- PCM recording (using faad2 for converting AAC => PCM)
- UDP streaming support (ADTS aac for DAB+)
- Service number-based tuning
- PAD dynamic label parsing

### FM Radio
- Mono/Stereo FM demodulation
- PCM WAVE recording
- RDS RadioText parsing

### User Interface & Platform
- Terminal-based GUI (Terminal.GUI)
- Cross-platform: Linux & Windows support
- Audio playback via libVLC

## Requirements

### Hardware
- **RTL-SDR Dongle**: RTL2832U-based USB receiver (e.g., RTL-SDR Blog V3, NooElec NESDR, ASTROMETA USB Dongle)
- **Antenna**: Standard telescopic or dipole antenna (suitable for 88-108 MHz FM and 174-240 MHz DAB+)
- **USB Port**: For connecting the RTL-SDR dongle

### Software
- **.NET Runtime**: .NET 10 or later
- **Operating System**: Linux (Ubuntu, Debian, etc.) or Windows
- **External Dependencies**:
  - <a href="https://github.com/osmocom/rtl-sdr">rtl-sdr</a> - RTL-SDR driver library
  - <a href="https://github.com/knik0/faad2">faad2</a> - AAC audio decoder


<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0Logo.png" width="450" alt="RadI0"/>

<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0Scheme.png" width="800" alt="Architecture Scheme"/>

### Remote Access via rtl_tcp

RadI0 supports remote signal processing by connecting to an rtl_tcp instance. This allows you to set up your RTL-SDR dongle and antenna in a location with optimal reception and run the RadI0 receiver interface on a different machine anywhere in the world.

    [!WARNING]
    High Bandwidth Required: Using this feature requires a very stable and high-speed network connection. Because raw I/Q samples are transferred over the network, the data throughput for DAB+ is approximately 30 Mbit/s. Ensure your internet bandwidth can support this sustained rate to avoid signal stuttering or synchronization loss.

## Installation

### From Release Package

  - Linux

    ```bash
    sudo apt-get install libfaad2 rtl-sdr libasound2 libasound2-dev libvlc-dev vlc libvlc-bin libvlc-dev
    ```

    - extract release archive package (tar.xz)

  - Windows
    - install RTL2832U driver (Zadig)
    - download rtl-sdr windows binaries (<a href="https://ftp.osmocom.org/binaries/windows/rtl-sdr/">https://ftp.osmocom.org/binaries/windows/rtl-sdr/</a>)
    - download (or build from source) `libfaad2.dll`
    - Modify PATH varible (or copy `libfaad2.dll` and `rtl-sdr` to suitable folder) to make the libraries visible
      ( I'm using this Windows folder `c:\users\petrj\\.dotnet\Tools` with these files:
        `libfaad2.dll`
        `libfaad2_dll.dll`
        `librtlsdr.dll`
        `libusb-1.0.dll`
        `libwinpthread-1.dll`
        `rtl_tcp.exe`)
    - extract release archive package (7z)

## Usage

### Console Commands

#### DAB+

Tune 8D frequency

```
./RadI0 -dab -f 8D
```

Tune 8C frequency and play radio corresponding to service number 1175:

```
./RadI0 -dab -f 8C -sn "1175"
```

Tune 8C frequency, play radio corresponding to service number 1175 and save audio to PCM wave:

```
./RadI0 -dab -f 8C -sn "1175" -wave /tmp/radio.wave
```

Stream DAB audio to UDP:

```
./RadI0 -f 8C -sn 1175 -udp 127.0.0.1:8020
```

> **Note:** The stream (ADTS aac format) can be played by VLC or mplayer:
> ```
> cvlc udp://@:8020 :demux=aac
> mplayer -nocache -demuxer aac udp://127.0.0.1:8020
> ```

Export DAB audio to AAC file:

```
./RadI0 -f 7C -sn 3889 -aac MyDABRadioRecord.aac
```

#### FM

Tune and play 104 MHz

```
./RadI0 -fm -f "104 MHz"
```

Export FM audio to WAVE file:

```
./RadI0 -fm -if FM.raw -wave MyFMRadioRecord.wave
```

## NuGet packages used by RadI0

RadI0 builds on several public library packages from this repository. These packages are publicly available and can be reused in other .NET projects for RTL-SDR hardware access, audio playback, FM demodulation, DAB+ decoding, and shared common functionality.

### [RTLSDR](https://github.com/users/petrj/packages/nuget/package/RTLSDR)
- Provides low-level RTL-SDR driver support, USB dongle control, and device state handling.

### [RTLSDR.Common](https://github.com/users/petrj/packages/nuget/package/RTLSDR.Common)
- Provides shared DSP utilities, audio data structures, thread worker helpers, and common event types.

### [RTLSDR.Audio](https://github.com/users/petrj/packages/nuget/package/RTLSDR.Audio)
- Provides audio playback and buffering abstractions for different platforms, plus AAC decoding support.

### [RTLSDR.FM](https://github.com/users/petrj/packages/nuget/package/RTLSDR.FM)
- Provides FM demodulation and stereo decoding.

### [RTLSDR.DAB](https://github.com/users/petrj/packages/nuget/package/RTLSDR.DAB)
- Provides DAB+ signal processing, ensemble and service discovery, AAC frame extraction, and DAB service handling.

## Build & Install from Source

### GitHub Packages repository

1. Create a Personal Access Token on GitHub

   * Go to: https://github.com/settings/tokens
   * Create a token (classic) with at least: `read:packages` scope

2. Add the NuGet source:

```bash
dotnet nuget add source \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_PERSONAL_ACCESS_TOKEN \
  --store-password-in-clear-text \
  --name github \
  "https://nuget.pkg.github.com/petrj/index.json"
```

### Manual build

```bash
dotnet restore
```

```bash
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r linux-arm64
dotnet publish -c Release -r linux-arm
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r win-x86
dotnet publish -c Release -r win-arm64
```

### Powershell build


## Linux

Ask user:

```bash
pwsh ./Clear.ps1
```

Select runtime:

```bash
pwsh ./MakeRelease.ps1
```

```bash
Available Runtimes:
1) linux-x64
2) linux-arm64
3) linux-arm
4) win-x64
5) win-x86
6) win-arm64
Select Runtime [default: 1]:
```

Specific runtime:

```bash
pwsh ./MakeRelease.ps1 -Runtime linux-x64
```

All available runtimes:

```bash
pwsh ./MakeRelease.ps1 -Clear -AllRuntimes
```


## Windows

```bash
./MakeRelease.ps1 -Clear -AllRuntimes
```
## Screenshots

<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-2.png" width="800" alt="Main Interface with Station List"/>
<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-3.png" width="800" alt="Tuning and Statistics View"/>
<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-4.png" width="800" alt="Spectrum Analyzer"/>
<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-5.png" width="800" alt="Menu"/>

## License

GNU GPL v2. See the <a href="https://raw.github.com/petrj/RTL-SDR-Receiver/master/LICENSE">LICENSE</a> file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests to improve RadI0.

## Related Resources

- [RTL-SDR Community](https://www.rtl-sdr.com/)
- [RTL-SDR GitHub](https://github.com/osmocom/rtl-sdr)
- [github - welle.io](https://github.com/AlbrechtL/welle.io/)
- [etsi.org DAB documentation ](https://www.etsi.org/deliver/etsi_en/300400_300499/300401/02.01.01_60/en_300401v020101p.pdf)
- [ETSI TS 102 563 - AAC Superframe](https://www.etsi.org/deliver/etsi_ts/102500_102599/102563/02.01.01_60/ts_102563v020101p.pdf)
- [ETSI TS 101 756 - EBU Encoding](https://www.etsi.org/deliver/etsi_ts/101700_101799/101756/02.04.01_60/ts_101756v020401p.pdf)
- [Complex Numbers Algebra](https://www.karlin.mff.cuni.cz/~portal/komplexni_cisla/?page=algebraicky-tvar-operace)
- [libfec - Reed-Solomon](https://github.com/quiet/libfec/tree/master)
- [Accord-NET Fourier Transform](https://github.com/Azure/Accord-NET/blob/master/Sources/Accord.Math/Transforms/FourierTransform2.cs)
- [Tektronix RF Power Calculation](https://www.tek.com/en/blog/calculating-rf-power-iq-samples)
- [rtl_tcp tool](https://hz.tools/rtl_tcp/)
- [rtl-sdr rtl_fm.c](https://github.com/osmocom/rtl-sdr/blob/master/src/rtl_fm.c)
