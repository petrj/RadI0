using System;
using LoggerService;

namespace RTLSDR.FM
{
    /// <summary>
    /// Decodes RDS (Radio Data System) data from FM baseband.
    /// Processes raw IQ data through its own FM demodulation pipeline at a higher
    /// intermediate sample rate (~200 kHz) to preserve the 57 kHz RDS subcarrier,
    /// then uses I/Q downconversion to extract the RDS BPSK signal.
    /// </summary>
    public class RDSDecoder
    {
        // RDS constants
        private const double RDS_SUBCARRIER_FREQ = 57000.0;
        private const double RDS_SYMBOL_RATE = 1187.5;

        // CRC generator polynomial: x^10 + x^8 + x^7 + x^5 + x^4 + x^3 + 1
        private const ushort CRC_POLY = 0x1B9;

        // Offset words (10-bit) for block identification
        private const ushort OFFSET_A = 0x0FC;
        private const ushort OFFSET_B = 0x198;
        private const ushort OFFSET_C = 0x168;
        private const ushort OFFSET_CP = 0x350;
        private const ushort OFFSET_D = 0x1B4;

        // Internal FM demod state (separate from audio path)
        private short _rdsNowR = 0, _rdsNowJ = 0;
        private int _rdsPrevIndex = 0;
        private short _rdsPreR = 0, _rdsPreJ = 0;

        // RDS intermediate sample rate after downsampling from ~1 MHz
        private const int RDS_INTERMEDIATE_RATE = 200000;
        private const int RDS_DOWNSAMPLE = 5;

        // Buffers
        private readonly short[] _rdsIQBuffer;
        private readonly short[] _rdsBasebandBuffer;

        // I/Q mixer: shift 57 kHz → DC (avoids bandpass selectivity issues)
        private double _mixerPhase = 0.0;
        private readonly double _mixerPhaseInc;

        // 4th-order LP filters (2 cascaded biquads) for I and Q after mixing
        // Tight cutoff to isolate RDS from stereo subcarrier leakage
        private readonly BiquadFilter _lpI1, _lpI2;
        private readonly BiquadFilter _lpQ1, _lpQ2;

        // Costas loop for BPSK carrier phase tracking (operates at baseband)
        private double _costasPhase = 0.0;
        private double _costasInteg = 0.0;
        private const double COSTAS_KP = 0.04;
        private const double COSTAS_KI = 0.001;

        // Symbol timing with Gardner TED
        private double _symbolPhase = 0.0;
        private readonly double _symbolPhaseInc;
        private double _prevDecisionSample = 0.0;
        private double _prevMidSample = 0.0;
        private bool _pastHalf = false;
        private const double TIMING_GAIN = 0.005;

        // Bit accumulation
        private uint _bitBuffer = 0;
        private int _bitCount = 0;

        // Block sync
        private int _blockIndex = -1; // -1 = unsynced, 0=A, 1=B, 2=C, 3=D
        private bool _blockSynced = false;
        private int _goodBlockCount = 0;
        private int _badBlockCount = 0;

        // Group data
        private readonly ushort[] _groupData = new ushort[4];

        // Differential decoding
        private int _prevBit = 0;

        // Parsed RDS data
        private readonly RDSData _rdsData = new RDSData();
        private bool _rdsDataChanged = false;

        // PS name assembly (8 chars, 4 segments of 2 chars)
        private readonly char[] _psChars = new char[8];
        private int _psSegmentMask = 0;

        // Radio Text assembly (64 chars max)
        private readonly char[] _rtChars = new char[64];
        private int _rtSegmentMask = 0;
        private int _rtAbFlag = -1;

        // Diagnostics
        private int _totalBitsProcessed = 0;
        private DateTime _lastDiagTime = DateTime.MinValue;

        private readonly ILoggingService? _loggingService;

        /// <summary>
        /// Gets the current parsed RDS data.
        /// </summary>
        public RDSData Data => _rdsData;

