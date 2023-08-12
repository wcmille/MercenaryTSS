using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace MercenaryTSS
{
    [MyTextSurfaceScript("PlanetMapTSSZones", "Planet Map w Zones")]
    public class PlanetMapTSSZones : PlanetMapTSS
    {
        public PlanetMapTSSZones(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base("GVK_KharakMercatorZones", surface, block, size)
        { }
    }
}
