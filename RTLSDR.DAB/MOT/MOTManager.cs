using System;
using System.Collections.Generic;
using System.Linq;
using LoggerService;
using RTLSDR.Common;

namespace RTLSDR.DAB.MOT
{
    /// <summary>
    /// Manages MOT object reassembly from MSC data groups and raises events when complete slides arrive.
    /// References: ETSI EN 301 234 (MOT Protocol), ETSI TS 101 499 (MOT SlideShow).
    /// </summary>
    public class MOTManager
    {
        private const int MAX_CONCURRENT_OBJECTS = 8;
        private readonly ILoggingService _loggingService;
        private readonly Dictionary<int, MOTObject> _activeObjects = new Dictionary<int, MOTObject>();

        /// <summary>
        /// Event raised when a complete slide has been decoded.
        /// </summary>
        public event EventHandler<DABSlide>? OnSlideCompleted;

        /// <summary>
        /// Most recently completed slide.
        /// </summary>
        public DABSlide? LastSlide { get; private set; }

        public MOTManager(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Resets active MOT objects.
        /// </summary>
        public void Reset()
        {
            _activeObjects.Clear();
            LastSlide = null;
        }

        /// <summary>
        /// Handles a complete, CRC-checked MOT MSC Data Group.
        /// </summary>
        public void HandleMOTDataGroup(byte[] dg)
        {
            if (dg == null || dg.Length < 7)
                return;

            int offset = 0;

            // 1. Data Group Header (EN 300 401 §5.3.3)
            bool extFlag = (dg[offset] & 0x80) != 0;
            bool crcFlag = (dg[offset] & 0x40) != 0;
            bool segFlag = (dg[offset] & 0x20) != 0;
            bool userAccessFlag = (dg[offset] & 0x10) != 0;
            int dgType = dg[offset] & 0x0F;

            offset += 2 + (extFlag ? 2 : 0);

            if (!crcFlag || !segFlag || !userAccessFlag)
                return;

            if (dgType != 3 && dgType != 4) // 3 = MOT Header, 4 = MOT Body
                return;

            // 2. Session Header (EN 301 234 §5.1)
            if (dg.Length < offset + 3)
                return;

            bool lastSeg = (dg[offset] & 0x80) != 0;
            int segNumber = ((dg[offset] & 0x7F) << 8) | dg[offset + 1];
            bool transportIdFlag = (dg[offset + 2] & 0x10) != 0;
            int lenIndicator = dg[offset + 2] & 0x0F;
            offset += 3;

            if (!transportIdFlag || lenIndicator < 2 || dg.Length < offset + lenIndicator)
                return;

            int transportId = (dg[offset] << 8) | dg[offset + 1];
            offset += lenIndicator;

            // 3. Segmentation Header (EN 301 234 §5.2)
            if (dg.Length < offset + 2)
                return;

            int segSize = ((dg[offset] & 0x1F) << 8) | dg[offset + 1];
            offset += 2;

            // Validate segment size against remaining data (minus 2 bytes CRC)
            if (segSize != dg.Length - offset - 2)
                return;

            // Look up or instantiate active MOT object for this transport ID
            if (!_activeObjects.TryGetValue(transportId, out var obj))
            {
                if (_activeObjects.Count >= MAX_CONCURRENT_OBJECTS)
                {
                    var oldestKey = _activeObjects.Keys.First();
                    _activeObjects.Remove(oldestKey);
                }

                obj = new MOTObject(transportId);
                _activeObjects[transportId] = obj;
            }

            // Add segment
            obj.AddSeg(isHeader: dgType == 3, segNumber: segNumber, lastSeg: lastSeg, data: dg, offset: offset, length: segSize);

            // Check if slide is complete and ready to display
            var slide = obj.CheckAndCreateSlide();
            if (slide != null)
            {
                LastSlide = slide;
                _loggingService.Info($"DAB MOT Slide: '{slide.ContentName}' ({slide.MimeType}, {slide.ImageBytes.Length} bytes)");
                OnSlideCompleted?.Invoke(this, slide);
            }
        }
    }
}
