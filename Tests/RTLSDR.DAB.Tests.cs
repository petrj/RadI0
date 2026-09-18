using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RTLSDR.DAB;
using RTLSDR.Common;
using LoggerService;

namespace Tests
{
    [TestClass]
    /// <summary>
    /// The rtlsdrdab tests.
    /// </summary>
    public class RTLSDRDABTests
    {
        [TestMethod]
        public void FComplex_Multiply_Works()
        {
            var a = new FComplex(1f, 1f);
            var b = new FComplex(1f, 1f);

            var r = FComplex.Multiply(a, b);

            // (1+i)*(1+i) = (1-1) + (1+1)i = 0 + 2i
            Assert.AreEqual(0f, r.Real, 1e-6f);
            Assert.AreEqual(2f, r.Imaginary, 1e-6f);
        }

        [TestMethod]
        public void FComplex_Exp_And_Phase_And_Abs()
        {
            var e = FComplex.Exp((float)(Math.PI / 2.0));
            // exp(j*pi/2) = 0 + 1i
            Assert.AreEqual(0f, e.Real, 1e-5f);
            Assert.AreEqual(1f, e.Imaginary, 1e-5f);

            var c = new FComplex(3f, 4f);
            Assert.AreEqual(5f, c.Abs(), 1e-5f);

            var angle = c.PhaseAngle();
            // angle of (3,4) should be atan2(4,3)
            Assert.AreEqual((float)Math.Atan2(4, 3), angle, 1e-6f);
        }

        [TestMethod]
        public void FComplex_CloneArray_IsDeepCopy()
        {
            var arr = new FComplex[] { new FComplex(1, 2), new FComplex(3, 4) };
            var clone = FComplex.CloneComplexArray(arr);

            // modify original
            arr[0].Real = 9f;

            // clone should remain unchanged
            Assert.AreEqual(1f, clone[0].Real, 1e-6f);
            Assert.AreEqual(2f, clone[0].Imaginary, 1e-6f);
        }

        [TestMethod]
        public void FrequencyInterleaver_CreateAndMap_BasicChecks()
        {
            // use standard DAB symbol size
            var T_u = 1536;
            var fi = new FrequencyInterleaver(T_u, 0);

            // index 0 is reserved and should map to something (implementation returns 0 for many positions)
            var m0 = fi.MapIn(0);
            Assert.IsTrue(m0 <= T_u / 2 && m0 >= -T_u / 2);

            // verify that returned table length allows access at upper index
            var mHigh = fi.MapIn(T_u - 1);
            Assert.IsTrue(mHigh <= T_u / 2 && mHigh >= -T_u / 2);

            // ensure we get at least one non-zero mapping somewhere
            bool anyNonZero = false;
            for (int i = 0; i < Math.Min(200, T_u); i++)
            {
                if (fi.MapIn(i) != 0)
                {
                    anyNonZero = true;
                    break;
                }
            }

            Assert.IsTrue(anyNonZero, "Expected at least one non-zero mapping in the perm table.");
        }

        [TestMethod]
        public void PhaseTable_Builds_RefTableAndValues()
        {
            // PhaseTable depends on a logging service; use the DummyLoggingService from LoggerService package
            var logging = new DummyLoggingService();

            var INPUT_RATE = 1536;
            var T_u = 1536;

            var pt = new PhaseTable(logging, INPUT_RATE, T_u);

            Assert.IsNotNull(pt.RefTable);
            Assert.AreEqual(INPUT_RATE, pt.RefTable.Length);

            // index 0 should be zero as initialized
            Assert.AreEqual(0f, pt.RefTable[0].Real, 1e-6f);
            Assert.AreEqual(0f, pt.RefTable[0].Imaginary, 1e-6f);

            // for small positive index values magnitude should be ~1 on unit circle
            var v = pt.RefTable[1];
            var mag = new FComplex(v.Real, v.Imaginary).Abs();
            Assert.AreEqual(1f, mag, 1e-4f);

            // symmetric entry at T_u - 1 should also be unit magnitude
            var v2 = pt.RefTable[T_u - 1];
            var mag2 = new FComplex(v2.Real, v2.Imaginary).Abs();
            Assert.AreEqual(1f, mag2, 1e-4f);
        }

