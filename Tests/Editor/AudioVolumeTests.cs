using NUnit.Framework;

namespace KidzDev.Unity.Audio.Tests
{
    sealed class AudioVolumeTests
    {
        [TestCase(1f,    0f)]
        [TestCase(0.5f, -6.02f)]
        [TestCase(0f,  -80f)]
        public void RatioToDB_ConvertsCorrectly(float ratio, float expectedDb)
        {
            Assert.AreEqual(expectedDb, AudioVolume.RatioToDB(ratio), 0.1f);
        }

        [TestCase(0f,   0f)]
        [TestCase(-80f, 0f)]
        [TestCase(0f,   0f)]
        public void DBToRatio_AtMinDb_ReturnsZero(float db, float _)
        {
            Assert.AreEqual(0f, AudioVolume.DBToRatio(-80f), 0.0001f);
        }

        [Test]
        public void RatioToDB_DBToRatio_RoundTrip()
        {
            float ratio = 0.75f;
            float db    = AudioVolume.RatioToDB(ratio);
            float back  = AudioVolume.DBToRatio(db);
            Assert.AreEqual(ratio, back, 0.001f);
        }

        [TestCase(-1f,  0f)]
        [TestCase(0f,   0f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(1f,   1f)]
        [TestCase(2f,   1f)]
        public void Clamp_ClampsToZeroOne(float input, float expected)
        {
            Assert.AreEqual(expected, AudioVolume.Clamp(input), 0.0001f);
        }
    }
}
