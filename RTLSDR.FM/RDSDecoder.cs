using System;
using LoggerService;

namespace RTLSDR.FM
{
    /// <summary>
    /// Decodes RDS (Radio Data System) data from FM baseband.
    /// Uses pilot-tone-locked carrier recovery: locks a PLL to the strong 19 kHz
    /// pilot, then triples the phase to synthesize a clean 57 kHz RDS carrier.
    /// </summary>
    public class RDSDecoder
    {
        // RDS constants
        private const double RDS_SUBCARRIER_FREQ = 57000.0;
        private const double RDS_PILOT_FREQ = 19000.0;
        private const double RDS_SYMBOL_RATE = 1187.5;

        // CRC generator polynomial: x^10 + x^8 + x^7 + x^5 + x^4 + x^3 + 1
        private const ushort CRC_POLY = 0x1B9;

        // Offset words (10-bit) for block identification per IEC 62106
        private const ushort OFFSET_A = 0x0FC;
        private const ushort OFFSET_B = 0x198;
        private const ushort OFFSET_C = 0x168;
        private const ushort OFFSET_CP = 0x350;
        private const ushort OFFSET_D = 0x1B4;
        private static readonly ushort[] ALL_OFFSETS = { OFFSET_A, OFFSET_B, OFFSET_C, OFFSET_CP, OFFSET_D };

        // SDR input sample rate (1 MHz for FM)
        private readonly int _inputSampleRate;

        // Internal FM demod state (separate from audio path)
        private short _rdsNowR = 0, _rdsNowJ = 0;
        private int _rdsPrevIndex = 0;
        private short _rdsPreR = 0, _rdsPreJ = 0;

        // Downsample factor: input rate → intermediate rate
        private readonly int _downsampleFactor;

        // Intermediate sample rate after IQ downsampling and FM demod
        private readonly int _intermediateRate;

        // Buffers
        private readonly short[] _rdsIQBuffer;
        private readonly short[] _rdsBasebandBuffer;

        // === 19 kHz Pilot PLL ===
        private double _pilotPllPhase = 0.0;
        private readonly double _pilotPllNominalInc;
        private double _pilotPllInteg = 0.0;
        // Narrow bandwidth PLL for clean pilot lock
        private const double PILOT_PLL_KP = 0.01;
        private const double PILOT_PLL_KI = 0.00005;
        private bool _pilotLocked = false;
        private double _pilotLockAvg = 0.0;

        // Bandpass filter for 19 kHz pilot extraction
        private readonly BiquadFilter _bpPilot1;
        private readonly BiquadFilter _bpPilot2;

        // 4th-order LP for RDS I and Q after mixing with 3×pilot
        private readonly BiquadFilter _lpI1, _lpI2;
        private readonly BiquadFilter _lpQ1, _lpQ2;

        // Symbol timing with Gardner TED
        private double _symbolPhase = 0.0;
        private readonly double _symbolPhaseInc;
        private double _prevSymbolSample = 0.0;
        private double _midSymbolSample = 0.0;
        private bool _pastMid = false;
        private const double TIMING_GAIN = 0.003;

        // Bit accumulation
        private uint _bitBuffer = 0;
        private int _bitCount = 0;

        // Block sync
        private int _blockIndex = -1;
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

        // PS name assembly
        private readonly char[] _psChars = new char[8];
        private int _psSegmentMask = 0;

        // Radio Text assembly
        private readonly char[] _rtChars = new char[64];
        private int _rtSegmentMask = 0;
        private int _rtAbFlag = -1;

        // Diagnostics
        private int _totalBitsProcessed = 0;
        private int _syncAttempts = 0;
        private DateTime _lastDiagTime = DateTime.MinValue;

        private readonly ILoggingService? _loggingService;

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

        public bool Synced => _blockSynced;
        public bool PilotLocked => _pilotLocked;
        public int GoodBlockCount => _goodBlockCount;

        /// <param name="loggingService">Logger</param>
        /// <param name="inputSampleRate">SDR sample rate in Hz (default 1000000 for FM)</param>
        public RDSDecoder(ILoggingService? loggingService, int inputSampleRate = 1000000)
        {
            _loggingService = loggingService;
            _inputSampleRate = inputSampleRate;

            // Target intermediate rate around 200 kHz (must be > 2 × 57 kHz)
            _downsampleFactor = Math.Max(1, _inputSampleRate / 200000);
            _intermediateRate = _inputSampleRate / _downsampleFactor;

            _loggingService?.Info($"RDS decoder: input={_inputSampleRate} Hz, downsample={_downsampleFactor}, intermediate={_intermediateRate} Hz");

            _rdsIQBuffer = new short[(_inputSampleRate / _downsampleFactor) + 1000];
            _rdsBasebandBuffer = new short[(_inputSampleRate / _downsampleFactor / 2) + 1000];

            // Pilot bandpass: two cascaded biquads at 19 kHz with high Q for narrow passband
            _bpPilot1 = BiquadFilter.Bandpass(_intermediateRate, RDS_PILOT_FREQ, 10.0);
            _bpPilot2 = BiquadFilter.Bandpass(_intermediateRate, RDS_PILOT_FREQ, 10.0);

            // Pilot PLL nominal phase increment
            _pilotPllNominalInc = 2.0 * Math.PI * RDS_PILOT_FREQ / _intermediateRate;

            // 4th-order LP at 2400 Hz for RDS baseband (covers ±1187.5 Hz symbol rate)
            _lpI1 = BiquadFilter.Lowpass(_intermediateRate, 2400, 0.707);
            _lpI2 = BiquadFilter.Lowpass(_intermediateRate, 2400, 0.707);
            _lpQ1 = BiquadFilter.Lowpass(_intermediateRate, 2400, 0.707);
            _lpQ2 = BiquadFilter.Lowpass(_intermediateRate, 2400, 0.707);

            // Symbol timing increment
            _symbolPhaseInc = RDS_SYMBOL_RATE / _intermediateRate;

            for (int i = 0; i < 8; i++) _psChars[i] = ' ';
            for (int i = 0; i < 64; i++) _rtChars[i] = ' ';
        }