        private static byte[] MakeSyntheticDataGroup(int dgType, int transportId, byte[] payload)
        {
            var dg = new System.Collections.Generic.List<byte>();
            // DG header: ext=0, crc=1, seg=1, userAccess=1, dgType
            dg.Add((byte)(0x70 | (dgType & 0x0F)));
            dg.Add(0x00); // continuity/repetition
            // Session header: lastSeg=1, segNum=0
            dg.Add(0x80);
            dg.Add(0x00);
            // transportIdFlag=1, lenInd=2
            dg.Add(0x12);
            dg.Add((byte)((transportId >> 8) & 0xFF));
            dg.Add((byte)(transportId & 0xFF));
            // Segmentation header: segSize
            int segSize = payload.Length;
            dg.Add((byte)((segSize >> 8) & 0x1F));
            dg.Add((byte)(segSize & 0xFF));
            // Payload
            dg.AddRange(payload);
            // CRC-16-CCITT
            var crc16 = new DABCRC(true, true, 0x1021);
            var crc = crc16.CalcCRC(dg.ToArray());
            dg.Add((byte)((crc >> 8) & 0xFF));
            dg.Add((byte)(crc & 0xFF));
            return dg.ToArray();
        }

        private static byte[] MakeSyntheticAU(byte[] xpadBytes, int xpadInd, bool ciFlag)
        {
            int padLen = xpadBytes.Length + 2;
            byte fpadL1 = (byte)(xpadInd << 4);
            byte fpadL = (byte)(ciFlag ? 0x02 : 0x00);
            var au = new byte[2 + xpadBytes.Length + 2];
            au[0] = 0x80; // DSE ID=4
            au[1] = (byte)padLen;
            for (int i = 0; i < xpadBytes.Length; i++)
            {
                au[2 + i] = xpadBytes[xpadBytes.Length - 1 - i]; // reversed X-PAD
            }
            au[2 + xpadBytes.Length] = fpadL1;
            au[2 + xpadBytes.Length + 1] = fpadL;
            return au;
        }

        private static byte[] MakeDGLI(int dgLen)
        {
            var d = new byte[] { (byte)((dgLen >> 8) & 0x3F), (byte)(dgLen & 0xFF) };
            var crc16 = new DABCRC(true, true, 0x1021);
            var crc = crc16.CalcCRC(d);
            return new byte[] { d[0], d[1], (byte)((crc >> 8) & 0xFF), (byte)(crc & 0xFF) };
        }

        [TestMethod]
        public void MOTDecoder_WithAnnouncedLength_ReassemblesAndReturnsTrue()
        {
            var payload = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01, 0x02, 0x03 };
            var dg = MakeSyntheticDataGroup(4, 42, payload);
            Assert.AreEqual(23, dg.Length);

            var motDecoder = new RTLSDR.DAB.MOT.MOTDecoder();
            motDecoder.SetLen(dg.Length);

            // Chunk 1: first 12 bytes (start)
            var chunk1 = new byte[12];
            Buffer.BlockCopy(dg, 0, chunk1, 0, 12);
            bool complete1 = motDecoder.ProcessDataSubfield(true, chunk1, 0, 12);
            Assert.IsFalse(complete1, "Should not be complete after first chunk");
            Assert.IsTrue(motDecoder.InProgress);

            // Chunk 2: remaining 11 bytes (continuation)
            var chunk2 = new byte[11];
            Buffer.BlockCopy(dg, 12, chunk2, 0, 11);
            bool complete2 = motDecoder.ProcessDataSubfield(false, chunk2, 0, 11);
            Assert.IsTrue(complete2, "Should be complete and CRC valid after second chunk");

            var result = motDecoder.GetMOTDataGroup();
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(dg, result);
        }

        [TestMethod]
        public void MOTDecoder_WithoutAnnouncedLength_InfersLengthAndReturnsTrue()
        {
            var payload = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01, 0x02, 0x03 };
            var dg = MakeSyntheticDataGroup(4, 42, payload);
            Assert.AreEqual(23, dg.Length);

            var motDecoder = new RTLSDR.DAB.MOT.MOTDecoder();
            motDecoder.SetLen(0); // no DGLI available

            // Chunk 1: first 14 bytes (enough to include DG header, session header, segmentation header)
            var chunk1 = new byte[14];
            Buffer.BlockCopy(dg, 0, chunk1, 0, 14);
            bool complete1 = motDecoder.ProcessDataSubfield(true, chunk1, 0, 14);
            Assert.IsFalse(complete1, "Should not be complete after first chunk");
            Assert.IsTrue(motDecoder.InProgress);

            // Chunk 2: remaining 9 bytes (continuation)
            var chunk2 = new byte[9];
            Buffer.BlockCopy(dg, 14, chunk2, 0, 9);
            bool complete2 = motDecoder.ProcessDataSubfield(false, chunk2, 0, 9);
            Assert.IsTrue(complete2, "Should infer length and check CRC successfully");

