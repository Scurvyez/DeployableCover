using Verse;

namespace DeployableCover
{
    public class CompProperties_LauncherStorage : CompProperties
    {
        public int maxChargeStorage = 10;
        public int minRequiredCharges = 1;
        public ThingFilter chargeFilter;

        public CompProperties_LauncherStorage()
        {
            compClass = typeof(CompLauncherStorage);
        }
    }
}
