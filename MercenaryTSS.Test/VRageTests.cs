using VRageMath;

namespace MercenaryTSS.Test;

[TestClass]
public class VRageTests
{
    [TestMethod]
    public void CheckItem1()
    {
        Vector3D grav = new Vector3D(0, -3, 1);
        Assert.AreEqual(1, grav.AbsMaxComponent(),"AbsMaxComponent");
    }

    [TestMethod]
    public void CheckItem2()
    {
        Vector3D grav = new Vector3D(0, -3, -5);
        Assert.AreEqual(2, grav.AbsMaxComponent(), "AbsMaxComponent");
    }
}
