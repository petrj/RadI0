using System;
using System.Text;
using RTLSDR.Common;

namespace RTLSDR.DAB.MOT
{
    /// <summary>
    /// Represents an MOT object under transmission, consisting of a header entity and a body entity.
    /// References: ETSI EN 301 234 §6 (MOT Header), ETSI TS 101 499 (MOT SlideShow).
    /// </summary>
    public class MOTObject
    {
        public const int CONTENT_TYPE_IMAGE = 0x02;
        public const int CONTENT_TYPE_MOT_TRANSPORT = 0x05;

        public const int CONTENT_SUB_TYPE_HEADER_UPDATE = 0x000;
        public const int CONTENT_SUB_TYPE_JFIF = 0x001;
        public const int CONTENT_SUB_TYPE_GIF = 0x002;
        public const int CONTENT_SUB_TYPE_PNG = 0x003;
        public const int CONTENT_SUB_TYPE_BMP = 0x004;

        private readonly MOTEntity _header = new MOTEntity();
        private readonly MOTEntity _body = new MOTEntity();

        private bool _shown = false;
        private bool _headerReceived = false;
        private int _bodySize = 0;
        private int _contentType = -1;
        private int _contentSubType = -1;
        private string _contentName = string.Empty;
        private string _categoryTitle = string.Empty;
        private string _clickThroughUrl = string.Empty;
        private bool _triggerTimeNow = true;

        public int TransportId { get; }
        public bool Shown => _shown;
        public int BodySize => _bodySize;
        public int CurrentBodySize => _body.TotalSize;
        public bool HeaderReceived => _headerReceived;

        public MOTObject(int transportId)
        {
            TransportId = transportId;
        }

        public void AddSeg(bool isHeader, int segNumber, bool lastSeg, byte[] data, int offset, int length)
        {
            if (isHeader)
            {
                _header.AddSeg(segNumber, lastSeg, data, offset, length);
            }
            else
            {
                _body.AddSeg(segNumber, lastSeg, data, offset, length);
            }
        }

