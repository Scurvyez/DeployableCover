using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Verse;
using Verse.AI;

namespace DeployableCover
{
    public class WorkGiver_LoadCharges : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Thing> coverLaunchers = pawn.Map.listerThings.ThingsOfDef(CoreDefOf.SZ_CoverLauncher);
            foreach (var launcher in coverLaunchers)
            {
                yield return launcher;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (thing is Building_CoverLauncher building)
            {
                CompLauncherStorage comp = building.GetComp<CompLauncherStorage>();
                if (comp != null)
                {
                    if (building.IsForbidden(pawn))
                    {
                        return false;
                    }
                    if (!pawn.CanReserveAndReach(building, PathEndMode.Touch, pawn.NormalMaxDanger()))
                    {
                        return false;
                    }
                    if (pawn.Map.designationManager.DesignationOn(building, DesignationDefOf.Deconstruct) != null)
                    {
                        return false;
                    }
                    if (building.IsBurning())
                    {
                        return false;
                    }
                    // Check if there are available charges on the map
                    if (!MapHasAvailableCharges(pawn.Map, pawn))
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing builing, bool forced = false)
	    {
            Thing ammo = FindBestFuel(pawn, builing);
            if (ammo != null)
            {
                Job job = JobMaker.MakeJob(CoreDefOf.SZ_LoadCoverChargesJob, builing, ammo);
                job.count = 1;
                return job;
            }
            return null;
        }

        private bool MapHasAvailableCharges(Map map, Pawn pawn)
        {
            ThingDef ammoDef = CoreDefOf.SZ_CoverCharge;
            return map.listerThings.ThingsOfDef(ammoDef).Exists(ammo => !ammo.IsForbidden(pawn) 
                && pawn.CanReserveAndReach(ammo, PathEndMode.Touch, pawn.NormalMaxDanger()));
        }

        private static Thing FindBestFuel(Pawn pawn, Thing ammo)
        {
            ThingFilter filter = ammo.TryGetComp<CompLauncherStorage>().Props.chargeFilter;
            Predicate<Thing> validator = x => !x.IsForbidden(pawn) && pawn.CanReserve(x);
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(CoreDefOf.SZ_CoverCharge), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
        }
    }
}
