using RimWorld;
using Verse;

namespace DeployableCover
{
    [DefOf]
    public class CoreDefOf
    {
        public static ThingDef SZ_CoverLauncher;
        public static ThingDef SZ_DeployableCover;
        public static ThingDef SZ_CoverCharge;
        public static JobDef SZ_LoadCoverChargesJob;
        public static WorkGiverDef SZ_LoadCoverChargesWork;

        static CoreDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CoreDefOf));
        }
    }
}
