using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace MercenaryTSS
{
    [MyTextSurfaceScript("PlanetMapTSSZones", "Planet Map w Zones")]
    public class PlanetMapZonesTSS : PlanetMapTSS
    {
        public PlanetMapZonesTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base("GV_Zone_", surface, block, size)
        { }
    }
}