            var result = motDecoder.GetMOTDataGroup();
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(dg, result);
        }

        [TestMethod]
        public void DynamicLabelDecoder_MOTSlide_ReassemblesCompleteImage()
        {
            var logging = new DummyLoggingService();
            var decoder = new DynamicLabelDecoder(logging);

            DABSlide? decodedSlide = null;
            decoder.OnSlideShowChanged += (s, slide) =>
            {
                decodedSlide = slide;
            };

            // Synthetic PNG payload (12 bytes)
            var bodyData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01, 0x02, 0x03 };

            // MOT Header entity:
            // ContentName param (id 0x0C): len=5 (charset 0 + "test")
            var paramCn = new byte[] { 0xCC, 5, 0, (byte)'t', (byte)'e', (byte)'s', (byte)'t' };
            int headerSize = 7 + paramCn.Length; // 14
            int bodySize = bodyData.Length; // 12
            int contentType = 2; // IMAGE
            int contentSubType = 3; // PNG

            byte b0 = (byte)((bodySize >> 20) & 0xFF);
            byte b1 = (byte)((bodySize >> 12) & 0xFF);
            byte b2 = (byte)((bodySize >> 4) & 0xFF);
            byte b3 = (byte)(((bodySize & 0x0F) << 4) | ((headerSize >> 9) & 0x0F));
            byte b4 = (byte)((headerSize >> 1) & 0xFF);
            byte b5 = (byte)(((headerSize & 1) << 7) | ((contentType & 0x7F) << 1) | ((contentSubType >> 8) & 1));
            byte b6 = (byte)(contentSubType & 0xFF);

            var basicHdr = new byte[] { b0, b1, b2, b3, b4, b5, b6 };
            var headerPayload = new byte[basicHdr.Length + paramCn.Length];
            Buffer.BlockCopy(basicHdr, 0, headerPayload, 0, basicHdr.Length);
            Buffer.BlockCopy(paramCn, 0, headerPayload, basicHdr.Length, paramCn.Length);

            var headerDg = MakeSyntheticDataGroup(3, 42, headerPayload); // 25 bytes
            var bodyDg = MakeSyntheticDataGroup(4, 42, bodyData); // 23 bytes

            // AU 1: Frame with DGLI (len 4) + MOT Header DG start (12 bytes)
            // Raw CIs: (len 4, type 1) = 0x01; (len 12, type 12) = 0x6C; end marker = 0x00
            var dgli1 = MakeDGLI(headerDg.Length);
            var xpad1List = new System.Collections.Generic.List<byte> { 0x01, 0x6C, 0x00 };
            xpad1List.AddRange(dgli1);
            var hdrPart1 = new byte[12];
            Buffer.BlockCopy(headerDg, 0, hdrPart1, 0, 12);
            xpad1List.AddRange(hdrPart1);
            var au1 = MakeSyntheticAU(xpad1List.ToArray(), 2, true);

            // AU 2: Continuation without CI (remaining 13 bytes of headerDg)
            var hdrPart2 = new byte[headerDg.Length - 12];
            Buffer.BlockCopy(headerDg, 12, hdrPart2, 0, hdrPart2.Length);
            var au2 = MakeSyntheticAU(hdrPart2, 2, false);

            // AU 3: Frame with DGLI (len 4) + MOT Body DG start (12 bytes)
            var dgli2 = MakeDGLI(bodyDg.Length);
            var xpad3List = new System.Collections.Generic.List<byte> { 0x01, 0x6C, 0x00 };
            xpad3List.AddRange(dgli2);
            var bodyPart1 = new byte[12];
            Buffer.BlockCopy(bodyDg, 0, bodyPart1, 0, 12);
            xpad3List.AddRange(bodyPart1);
            var au3 = MakeSyntheticAU(xpad3List.ToArray(), 2, true);

            // AU 4: Continuation without CI (remaining 11 bytes of bodyDg)
            var bodyPart2 = new byte[bodyDg.Length - 12];
            Buffer.BlockCopy(bodyDg, 12, bodyPart2, 0, bodyPart2.Length);
            var au4 = MakeSyntheticAU(bodyPart2, 2, false);

            // Feed all 4 AUs
            decoder.ProcessAUData(au1);
            Assert.IsNull(decodedSlide, "Slide should not be ready after AU1");

            decoder.ProcessAUData(au2);
            Assert.IsNull(decodedSlide, "Header complete after AU2, but body not yet received");

            decoder.ProcessAUData(au3);
            Assert.IsNull(decodedSlide, "Slide should not be ready after AU3");

            decoder.ProcessAUData(au4);
            Assert.IsNotNull(decodedSlide, "Slide should be fully decoded after AU4!");
            Assert.AreEqual("image/png", decodedSlide.MimeType);
            Assert.AreEqual("test", decodedSlide.ContentName);
            CollectionAssert.AreEqual(bodyData, decodedSlide.ImageBytes);
        }
    }
}
