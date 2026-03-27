using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RTLSDR.Common;

namespace Tests
{
    [TestClass]
    public class RTLSDRCommonTests
    {
        [TestMethod]
        public void SByteExtensions_CloneArray_PartialClone()
        {
            var src = new sbyte[] { (sbyte)1, (sbyte)2, (sbyte)3, (sbyte)4 };
            var clone = src.CloneArray(2);

            Assert.HasCount(2, clone);
            Assert.AreEqual(src[0], clone[0]);
            Assert.AreEqual(src[1], clone[1]);
        }

        [TestMethod]
        public void AudioTools_GetBitRateAsString_KbAndMb()
        {
            var kb = AudioTools.GetBitRateAsString(32000); // 32 kb/s
            Assert.Contains("Kb/s", kb);
            Assert.Contains("32", kb);

            var mb = AudioTools.GetBitRateAsString(2_000_000); // 2 Mb/s
            Assert.Contains("Mb/s", mb);
            Assert.Contains("2.00", mb);
        }

        [TestMethod]
        public void AudioTools_ParseFreq_VariousFormats()
        {
            Assert.AreEqual(103200000, AudioTools.ParseFreq("103.2 mhz"));
            Assert.AreEqual(103200000, AudioTools.ParseFreq("103,2 mhz"));
            Assert.AreEqual(108000000, AudioTools.ParseFreq("108 000 000"));
            // DAB block should be handled
            Assert.AreEqual(AudioTools.DabFrequenciesHz["5A"], AudioTools.ParseFreq("5A"));
        }

        [TestMethod]
        public void AudioTools_ParseDab_Tests()
        {
            Assert.IsTrue(AudioTools.ParseDab("8C", out var freq));
            Assert.AreEqual(AudioTools.DabFrequenciesHz["8C"], freq);
        }

        [TestMethod]
        public void PowerCalculation_GetCurrentPower_And_Avg()
        {
            Assert.AreEqual(0, PowerCalculation.GetCurrentPower(0, 0));

            // prepare small IQ byte array where each I=128 -> I-127 = 1, Q=127 -> Q-127 =0
            var iqBytes = new byte[] { 128, 127, 128, 127 };
            // bytesRead parameter: give number of pairs
            double avg = PowerCalculation.GetAvgPower(iqBytes, iqBytes.Length / 2, 2);
            var expected = PowerCalculation.GetCurrentPower(1, 0);
            Assert.AreEqual(expected, avg, 1e-6);

            // short version
            var iqShorts = new short[] { 1, 0, 1, 0 };
            double avgShort = PowerCalculation.GetAvgPower(iqShorts, 2);
            var expectedShort = PowerCalculation.GetCurrentPower(1, 0);
            Assert.AreEqual(expectedShort, avgShort, 1e-6);
        }

        [TestMethod]
        public void Wave_CreateWriteClose_CreatesValidWave()
        {
            var desc = new AudioDataDescription
            {
                SampleRate = 48000,
                Channels = 2,
                BitsPerSample = 16
            };

            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".wav");
            var wav = new Wave();
            wav.CreateWaveFile(tmp, desc);

            var sampleData = new byte[100];
            wav.WriteSampleData(sampleData);
            wav.CloseWaveFile();

            Assert.IsTrue(File.Exists(tmp));
            var b = File.ReadAllBytes(tmp);
            // basic header checks
            Assert.AreEqual((byte)'R', b[0]);
            Assert.AreEqual((byte)'I', b[1]);
            Assert.AreEqual((byte)'F', b[2]);
            Assert.AreEqual((byte)'F', b[3]);
            Assert.AreEqual((byte)'W', b[8]);
            Assert.AreEqual((byte)'A', b[9]);
            Assert.AreEqual((byte)'V', b[10]);
            Assert.AreEqual((byte)'E', b[11]);

            // find data chunk and verify size equals 100
            int dataIndex = -1;
            for (int i = 0; i < b.Length - 4; i++)
            {
                if (b[i] == (byte)'d' && b[i + 1] == (byte)'a' && b[i + 2] == (byte)'t' && b[i + 3] == (byte)'a')
                {
                    dataIndex = i;
                    break;
                }
            }

            Assert.IsGreaterThan(0, dataIndex);
            uint dataSize = BitConverter.ToUInt32(b, dataIndex + 4);
            Assert.AreEqual((uint)sampleData.Length, dataSize);

            // cleanup
            File.Delete(tmp);
        }
    }
}