        private bool ParseCheckHeader()
        {
            byte[] data = _header.GetData();
            if (data.Length < 7)
                return false;

            int bodySize = (data[0] << 20) | (data[1] << 12) | (data[2] << 4) | (data[3] >> 4);
            int headerSize = ((data[3] & 0x0F) << 9) | (data[4] << 1) | (data[5] >> 7);
            int contentType = (data[5] & 0x7F) >> 1;
            int contentSubType = ((data[5] & 0x01) << 8) | data[6];

            if (headerSize != data.Length)
                return false;

            bool headerUpdate = (contentType == CONTENT_TYPE_MOT_TRANSPORT &&
                                contentSubType == CONTENT_SUB_TYPE_HEADER_UPDATE);

            // Abort if neither none nor both conditions (header received / update) apply
            if (_headerReceived != headerUpdate)
                return false;

            if (!headerUpdate)
            {
                _bodySize = bodySize;
                _contentType = contentType;
                _contentSubType = contentSubType;
            }

            string oldContentName = _contentName;
            string newContentName = string.Empty;

            // Parse header extension parameters
            int offset = 7;
            while (offset < data.Length)
            {
                int pli = data[offset] >> 6;
                int paramId = data[offset] & 0x3F;
                offset++;

                int dataLen;
                switch (pli)
                {
                    case 0:
                        dataLen = 0;
                        break;
                    case 1:
                        dataLen = 1;
                        break;
                    case 2:
                        dataLen = 4;
                        break;
                    case 3:
                        if (offset >= data.Length) return false;
                        bool ext = (data[offset] & 0x80) != 0;
                        dataLen = data[offset] & 0x7F;
                        offset++;
                        if (ext)
                        {
                            if (offset >= data.Length) return false;
                            dataLen = (dataLen << 8) | data[offset];
                            offset++;
                        }
                        break;
                    default:
                        return false;
                }

                if (offset + dataLen > data.Length)
                    return false;

                switch (paramId)
                {
                    case 0x05: // TriggerTime
                        if (dataLen >= 4)
                        {
                            // Bit 31: 0 = Now, 1 = absolute/relative time
                            _triggerTimeNow = (data[offset] & 0x80) == 0;
                        }
                        break;

                    case 0x0C: // ContentName
                        if (dataLen >= 1)
                        {
                            int charset = (data[offset] >> 4) & 0x0F;
                            newContentName = DecodeText(data, offset + 1, dataLen - 1, charset);
                            _contentName = newContentName;
                        }
                        break;

                    case 0x26: // CategoryTitle
                        if (dataLen > 0)
                        {
                            _categoryTitle = Encoding.UTF8.GetString(data, offset, dataLen).TrimEnd('\0').Trim();
                        }
                        break;

                    case 0x27: // ClickThroughURL
                        if (dataLen > 0)
                        {
                            _clickThroughUrl = Encoding.UTF8.GetString(data, offset, dataLen).TrimEnd('\0').Trim();
                        }
                        break;
                }

                offset += dataLen;
            }

            if (!headerUpdate)
            {
                _headerReceived = true;
            }
            else
            {
                // Ensure matching content name on update
                if (!string.IsNullOrEmpty(newContentName) && newContentName != oldContentName)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if this MOT object has finished receiving all data and is ready to be displayed as a slide.
        /// </summary>
        /// <returns>Decoded DABSlide if complete and valid, otherwise null.</returns>
        public DABSlide? CheckAndCreateSlide()
        {
            if (_shown)
                return null;

            if (_header.IsFinished())
            {
                bool ok = ParseCheckHeader();
                _header.Reset(); // allow for subsequent header updates
                if (!ok)
                    return null;
            }

            if (!_headerReceived)
                return null;

            if (!_body.IsFinished() || _body.TotalSize != _bodySize)
                return null;

            if (!_triggerTimeNow)
                return null;

            byte[] bodyData = _body.GetData();

            string mimeType = GetMimeType(bodyData);
            if (string.IsNullOrEmpty(mimeType))
                return null;

            _shown = true;

            return new DABSlide
            {
                ImageBytes = bodyData,
                MimeType = mimeType,
                ContentName = _contentName,
                CategoryTitle = _categoryTitle,
                ClickThroughUrl = _clickThroughUrl,
                TransportId = TransportId,
                Timestamp = DateTime.UtcNow
            };
        }

        private string GetMimeType(byte[] bodyData)
        {
            string mimeType = string.Empty;

            if (_contentType == CONTENT_TYPE_IMAGE)
            {
                switch (_contentSubType)
                {
                    case CONTENT_SUB_TYPE_JFIF:
                        mimeType = "image/jpeg";
                        break;
                    case CONTENT_SUB_TYPE_PNG:
                        mimeType = "image/png";
                        break;
                    case CONTENT_SUB_TYPE_GIF:
                        mimeType = "image/gif";
                        break;
                    case CONTENT_SUB_TYPE_BMP:
                        mimeType = "image/x-bmp";
                        break;
                }
            }

            // Fallback: detect MIME type by inspection of magic bytes
            if (string.IsNullOrEmpty(mimeType) && bodyData.Length >= 4)
            {
                if (bodyData[0] == 0xFF && bodyData[1] == 0xD8)
                {
                    mimeType = "image/jpeg";
                }
                else if (bodyData[0] == 0x89 && bodyData[1] == 0x50 && bodyData[2] == 0x4E && bodyData[3] == 0x47)
                {
                    mimeType = "image/png";
                }
                else if (bodyData[0] == 0x47 && bodyData[1] == 0x49 && bodyData[2] == 0x46)
                {
                    mimeType = "image/gif";
                }
                else if (bodyData[0] == 0x42 && bodyData[1] == 0x4D)
                {
                    mimeType = "image/x-bmp";
                }
            }

            return mimeType;
        }

        private static string DecodeText(byte[] data, int offset, int length, int charset)
        {
            if (length <= 0)
                return string.Empty;

            var slice = new byte[length];
            Buffer.BlockCopy(data, offset, slice, 0, length);

            switch (charset)
            {
                case 0:
                    return EBUEncoding.GetString(slice);
                case 6:
                    return Encoding.UTF8.GetString(slice).TrimEnd('\0').Trim();
                case 15:
                    return Encoding.BigEndianUnicode.GetString(slice).TrimEnd('\0').Trim();
                default:
                    return Encoding.UTF8.GetString(slice).TrimEnd('\0').Trim();
            }
        }
    }
}
