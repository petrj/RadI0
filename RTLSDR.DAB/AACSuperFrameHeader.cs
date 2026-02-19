using System;
namespace RTLSDR.DAB
{
    /// <summary>
    /// Table 3: Definition of dac_rate
    /// </summary>
    public enum DacRateEnum
    {
        DacRate32KHz = 0,
        DacRate48KHz = 1
    }

    /// <summary>
    /// Table 4: Definition of sbr_flag
    /// </summary>
    public enum SBRFlagEnum
    {
        SBRNotUsed = 0,
        SBRUsed = 1
    }

    /// <summary>
    /// Table 5: Definition of aac_channel_mode
    /// </summary>
    public enum AACChannelModeEnum
    {
        Mono = 0,
        Stereo = 1
    }

    /// <summary>
    /// Table 6: Definition of ps_flag (Parametric stereo)
    /// </summary>
    public enum PSFlagEnum
    {
        PSNotUsed = 0,
        PSUsed = 1
    }

    /// <summary>
    /// Table 7: Definition of mpeg_surround_config
    /// </summary>
    public enum MPEGSurroundEnum
    {
        MPEGSurroundNotUsed = 0,
        MPEGSurround51 = 1,
        MPEGSurround71 = 2,
        Other = 7
    }

    /// <summary>
    /// https://www.etsi.org/deliver/etsi_ts/102500_102599/102563/02.01.01_60/ts_102563v020101p.pdf
    /// ETSI TS 102 563 V2.1.1 (2017-01)10 Table 2
    /// </summary>
    public class AACSuperFrameHeader
    {
        public int FireCode { get; set; }
        public int NumAUs { get; set; } = 0;
        public int[] AUStart { get; set; } = null;

        public DacRateEnum DacRate { get; set; }
        public SBRFlagEnum SBRFlag { get; set; }
        public AACChannelModeEnum AACChannelMode { get; set; }
        public PSFlagEnum PSFlag { get; set; }
        public MPEGSurroundEnum MPEGSurround { get; set; }

        public static AACSuperFrameHeader Parse(byte[] data)
        {
            var superFrame = new AACSuperFrameHeader();

            superFrame.FireCode = (data[0] << 8) | data[1];
            superFrame.DacRate = (DacRateEnum)((data[2] & 64) >> 6);
            superFrame.SBRFlag = (SBRFlagEnum)((data[2] & 32) >> 5);
            superFrame.AACChannelMode = (AACChannelModeEnum)((data[2] & 16) >> 4);
            superFrame.PSFlag = (PSFlagEnum)((data[2] & 8) >> 3);
            superFrame.MPEGSurround = (MPEGSurroundEnum)(data[2] & 7);

            if ((superFrame.DacRate == DacRateEnum.DacRate32KHz) && (superFrame.SBRFlag == SBRFlagEnum.SBRUsed)) superFrame.NumAUs = 2;    // AAC core sampling rate 16 kHz
            if ((superFrame.DacRate == DacRateEnum.DacRate48KHz) && (superFrame.SBRFlag == SBRFlagEnum.SBRUsed)) superFrame.NumAUs = 3;    // AAC core sampling rate 24 kHz
            if ((superFrame.DacRate == DacRateEnum.DacRate32KHz) && (superFrame.SBRFlag == SBRFlagEnum.SBRNotUsed)) superFrame.NumAUs = 4; // AAC core sampling rate 32 kHz
            if ((superFrame.DacRate == DacRateEnum.DacRate48KHz) && (superFrame.SBRFlag == SBRFlagEnum.SBRNotUsed)) superFrame.NumAUs = 6;    // AAC core sampling rate 48 kHz

            superFrame.AUStart = new int[superFrame.NumAUs+1];
            superFrame.AUStart[superFrame.NumAUs] = data.Length / 120 * 110;

            // Table 8: Definition of au_start for the first AU of the audio super frame
            switch (superFrame.NumAUs)
            {
                case 2:
                    superFrame.AUStart[0] = 5;
                    break;
                case 3:
                    superFrame.AUStart[0] = 6;
                    break;
                case 4:
                    superFrame.AUStart[0] = 8;
                    break;
                case 6:
                    superFrame.AUStart[0] = 11;
                    break;
            }

            int actAUStart = 1;
            int bitsPosition = 3 * 8; // 4th byte

            while (actAUStart < superFrame.NumAUs)
            {
                var remain = bitsPosition % 8;
                var startByte = bitsPosition / 8;

                int b = 0;
                if (remain == 0)
                {
                    // get 8 bits from actual byte, 4 bits from next byte
                    b = (data[startByte] << 4) | ((data[startByte+1] & 240) >> 4);
                } else
                {
                    // get 4 last bits from actual byte, 8 bits from next byte
                    b = ((data[startByte] & 15) << 8) | (data[startByte + 1]);
                }

                superFrame.AUStart[actAUStart] = b;

                bitsPosition += 12;
                actAUStart++;
            }

            return superFrame;
        }

