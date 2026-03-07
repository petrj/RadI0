# RadI0

<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0.png" width="800" alt="RadI0"/>

**A .NET 10 Software-Defined Radio (SDR) receiver for DAB+ and FM broadcasts**

RadI0 is a cross-platform software-defined radio receiver for DAB+ and FM radio broadcasts, built with .NET 10. It uses an RTL-SDR dongle to capture and demodulate radio signals, providing a terminal-based interface for tuning, playing, recording, and streaming audio. Perfect for enthusiasts exploring digital and analog radio without dedicated hardware.

## Table of Contents
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Usage](#usage)
- [Build from Source](#build--install-from-source)
- [Troubleshooting](#troubleshooting)
- [License](#license)

## Features

### DAB+ Radio
- OFDM Demodulator (Fast Fourier Transform)
- Viterbi convolution decoding
- Reed–Solomon forward error correction
- FIC channel data parsing
- ADTS stream demodulation
- AAC and PCM WAVE recording
- UDP streaming support
- Service number-based tuning

### FM Radio
- Mono/Stereo FM demodulation
- PCM WAVE recording

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
  - libVLC - Audio playback

<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0Scheme.png" width="800" alt="Architecture Scheme"/>

## Installation

### From Release Package

  - Linux
    - sudo apt-get install libfaad2 rtl-sdr libasound2 libasound2-dev libvlc-dev
    - extract release zip package

  - Windows
    - install RTL2832U driver (Zadig)
    - download rtl-sdr windows binaries (<a href="https://ftp.osmocom.org/binaries/windows/rtl-sdr/">https://ftp.osmocom.org/binaries/windows/rtl-sdr/</a>)
    - download (or build from source) libfaad2.dll
    - Modify PATH varible (or copy libfaad2.dll and rtl-sdr to suitable folder) to make the libraries visible
      ( I'm using this windows folder "c:\users\petrj\\.dotnet\Tools" with theese files:
        libfaad2.dll
        libfaad2_dll.dll
        librtlsdr.dll
        libusb-1.0.dll
        libwinpthread-1.dll
        rtl_tcp.exe)
    - extract release zip package

## Usage

### Console Commands
    - DAB+

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

        The stream (ADTS aac format) can be played by VLC or mplayer:
        ```
        cvlc udp://@:8020 :demux=aac
        mplayer -nocache -demuxer aac udp://127.0.0.1:8020
        ```

      Export DAB audio to AAC file:
      ```
      ./RadI0 -f 7C -sn 3889 -aac MyDABRadioRecord.aac
      ```

    - FM

      Tune and play 104 MHz
      ```
      ./RadI0 -fm -f "104 MHz"
      ```

      Export FM audio to WAVE file:
      ```
      ./RadI0 -fm -if FM.raw -wave MyFMRadioRecord.wave
      ```

## Build & Install from Source

    - Linux:

      ```
      sudo mkdir -p /opt/RadI0
      pwsh ./Clear.ps1
      pwsh ./MakeRelease.ps1
      pwsh ./Install.ps1
      ```


    - Windows:

      ```
      ./Clear.ps1
      ./MakeRelease.ps1
      ./Install.ps1
      ```


## Screenshots

**Main Interface - Station List**
<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-2.png" width="800" alt="Main Interface with Station List"/>

**Tuning and Statistics**
<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-3.png" width="800" alt="Tuning and Statistics View"/>

**Spectrum Analyzer**
<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-4.png" width="800" alt="Spectrum Analyzer"/>

## Troubleshooting

### Common Issues

**RTL-SDR device not found**
- Ensure the RTL-SDR dongle is connected and recognized by your system
- On Linux, check permissions: `lsusb` should show your device
- On Windows, install the RTL2832U driver using Zadig

**No audio playback**
- Verify libVLC is installed and accessible
- Check that your audio device is properly connected and configured

**Build errors with missing dependencies**
- Ensure all external dependencies are installed
- On Linux, run the installation commands in the Installation section
- On Windows, verify PATH variables point to the correct library locations

## License

This project is licensed under the MIT License. See the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests to improve RadI0.

## Related Resources

- [RTL-SDR Community](https://www.rtl-sdr.com/)
- [RTL-SDR GitHub](https://github.com/osmocom/rtl-sdr)
- [DAB+ Digital Radio](https://www.dab.org/)