        /// <summary>
        /// Processes raw IQ data from the SDR.
        /// </summary>
        public void ProcessIQData(byte[] iqData, int length)
        {
            // Step 1: Downsample IQ to intermediate rate
            int iqCount = LowPassRDS(iqData, _rdsIQBuffer, length);
            if (iqCount < 4) return;

            // Step 2: FM demodulate → baseband at intermediate rate
            int basebandCount = FMDemodulateRDS(_rdsIQBuffer, _rdsBasebandBuffer, iqCount);
            if (basebandCount < 2) return;

            // Step 3: Pilot-locked RDS extraction
            for (int i = 0; i < basebandCount; i++)
            {
                double sample = _rdsBasebandBuffer[i] / 16384.0;

                // --- 19 kHz Pilot Recovery ---
                // Narrow bandpass to isolate pilot
                double pilotFiltered = _bpPilot2.Process(_bpPilot1.Process(sample));

                // PLL phase detector: multiply filtered pilot by PLL sine
                double pilotPd = pilotFiltered * Math.Cos(_pilotPllPhase);

                // Loop filter (2nd order)
                _pilotPllInteg += PILOT_PLL_KI * pilotPd;
                double pilotFreqAdj = PILOT_PLL_KP * pilotPd + _pilotPllInteg;

                // Advance PLL phase
                _pilotPllPhase += _pilotPllNominalInc + pilotFreqAdj;
                if (_pilotPllPhase > 2.0 * Math.PI) _pilotPllPhase -= 2.0 * Math.PI;
                if (_pilotPllPhase < 0) _pilotPllPhase += 2.0 * Math.PI;

                // Pilot lock detector: correlation between filtered pilot and PLL sine
                double pilotCorr = pilotFiltered * Math.Sin(_pilotPllPhase);
                _pilotLockAvg = 0.9999 * _pilotLockAvg + 0.0001 * Math.Abs(pilotCorr);
                _pilotLocked = _pilotLockAvg > 0.001;

                // --- RDS Demodulation using 3× pilot phase ---
                // 57 kHz = 3 × 19 kHz, phase-coherent with pilot
                double rdsPhase = 3.0 * _pilotPllPhase;
                double rdsCarrierI = Math.Cos(rdsPhase);
                double rdsCarrierQ = Math.Sin(rdsPhase);

                // Mix baseband with 57 kHz carrier → RDS at DC
                double mixI = sample * rdsCarrierI * 2.0;
                double mixQ = sample * rdsCarrierQ * 2.0;

                // 4th-order low-pass to isolate RDS from everything else
                double filtI = _lpI2.Process(_lpI1.Process(mixI));
                double filtQ = _lpQ2.Process(_lpQ1.Process(mixQ));

                // Use I channel (BPSK data is on the in-phase component)
                // The pilot phase lock ensures correct I/Q orientation
                double rdsSignal = filtI;

                // Symbol timing with Gardner TED
                double prevPhase = _symbolPhase;
                _symbolPhase += _symbolPhaseInc;

                // Capture mid-symbol sample
                if (!_pastMid && _symbolPhase >= 0.5 && _symbolPhase < 1.0)
                {
                    _midSymbolSample = rdsSignal;
                    _pastMid = true;
                }

                if (_symbolPhase >= 1.0)
                {
                    _symbolPhase -= 1.0;
                    _pastMid = false;

                    // Gardner timing error
                    double timingErr = (_prevSymbolSample - rdsSignal) * _midSymbolSample;
                    double timingNorm = Math.Abs(_prevSymbolSample) + Math.Abs(rdsSignal) + 1e-10;
                    _symbolPhase += TIMING_GAIN * (timingErr / timingNorm);
                    if (_symbolPhase < -0.5) _symbolPhase = -0.5;
                    if (_symbolPhase > 0.5) _symbolPhase = 0.5;

                    _prevSymbolSample = rdsSignal;

                    // Hard bit decision + differential decode
                    int rawBit = rdsSignal > 0 ? 1 : 0;
                    int decodedBit = rawBit ^ _prevBit;
                    _prevBit = rawBit;

                    _totalBitsProcessed++;
                    ProcessBit(decodedBit);
                }
            }

            // Periodic diagnostics
            if ((DateTime.UtcNow - _lastDiagTime).TotalSeconds > 5)
            {
                _loggingService?.Info($"RDS: pilotLock={_pilotLocked} level={_pilotLockAvg:F6} bits={_totalBitsProcessed} synced={_blockSynced} goodBlk={_goodBlockCount} badBlk={_badBlockCount} syncAttempts={_syncAttempts}");
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

                if (_rdsPrevIndex >= _downsampleFactor)
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
                if (_bitCount >= 26)
                {
                    uint block26 = _bitBuffer & 0x03FFFFFF;
                    ushort syndrome = CalculateSyndrome(block26);

                    // Try to sync on any known offset word
                    for (int oi = 0; oi < 4; oi++)
                    {
                        ushort expected;
                        switch (oi)
                        {
                            case 0: expected = OFFSET_A; break;
                            case 1: expected = OFFSET_B; break;
                            case 2: expected = OFFSET_C; break;
                            default: expected = OFFSET_D; break;
                        }

                        if (syndrome == expected)
                        {
                            _blockIndex = oi;
                            _groupData[oi] = (ushort)((block26 >> 10) & 0xFFFF);
                            _blockSynced = true;
                            _goodBlockCount = 1;
                            _badBlockCount = 0;
                            _bitCount = 0;
                            _syncAttempts++;
                            _loggingService?.Info($"RDS: sync acquired on block {(char)('A' + oi)} (attempt {_syncAttempts})");

                            // Advance to next expected block
                            int completedBlock = _blockIndex;
                            _blockIndex = (_blockIndex + 1) % 4;
                            return;
                        }
                    }
                }
            }
            else
            {
                if (_bitCount >= 26)
                {
                    uint block26 = _bitBuffer & 0x03FFFFFF;
                    ushort syndrome = CalculateSyndrome(block26);
                    ushort data = (ushort)((block26 >> 10) & 0xFFFF);

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
                        if (_badBlockCount > 20)
                        {
                            _blockSynced = false;
                            _blockIndex = -1;
                            _badBlockCount = 0;
                            _bitCount = 0;
                            _loggingService?.Info($"RDS: sync lost after {_goodBlockCount} good blocks");
                            _goodBlockCount = 0;
                            return;
                        }
                    }

                    int completedBlock = _blockIndex;
                    _blockIndex = (_blockIndex + 1) % 4;
                    _bitCount = 0;

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

            ushort pi = blockA;
            int groupType = (blockB >> 12) & 0x0F;
            bool versionB = ((blockB >> 11) & 1) == 1;
            bool tp = ((blockB >> 10) & 1) == 1;
            int pty = (blockB >> 5) & 0x1F;

            _rdsData.PI = pi;
            _rdsData.PTY = pty;
            _rdsData.TP = tp;

            _loggingService?.Info($"RDS: decoded group type={groupType}{(versionB ? "B" : "A")} PI=0x{pi:X4} PTY={pty}");

            switch (groupType)
            {
                case 0:
                    DecodeGroup0(blockB, blockD);
                    break;
                case 2:
                    DecodeGroup2(blockB, blockC, blockD, versionB);
                    break;
            }
        }

        private void DecodeGroup0(ushort blockB, ushort blockD)
        {
            _rdsData.TA = ((blockB >> 4) & 1) == 1;
            _rdsData.IsStereo = ((blockB >> 3) & 1) == 1;

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

            if (_rtAbFlag != -1 && _rtAbFlag != abFlag)
            {
                _rtSegmentMask = 0;
                for (int i = 0; i < 64; i++) _rtChars[i] = ' ';
            }
            _rtAbFlag = abFlag;

            if (!versionB)
            {
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
                int baseIdx = segmentAddr * 2;
                if (baseIdx + 1 < 64)
                {
                    SetRTChar(baseIdx, (char)(blockD >> 8));
                    SetRTChar(baseIdx + 1, (char)(blockD & 0xFF));
                }
            }

            _rtSegmentMask |= (1 << segmentAddr);

            string rt = new string(_rtChars).TrimEnd();
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

            _pilotPllPhase = 0;
            _pilotPllInteg = 0;
            _pilotLocked = false;
            _pilotLockAvg = 0;

            _symbolPhase = 0;
            _prevSymbolSample = 0;
            _midSymbolSample = 0;
            _pastMid = false;
            _totalBitsProcessed = 0;
            _syncAttempts = 0;

            _bpPilot1.Reset();
            _bpPilot2.Reset();
            _lpI1.Reset();
            _lpI2.Reset();
            _lpQ1.Reset();
            _lpQ2.Reset();

            for (int i = 0; i < 8; i++) _psChars[i] = ' ';
            for (int i = 0; i < 64; i++) _rtChars[i] = ' ';
        }

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
