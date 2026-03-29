using LoggerService;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using RTLSDR.Common;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB decoder.
    /// </summary>
    public class DABDecoder
    {
        private readonly ILoggingService _loggingService;

        private readonly EEPProtection _EEPProtection;
        private readonly Viterbi _MSCViterbi;
        private readonly EnergyDispersal _energyDispersal;

        private readonly List<byte>? _buffer = null;
        private readonly byte[] _rsPacket = new byte[120];
        private readonly int[] _corrPos = new int[10];
        private readonly int _frameLength = 0;
        private int _currentFrame = 0; // frame_count
        private readonly int _fragmentSize = 0;

        private int _countforInterleaver = 0;
        private int _interleaverIndex = 0;

        private readonly int _bitRate = 0;

        private readonly sbyte[] InterleaveMap = new sbyte[16] { 0, 8, 4, 12, 2, 10, 6, 14, 1, 9, 5, 13, 3, 11, 7, 15 };
        private readonly sbyte[,]? _interleaveData = null;
        private readonly sbyte[]? _tempX = null;

        private readonly ReedSolomonErrorCorrection _rs;
        private readonly DABCRC _crcFireCode;
        private readonly DABCRC _crc16;

        private AACSuperFrameHeader? _aacSuperFrameHeader = null;
        private readonly DynamicLabelDecoder? _dynamicLabelDecoder = null;

        private event EventHandler? _onAACDataDemodulated;
        private event EventHandler? _onAACSuperFrameHeaderDemodulated;

        private event EventHandler? _onPADDataDemodulated;
        /// <summary>
        /// Occurs when null.
        /// </summary>
        public event EventHandler? OnProcessedSuperFramesChanged = null;

        /// <summary>
        /// Gets the dynamic label.
        /// </summary>
        public string DynamicLabel => _dynamicLabelDecoder?.DynamicLabel ?? string.Empty;

        private readonly ConcurrentQueue<byte[]> _DABQueue;

        /// <summary>
        /// Gets or sets the processed super frames count.
        /// </summary>
        public int ProcessedSuperFramesCount { get; set; } = 0;
        /// <summary>
        /// Gets or sets the processed super frames synced count.
        /// </summary>
        public int ProcessedSuperFramesSyncedCount { get; set; } = 0;
        /// <summary>
        /// Gets or sets the processed super frames a us count.
        /// </summary>
        public int ProcessedSuperFramesAUsCount { get; set; } = 0;
        /// <summary>
        /// Gets or sets the processed super frames a us synced count.
        /// </summary>
        public int ProcessedSuperFramesAUsSyncedCount { get; set; } = 0;

        private bool _synced = false;

        public DABDecoder(ILoggingService loggingService, DABSubChannel dABSubChannel, int CUSize, ConcurrentQueue<byte[]> queue,
            EventHandler OnAACDataDemodulated, EventHandler OnAACSuperFrameHeaderDemodulated)
        {
            _DABQueue = queue;
            _loggingService = loggingService;

            _onAACDataDemodulated = OnAACDataDemodulated;
            _onAACSuperFrameHeaderDemodulated = OnAACSuperFrameHeaderDemodulated;

            _MSCViterbi = new Viterbi(dABSubChannel.Bitrate * 24);
            _EEPProtection = new EEPProtection(dABSubChannel.Bitrate, dABSubChannel.ProtectionProfile, dABSubChannel.ProtectionLevel, _MSCViterbi);

            _energyDispersal = new EnergyDispersal();
            _rs = new ReedSolomonErrorCorrection(8, 0x11D, 0, 1, 10, 135);

            _fragmentSize = Convert.ToInt32(dABSubChannel.Length * CUSize);
            _bitRate = dABSubChannel.Bitrate;

            _interleaveData = new sbyte[16, _fragmentSize];
            _tempX = new sbyte[_fragmentSize];

            _frameLength = 24 * dABSubChannel.Bitrate / 8;
            _buffer = new List<byte>();

            for (var i = 0; i < _rsPacket.Length; i++)
            {
                _rsPacket[i] = 0;
            }
            for (var i = 0; i < _corrPos.Length; i++)
            {
                _corrPos[i] = 0;
            }

            _crcFireCode = new DABCRC(false, false, 0x782F);
            _crc16 = new DABCRC(true, true, 0x1021);

            _dynamicLabelDecoder = new DynamicLabelDecoder(loggingService);
        }

        public bool Synced
        {
            get
            {
                return _synced;
            }
        }

        private int SFLength
        {
            get
            {
                return _frameLength * 5;
            }
        }

        public void ProcessCIFFragmentData(sbyte[] DABBuffer)
        {
            // DAB-audio.run

            for (var i = 0; i < _fragmentSize; i++)
            {
                var index = (_interleaverIndex + InterleaveMap[i & 15]) & 15;
                _tempX![i] = _interleaveData![index, i];
                _interleaveData[_interleaverIndex, i] = DABBuffer[i];
            }

            _interleaverIndex = (_interleaverIndex + 1) & 15;

            //  only continue when de-interleaver is filled
            if (_countforInterleaver <= 15)
            {
                _countforInterleaver++;
                return;
            }

            var outV = _EEPProtection.Deconvolve(_tempX!);
            if (outV == null)
            {
                return;
            }

            var bytes = _energyDispersal.Dedisperse(outV);

            // -> decoder_adapter.addtoFrame

            var finalBytes = GetFrameBytes(bytes, _bitRate);

            if ((finalBytes != null) && (finalBytes.Length > 0))
            {
                _DABQueue.Enqueue(finalBytes);
            }
        }

        /// <summary>
        /// Convert 8 bits (stored in one uint8) into one uint8
        /// </summary>
        /// <returns></returns>
        private byte[]? GetFrameBytes(byte[] v, int bitRate)
        {
            try
            {
                var length = 24 * bitRate / 8; // should be 2880 bytes

                var res = new byte[length];

                for (var i = 0; i < length; i++)
                {
                    res[i] = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        res[i] <<= 1;
                        res[i] |= Convert.ToByte(v[8 * i + j] & 01);
                    }
                }

                return res;
            }
            catch
            {
                return new byte[0];
            }
        }

        private bool CheckSync(byte[] sf)
        {
            // abort, if au_start is kind of zero (prevent sync on complete zero array)
            if (sf[3] == 0x00 && sf[4] == 0x00)
                return false;

            // try to sync on fire code
            uint crc_stored = Convert.ToUInt16(sf[0] << 8 | sf[1]);

            byte[] dataForCRC = new byte[9];
            Buffer.BlockCopy(sf, 2, dataForCRC, 0, 9);

            uint crc_calced = _crcFireCode.CalcCRC(dataForCRC);
            if (crc_stored != crc_calced)
                return false;

            return true;
        }

        public void Feed(byte[] data)
        {
            try
            {
                _buffer!.AddRange(data);
                _currentFrame++;

                if (_currentFrame < 5)
                {
                    return;
                }
                if (_currentFrame > 5)
                {
                    // drop first part
                    _buffer.RemoveRange(0, data.Length);
                }

                var bytes = _buffer.ToArray();

                ProcessedSuperFramesCount++;

                if (OnProcessedSuperFramesChanged != null)
                {
                    OnProcessedSuperFramesChanged?.Invoke(this, new EventArgs());
                }

                DecodeSuperFrame(bytes);

                if (CheckSync(bytes))
                {
                    _currentFrame = 0;
                    _buffer.Clear();

                    ProcessedSuperFramesSyncedCount++;

                    if (OnProcessedSuperFramesChanged != null)
                    {
                        OnProcessedSuperFramesChanged?.Invoke(this, new EventArgs());
                    }

                    _synced = true;
                    _buffer.Clear();

                    _aacSuperFrameHeader = AACSuperFrameHeader.Parse(bytes);
                    // TODO: check for correct order of start offsets

                    // decode frames
                    for (int i = 0; i < _aacSuperFrameHeader.NumAUs; i++)
                    {
                        ProcessedSuperFramesAUsCount++;

                        if (OnProcessedSuperFramesChanged != null)
                        {
                            OnProcessedSuperFramesChanged?.Invoke(this, new EventArgs());
                        }

                        if (_aacSuperFrameHeader.AUStart == null)
                        {
                            _loggingService.Debug("DABDecoder: invalid AU start offsets");
                            continue;
                        }

                        var start = _aacSuperFrameHeader.AUStart[i];
                        var finish =  _aacSuperFrameHeader.AUStart[i+1];

                        var len = finish - start;

                        // last two bytes hold CRC
                        var crcStored = bytes[finish - 2] << 8 | bytes[finish - 1];

                        var AUData = new byte[len - 2];
                        Buffer.BlockCopy(bytes, start, AUData, 0, len - 2);

                        var crcCalced = _crc16.CalcCRC(AUData);

                        if (crcStored != crcCalced)
                        {
                            _loggingService.Debug("DABDecoder: crc failed");
                            continue;
                        }

                        ProcessedSuperFramesAUsSyncedCount++;

                        if (OnProcessedSuperFramesChanged != null)
                        {
                            OnProcessedSuperFramesChanged?.Invoke(this, new EventArgs());
                        }

                        // Extract PAD / Dynamic Label from each AU
                        _dynamicLabelDecoder?.ProcessAUData(AUData);

                        // send to _AACQueue
                        if (_onAACSuperFrameHeaderDemodulated != null)
                        {
                            _onAACSuperFrameHeaderDemodulated(this, new AACSuperFrameHaderDemodulatedEventArgs()
                            {
                                Header = _aacSuperFrameHeader
                            });
                        }

                        if (_onAACDataDemodulated != null)
                        {
                            _onAACDataDemodulated(this, new DataDemodulatedEventArgs()
                            {
                                Data = AUData
                            });
                        }

                    }
                }

                // Extract and send PAD data
                // this code about PAD  is generated by AI and is not tested!
                if (_onPADDataDemodulated != null && _aacSuperFrameHeader != null && _aacSuperFrameHeader.AUStart != null)
                {
                    int padStart = _aacSuperFrameHeader.AUStart[_aacSuperFrameHeader.NumAUs];
                    int padLength = bytes.Length - padStart;
                    if (padLength > 0)
                    {
                        byte[] padData = new byte[padLength];
                        Buffer.BlockCopy(bytes, padStart, padData, 0, padLength);
                        _onPADDataDemodulated(this, new DataDemodulatedEventArgs() { Data = padData });
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, ex.StackTrace);
            }
        }

        private bool DecodeSuperFrame(byte[] sf)
        {
            var subch_index = SFLength / 120;
            var total_corr_count = 0;
            var uncorr_errors = false;

            // process all RS packets
            for (int i = 0; i < subch_index; i++)
            {
                for (int pos = 0; pos < 120; pos++)
                {
                    _rsPacket[pos] = sf[pos * subch_index + i];
                }

                // detect errors
                int corr_count = _rs.DecodeRSChar(_rsPacket, _corrPos, 0);
                if (corr_count == -1)
                    uncorr_errors = true;
                else
                    total_corr_count += corr_count;

                // correct errors
                for (int j = 0; j < corr_count; j++)
                {
                    int pos = _corrPos[j] - 135;
                    if (pos < 0)
                        continue;

                    sf[pos * subch_index + i] = _rsPacket[pos];
                }
            }

            return uncorr_errors;
        }
    }
}
