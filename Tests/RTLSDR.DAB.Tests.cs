using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RTLSDR.DAB;
using LoggerService;

namespace Tests
{
    [TestClass]
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
    }
}
