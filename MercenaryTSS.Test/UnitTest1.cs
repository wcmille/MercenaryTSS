using VRageMath;

namespace MercenaryTSS.Test
{
    [TestClass]
    public class UnitTest1
    {
        int screenHeight = 512;
        int screenWidth = 1024;

        [TestMethod]
        public void TestX1Y0Z0()
        {
            var loc = new Vector3D(1.0,0.0,0.0);
            var pos=PlanetMapTSS.GPSToVector(loc, screenWidth, screenHeight);
            Assert.AreEqual(new Vector2(256,256), pos);
        }
        [TestMethod]
        public void TestX0Y0Z1()
        {
            var loc = new Vector3D(0.0, 0.0, 1.0);
            var pos = PlanetMapTSS.GPSToVector(loc, screenWidth, screenHeight);
            Assert.AreEqual(new Vector2(512, 256), pos);
        }
        [TestMethod]
        public void TestXn1Y0Z0()
        {
            var loc = new Vector3D(-1.0, 0.0, 0.0);
            var pos = PlanetMapTSS.GPSToVector(loc, screenWidth, screenHeight);
            Assert.AreEqual(new Vector2(768, 256), pos);
        }
        [TestMethod]
        public void TestX0Y0Zn1()
        {
            var loc = new Vector3D(0.0, 0.0, -1.0);
            var pos = PlanetMapTSS.GPSToVector(loc, screenWidth, screenHeight);
            Assert.AreEqual(new Vector2(0, 256), pos);
        }
        [TestMethod]
        public void TestX0Y1Z0()
        {
            var loc = new Vector3D(0.0, 1.0, 0.0);
            var pos = PlanetMapTSS.GPSToVector(loc, screenWidth, screenHeight);
            Assert.AreEqual(new Vector2(512, 512), pos);
        }
    }
}