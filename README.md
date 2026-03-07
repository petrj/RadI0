# RadI0

<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0.png" width="800" alt="RadI0"/>

<i>.NET 10 DAB+/FM radio</i>

- DAB+

  - OFDM Demodulator (Fast Fourier Transform)
  - Viterbi convolution decoding
  - Reed–Solomon forward error correction
  - FIC channnel data parsing
  - demodulating to ADTS stream

- FM

  - Mono/Stereo FM demodulator

- UI
  - Terminal.GUI (Console)

- OS
  - Linux
  - Windows

- Audio
  - using libVLC

  - DAB
    - recording to AAC
    - recording to PCM WAVE
      - for AAC => PCM decoding using faad2
    - streaming to UDP
  - FM
    - recording to PCM WAVE

- External dependencies (not included in this repo):
  - <a href="https://github.com/osmocom/rtl-sdr">rtl-sdr</a>
  - <a href="https://github.com/knik0/faad2">faad2</a> for AAC decoding from aac to PCM

<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0Scheme.png" width="800" alt="Scheme"/>

- Installation from release package:

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

- Console usage:
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

          stream (ADTS aac format) can be played by VLC or mplayer:
          # cvlc udp://@:8020 :demux=aac
          # mplayer -nocache -demuxer aac udp://127.0.0.1:8020

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

- Build & install from source:

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


<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-2.png" width="800" alt="RadI0"/>
<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-3.png" width="800" alt="RadI0"/>
<img src="https://raw.github.com/petrj/RTL-SDR-Receiver/master/Graphics/RadI0-4.png" width="800" alt="RadI0"/>