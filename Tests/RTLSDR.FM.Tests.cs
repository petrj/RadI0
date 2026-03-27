using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RTLSDR.FM;

namespace Tests
{
    /// <summary>
    ///  AI generated tests for RTLSDR.FM. These tests cover basic functionality of the FMDemodulator and FMStereoDecoder classes. They check that methods produce expected outputs for given inputs, such as interleaving stereo samples, converting shorts to bytes, and decoding stereo audio. The tests ensure that the core processing logic in the FM demodulation and stereo decoding works correctly under simple conditions (e.g., all zeros input). More complex test cases can be added to further validate the behavior under various signal conditions.
    /// </summary>
    [TestClass]
    public class RTLSDRFMTests
    {
        [TestMethod]
        public void Move_AddsVector()
        {
            byte[] data = new byte[] { 1, 255 };
            short[] res = FMDemodulator.Move(data, data.Length, 10);
            Assert.AreEqual((short)(1 + 10), res[0]);
            Assert.AreEqual((short)(255 + 10), res[1]);
        }

        [TestMethod]
        public void InterleaveStereo_Works()
        {
            short[] left = new short[] { 1, 2 };
            short[] right = new short[] { 3, 4 };
            short[] interleaved = FMDemodulator.InterleaveStereo(left, right);
            CollectionAssert.AreEqual(new short[] { 1, 3, 2, 4 }, interleaved);
        }

        [TestMethod]
        public void ShortsToBytes_ProducesExpectedBytes()
        {
            short[] samples = new short[] { 1, -2, 30000 };
            byte[] bytes = FMDemodulator.ShortsToBytes(samples);

            byte[] expected = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, expected, 0, expected.Length);

            CollectionAssert.AreEqual(expected, bytes);
        }

        [TestMethod]
        public void PolarDiscriminant_ZeroForAlignedVectors()
        {
            // when both vectors point along real axis (imag=0) and same sign, result angle==0
            short res = FMDemodulator.PolarDiscriminant(123, 0, 123, 0);
            Assert.AreEqual((short)0, res);
        }

        [TestMethod]
        public void DecodeStereo_AllZeros_OutputsZeros()
        {
            var dec = new FMStereoDecoder(96000);
            float[] input = new float[128]; // all zeros

            dec.DecodeStereoFloat(input, out var left, out var right);

            Assert.IsNotNull(left);
            Assert.IsNotNull(right);
            Assert.AreEqual(input.Length, left.Length);
            Assert.AreEqual(input.Length, right.Length);

            // all output samples must be zero
            Assert.IsTrue(left.All(x => x == 0));
            Assert.IsTrue(right.All(x => x == 0));
        }

        [TestMethod]
        public void DecodeStereoFromShort_AllZeros_OutputsZeros()
        {
            var dec = new FMStereoDecoder(96000);
            short[] input = new short[128];

            dec.DecodeStereoFromShort(input, out var left, out var right);

            Assert.IsNotNull(left);
            Assert.IsNotNull(right);
            Assert.AreEqual(input.Length, left.Length);
            Assert.AreEqual(input.Length, right.Length);

            Assert.IsTrue(left.All(x => x == 0));
            Assert.IsTrue(right.All(x => x == 0));
        }
    }
}
