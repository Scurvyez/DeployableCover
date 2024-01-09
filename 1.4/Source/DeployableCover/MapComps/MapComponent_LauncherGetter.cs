using System.Collections.Generic;
using Verse;

namespace DeployableCover
{
    public class MapComponent_LauncherGetter : MapComponent
    {
        public Dictionary<IntVec3, HashSet<Building_CoverLauncher>> LauncherLocations = new Dictionary<IntVec3, HashSet<Building_CoverLauncher>>();

        public MapComponent_LauncherGetter(Map map) : base(map) { }
    }
}