        /// <summary>
        /// Returns true if new RDS data has been decoded since the last check.
        /// Resets the flag after reading.
        /// </summary>
        public bool HasNewData
        {
            get
            {
                if (_rdsDataChanged)
                {
                    _rdsDataChanged = false;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets whether the RDS decoder is synchronized to the data stream.
        /// </summary>
        public bool Synced => _blockSynced;

        /// <summary>
        /// Gets the count of successfully decoded blocks.
        /// </summary>
        public int GoodBlockCount => _goodBlockCount;

        public RDSDecoder(ILoggingService? loggingService)
        {
            _loggingService = loggingService;

            _rdsIQBuffer = new short[50000];
            _rdsBasebandBuffer = new short[25000];

            // I/Q mixer: 57 kHz phase increment at 200 kHz sample rate
            _mixerPhaseInc = 2.0 * Math.PI * RDS_SUBCARRIER_FREQ / RDS_INTERMEDIATE_RATE;

            // 4th-order LP at 1800 Hz (covers ±1187.5 Hz RDS bandwidth with margin)
            // Two cascaded 2nd-order Butterworth sections
            _lpI1 = BiquadFilter.Lowpass(RDS_INTERMEDIATE_RATE, 1800, 0.707);
            _lpI2 = BiquadFilter.Lowpass(RDS_INTERMEDIATE_RATE, 1800, 0.707);
            _lpQ1 = BiquadFilter.Lowpass(RDS_INTERMEDIATE_RATE, 1800, 0.707);
            _lpQ2 = BiquadFilter.Lowpass(RDS_INTERMEDIATE_RATE, 1800, 0.707);

            // Symbol timing increment
            _symbolPhaseInc = RDS_SYMBOL_RATE / RDS_INTERMEDIATE_RATE;

            for (int i = 0; i < 8; i++) _psChars[i] = ' ';
            for (int i = 0; i < 64; i++) _rtChars[i] = ' ';
        }

        /// <summary>
        /// Processes raw IQ data from the SDR. Performs its own FM demodulation
        /// at a sample rate high enough to preserve the 57 kHz RDS subcarrier.
        /// </summary>
        public void ProcessIQData(byte[] iqData, int length)
        {
            // Step 1: Low-pass + downsample IQ data to ~200 kHz
            int iqCount = LowPassRDS(iqData, _rdsIQBuffer, length);

            if (iqCount < 4) return;

            // Step 2: FM demodulate to get baseband at ~200 kHz
            int basebandCount = FMDemodulateRDS(_rdsIQBuffer, _rdsBasebandBuffer, iqCount);

            if (basebandCount < 2) return;

            // Step 3: Extract RDS via I/Q downconversion + Costas loop
            for (int i = 0; i < basebandCount; i++)
            {
                double sample = _rdsBasebandBuffer[i] / 16384.0;

                // Mix down: shift 57 kHz RDS subcarrier to DC
                double cosM = Math.Cos(_mixerPhase);
                double sinM = Math.Sin(_mixerPhase);
                double mixI = sample * cosM;
                double mixQ = sample * (-sinM);
                _mixerPhase += _mixerPhaseInc;
                if (_mixerPhase > 2.0 * Math.PI) _mixerPhase -= 2.0 * Math.PI;

                // 4th-order low-pass: removes stereo, audio, and all non-RDS content
                double filtI = _lpI2.Process(_lpI1.Process(mixI));
                double filtQ = _lpQ2.Process(_lpQ1.Process(mixQ));

                // Costas loop: track residual carrier phase for BPSK demodulation
                double cosP = Math.Cos(_costasPhase);
                double sinP = Math.Sin(_costasPhase);
                double corrI = filtI * cosP + filtQ * sinP;
                double corrQ = -filtI * sinP + filtQ * cosP;

                // BPSK phase error detector
                double phaseErr = corrQ * Math.Sign(corrI + 1e-30);
                _costasInteg += COSTAS_KI * phaseErr;
                _costasPhase += COSTAS_KP * phaseErr + _costasInteg;
                if (_costasPhase > Math.PI) _costasPhase -= 2.0 * Math.PI;
                if (_costasPhase < -Math.PI) _costasPhase += 2.0 * Math.PI;

                // Symbol timing with Gardner TED
                double dataSignal = corrI;
                double prevPhase = _symbolPhase;
                _symbolPhase += _symbolPhaseInc;

                // Capture mid-symbol sample (at phase ≈ 0.5)
                if (!_pastHalf && _symbolPhase >= 0.5 && _symbolPhase < 1.0)
                {
                    _prevMidSample = dataSignal;
                    _pastHalf = true;
                }

                if (_symbolPhase >= 1.0)
                {
                    _symbolPhase -= 1.0;
                    _pastHalf = false;

                    // Gardner timing error: e = (x[n] - x[n-1]) * x_mid
                    double timingErr = (dataSignal - _prevDecisionSample) * _prevMidSample;

                    // Normalize and apply timing correction
                    double norm = Math.Abs(dataSignal) + Math.Abs(_prevDecisionSample) + 1e-10;
                    _symbolPhase += TIMING_GAIN * (timingErr / norm);

                    // Clamp to prevent runaway
                    if (_symbolPhase < -0.5) _symbolPhase = -0.5;
                    if (_symbolPhase > 0.5) _symbolPhase = 0.5;

                    _prevDecisionSample = dataSignal;

                    // Hard bit decision + differential decode
                    int bit = dataSignal > 0 ? 1 : 0;
                    int decodedBit = bit ^ _prevBit;
                    _prevBit = bit;

                    _totalBitsProcessed++;
                    ProcessBit(decodedBit);
                }
            }

            // Periodic diagnostics
            if ((DateTime.UtcNow - _lastDiagTime).TotalSeconds > 10)
            {
                _loggingService?.Info($"RDS: bits={_totalBitsProcessed}, synced={_blockSynced}, goodBlocks={_goodBlockCount}, badBlocks={_badBlockCount}");
                _lastDiagTime = DateTime.UtcNow;
            }
        }

        private int LowPassRDS(byte[] iqData, short[] result, int count)
        {
            int i = 0, i2 = 0;
            int maxOutput = result.Length - 1;

            while (i < count - 1 && i2 < maxOutput)
            {
                _rdsNowR += (short)(iqData[i] - 127);
                _rdsNowJ += (short)(iqData[i + 1] - 127);
                i += 2;
                _rdsPrevIndex++;

                if (_rdsPrevIndex >= RDS_DOWNSAMPLE)
                {
                    result[i2++] = _rdsNowR;
                    result[i2++] = _rdsNowJ;
                    _rdsPrevIndex = 0;
                    _rdsNowR = 0;
                    _rdsNowJ = 0;
                }
            }

            return i2;
        }

        private int FMDemodulateRDS(short[] iq, short[] output, int count)
        {
            if (count < 4) return 0;

            int maxOutput = output.Length;
            output[0] = PolarDiscriminant(iq[0], iq[1], _rdsPreR, _rdsPreJ);

            int outputCount = 1;
            for (int i = 2; i < count - 1 && outputCount < maxOutput; i += 2)
            {
                output[outputCount++] = PolarDiscriminant(iq[i], iq[i + 1], iq[i - 2], iq[i - 1]);
            }

            _rdsPreR = iq[count - 2];
            _rdsPreJ = iq[count - 1];

            return outputCount;
        }

        private static short PolarDiscriminant(int ar, int aj, int br, int bj)
        {
            var cr = ar * br - aj * (-bj);
            var cj = aj * br + ar * (-bj);
            var angle = Math.Atan2(cj, cr);
            return (short)(angle / Math.PI * (1 << 14));
        }

        private void ProcessBit(int bit)
        {
            _bitBuffer = (_bitBuffer << 1) | (uint)(bit & 1);
            _bitCount++;

            if (!_blockSynced)
            {
                // Search for block A sync by checking every incoming bit
                if (_bitCount >= 26)
                {
                    ushort syndrome = CalculateSyndrome(_bitBuffer & 0x03FFFFFF);

                    if (syndrome == OFFSET_A)
                    {
                        _blockIndex = 0;
                        _groupData[0] = (ushort)((_bitBuffer >> 10) & 0xFFFF);
                        _blockSynced = true;
                        _goodBlockCount = 1;
                        _badBlockCount = 0;
                        _bitCount = 0;
                        _loggingService?.Info("RDS: Block sync acquired");
                    }
                }
            }
            else
            {
                // Synced: collect 26 bits per block
                if (_bitCount >= 26)
                {
                    uint block = _bitBuffer & 0x03FFFFFF;
                    ushort syndrome = CalculateSyndrome(block);
                    ushort data = (ushort)((block >> 10) & 0xFFFF);

                    bool blockOk = false;

                    switch (_blockIndex)
                    {
                        case 0: blockOk = (syndrome == OFFSET_A); break;
                        case 1: blockOk = (syndrome == OFFSET_B); break;
                        case 2: blockOk = (syndrome == OFFSET_C) || (syndrome == OFFSET_CP); break;
                        case 3: blockOk = (syndrome == OFFSET_D); break;
                    }

                    if (blockOk)
                    {
                        _groupData[_blockIndex] = data;
                        _goodBlockCount++;
                        _badBlockCount = 0;
                    }
                    else
                    {
                        _badBlockCount++;
                        if (_badBlockCount > 10)
                        {
                            // Lost sync
                            _blockSynced = false;
                            _blockIndex = -1;
                            _badBlockCount = 0;
                            _goodBlockCount = 0;
                            _bitCount = 0;
                            _loggingService?.Info("RDS: Block sync lost");
                            return;
                        }
                    }

                    int completedBlock = _blockIndex;
                    _blockIndex = (_blockIndex + 1) % 4;
                    _bitCount = 0;

                    // Decode group when block D is completed successfully
                    if (completedBlock == 3 && blockOk)
                    {
                        DecodeGroup(_groupData);
                    }
                }
            }
        }

        private static ushort CalculateSyndrome(uint block)
        {
            uint reg = 0;

            for (int i = 25; i >= 0; i--)
            {
                uint feedback = ((reg >> 9) ^ ((block >> i) & 1)) & 1;
                reg = (reg << 1) & 0x3FF;

                if (feedback != 0)
                {
                    reg ^= CRC_POLY;
                }
            }

            return (ushort)(reg & 0x3FF);
        }

        private void DecodeGroup(ushort[] group)
        {
            ushort blockA = group[0];
            ushort blockB = group[1];
            ushort blockC = group[2];
            ushort blockD = group[3];

            // Block A: PI code
            ushort pi = blockA;

            // Block B: Group type, version, TP, PTY
            int groupType = (blockB >> 12) & 0x0F;
            bool versionB = ((blockB >> 11) & 1) == 1;
            bool tp = ((blockB >> 10) & 1) == 1;
            int pty = (blockB >> 5) & 0x1F;

            _rdsData.PI = pi;
            _rdsData.PTY = pty;
            _rdsData.TP = tp;

            switch (groupType)
            {
                case 0: // Group 0: Basic tuning and switching info + PS name
                    DecodeGroup0(blockB, blockD);
                    break;
                case 2: // Group 2: Radio Text
                    DecodeGroup2(blockB, blockC, blockD, versionB);
                    break;
            }
        }

        private void DecodeGroup0(ushort blockB, ushort blockD)
        {
            // TA flag
            _rdsData.TA = ((blockB >> 4) & 1) == 1;

            // Music/Speech flag (DI segment 3 = MS)
            _rdsData.IsStereo = ((blockB >> 3) & 1) == 1;

            // PS name segment address (2 chars per group, 4 segments total)
            int segmentAddr = blockB & 0x03;

            char c1 = (char)(blockD >> 8);
            char c2 = (char)(blockD & 0xFF);

            if (c1 >= 0x20 && c1 <= 0x7E)
                _psChars[segmentAddr * 2] = c1;
            if (c2 >= 0x20 && c2 <= 0x7E)
                _psChars[segmentAddr * 2 + 1] = c2;

            _psSegmentMask |= (1 << segmentAddr);

            if (_psSegmentMask != 0)
            {
                string ps = new string(_psChars).TrimEnd();
                if (!string.IsNullOrWhiteSpace(ps) && ps != _rdsData.PS)
                {
                    _rdsData.PS = ps;
                    _rdsData.Valid = true;
                    _rdsDataChanged = true;
                    _loggingService?.Info($"RDS PS: '{ps}' PI: 0x{_rdsData.PI:X4}");
                }
            }
        }

        private void DecodeGroup2(ushort blockB, ushort blockC, ushort blockD, bool versionB)
        {
            int abFlag = (blockB >> 4) & 1;
            int segmentAddr = blockB & 0x0F;

            // A/B flag change means new Radio Text message
            if (_rtAbFlag != -1 && _rtAbFlag != abFlag)
            {
                _rtSegmentMask = 0;
                for (int i = 0; i < 64; i++) _rtChars[i] = ' ';
            }
            _rtAbFlag = abFlag;

            if (!versionB)
            {
                // Version A: 4 chars per segment (from blocks C and D)
                int baseIdx = segmentAddr * 4;
                if (baseIdx + 3 < 64)
                {
                    SetRTChar(baseIdx, (char)(blockC >> 8));
                    SetRTChar(baseIdx + 1, (char)(blockC & 0xFF));
                    SetRTChar(baseIdx + 2, (char)(blockD >> 8));
                    SetRTChar(baseIdx + 3, (char)(blockD & 0xFF));
                }
            }
            else
            {
                // Version B: 2 chars per segment (from block D only)
                int baseIdx = segmentAddr * 2;
                if (baseIdx + 1 < 64)
                {
                    SetRTChar(baseIdx, (char)(blockD >> 8));
                    SetRTChar(baseIdx + 1, (char)(blockD & 0xFF));
                }
            }

            _rtSegmentMask |= (1 << segmentAddr);

            string rt = new string(_rtChars).TrimEnd();
            // Check for end-of-text marker (0x0D)
            int endIdx = rt.IndexOf('\r');
            if (endIdx >= 0)
                rt = rt.Substring(0, endIdx);

            rt = rt.TrimEnd();

            if (!string.IsNullOrWhiteSpace(rt) && rt != _rdsData.RadioText)
            {
                _rdsData.RadioText = rt;
                _rdsData.Valid = true;
                _rdsDataChanged = true;
                _loggingService?.Info($"RDS RT: '{_rdsData.RadioText}'");
            }
        }

        private void SetRTChar(int index, char c)
        {
            if (index >= 0 && index < 64 && c >= 0x20 && c <= 0x7E)
            {
                _rtChars[index] = c;
            }
        }

        /// <summary>
        /// Resets the decoder state (use when changing frequency).
        /// </summary>
        public void Reset()
        {
            _blockSynced = false;
            _blockIndex = -1;
            _bitCount = 0;
            _bitBuffer = 0;
            _goodBlockCount = 0;
            _badBlockCount = 0;
            _prevBit = 0;
            _psSegmentMask = 0;
            _rtSegmentMask = 0;
            _rtAbFlag = -1;
            _rdsDataChanged = false;
            _rdsData.Valid = false;
            _rdsData.PS = "";
            _rdsData.RadioText = "";
            _rdsData.PI = 0;
            _rdsData.PTY = 0;
            _rdsData.TP = false;
            _rdsData.TA = false;
            _rdsData.IsStereo = false;

            _rdsNowR = 0;
            _rdsNowJ = 0;
            _rdsPrevIndex = 0;
            _rdsPreR = 0;
            _rdsPreJ = 0;

            _mixerPhase = 0;
            _costasPhase = 0;
            _costasInteg = 0;
            _symbolPhase = 0;
            _prevDecisionSample = 0;
            _prevMidSample = 0;
            _pastHalf = false;
            _totalBitsProcessed = 0;

            _lpI1.Reset();
            _lpI2.Reset();
            _lpQ1.Reset();
            _lpQ2.Reset();

            for (int i = 0; i < 8; i++) _psChars[i] = ' ';
            for (int i = 0; i < 64; i++) _rtChars[i] = ' ';
        }

        /// <summary>
        /// Simple biquad IIR filter for RDS signal processing.
        /// </summary>
        internal class BiquadFilter
        {
            private double _a0, _a1, _a2, _b1, _b2;
            private double _x1, _x2, _y1, _y2;

            private BiquadFilter() { }

            public double Process(double x)
            {
                double y = _a0 * x + _a1 * _x1 + _a2 * _x2 - _b1 * _y1 - _b2 * _y2;
                _x2 = _x1; _x1 = x;
                _y2 = _y1; _y1 = y;
                return y;
            }

            public void Reset()
            {
                _x1 = _x2 = _y1 = _y2 = 0;
            }

            public static BiquadFilter Lowpass(int sampleRate, double freq, double q)
            {
                var f = new BiquadFilter();
                double w0 = 2.0 * Math.PI * freq / sampleRate;
                double alpha = Math.Sin(w0) / (2.0 * q);
                double cosw0 = Math.Cos(w0);
                double a0 = 1 + alpha;

                f._a0 = (1 - cosw0) / 2.0 / a0;
                f._a1 = (1 - cosw0) / a0;
                f._a2 = f._a0;
                f._b1 = -2.0 * cosw0 / a0;
                f._b2 = (1 - alpha) / a0;
                return f;
            }

            public static BiquadFilter Bandpass(int sampleRate, double freq, double q)
            {
                var f = new BiquadFilter();
                double w0 = 2.0 * Math.PI * freq / sampleRate;
                double alpha = Math.Sin(w0) / (2.0 * q);
                double cosw0 = Math.Cos(w0);
                double a0 = 1 + alpha;

                f._a0 = alpha / a0;
                f._a1 = 0.0;
                f._a2 = -alpha / a0;
                f._b1 = -2.0 * cosw0 / a0;
                f._b2 = (1 - alpha) / a0;
                return f;
            }
        }
    }
}