        /// <summary>
        /// Build a 7-byte ADTS header for a single AAC raw frame (no CRC).
        /// The returned header must be prepended to the raw AAC payload.
        /// <param name="payloadLength">Length of AAC raw data in bytes (not including ADTS header).</param>
        /// <param name="profileIndex">ADTS profile index (0=Main,1=LC,2=SSR). Default: 1 (AAC LC).</param>
        /// </summary>
        public byte[] GetADTSHeaderForLength(int payloadLength, int profileIndex = 1)
        {
            // Determine core sampling frequency index based on DAC rate + SBR
            int samplingFreqIndex;
            if ((this.DacRate == DacRateEnum.DacRate32KHz) && (this.SBRFlag == SBRFlagEnum.SBRUsed)) samplingFreqIndex = 8; // 16 kHz
            else if ((this.DacRate == DacRateEnum.DacRate48KHz) && (this.SBRFlag == SBRFlagEnum.SBRUsed)) samplingFreqIndex = 6; // 24 kHz
            else if ((this.DacRate == DacRateEnum.DacRate32KHz) && (this.SBRFlag == SBRFlagEnum.SBRNotUsed)) samplingFreqIndex = 5; // 32 kHz
            else samplingFreqIndex = 3; // 48 kHz (DacRate48KHz + SBRNotUsed)

            int channelConfig = (this.AACChannelMode == AACChannelModeEnum.Mono) ? 1 : 2;

            int aacFrameLength = payloadLength + 7; // ADTS header (7 bytes) + payload

            var adts = new byte[7];

            // Syncword 0xFFF
            adts[0] = 0xFF;
            // 0xF1 : 1111 0001 -> syncword cont (1111), ID=0(MPEG-4), layer=00, protection_absent=1
            adts[1] = 0xF1;

            // profile (2 bits), sampling freq index (4 bits), private bit (1), channel cfg (1 bit part)
            adts[2] = (byte)(((profileIndex & 0x03) << 6) | ((samplingFreqIndex & 0x0F) << 2) | ((channelConfig >> 2) & 0x01));

            // channel_config (2 LSBs), original/copy, home, aac_frame_length (first 2 bits)
            adts[3] = (byte)(((channelConfig & 0x03) << 6) | ((aacFrameLength >> 11) & 0x03));

            // aac_frame_length (middle 8 bits)
            adts[4] = (byte)((aacFrameLength >> 3) & 0xFF);

            // aac_frame_length (last 3 bits), adts_buffer_fullness (first 5 bits)
            adts[5] = (byte)(((aacFrameLength & 0x7) << 5) | ((0x7FF >> 6) & 0x1F));

            // adts_buffer_fullness (last 6 bits), number_of_raw_data_blocks_in_frame (2 bits)
            adts[6] = (byte)(((0x7FF & 0x3F) << 2) | (0 & 0x03));

            return adts;
        }
    }
}
