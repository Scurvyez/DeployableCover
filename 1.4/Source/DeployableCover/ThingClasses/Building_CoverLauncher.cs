using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DeployableCover
{
    [StaticConstructorOnStartup]
    public class Building_CoverLauncher : Building
    {
        public static readonly Texture2D LaunchIcon = ContentFinder<Texture2D>.Get("DeployableCover/UI/Commands/LaunchCover");
        private LauncherExtension launcherExtension;
        private CompPowerTrader compPowerTrader;
        private CompLauncherStorage compLauncherStorage;
        private int lastLaunchTick = -1;

        private int TimeSinceLastLaunch => Find.TickManager.TicksGame - lastLaunchTick;
        private bool CanLaunchNow => compPowerTrader.PowerOn
            && TimeSinceLastLaunch > launcherExtension.cooldownTicks
            && compLauncherStorage.innerContainer.Count > 0;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            launcherExtension = def.GetModExtension<LauncherExtension>();
            compPowerTrader = GetComp<CompPowerTrader>();
            compLauncherStorage = GetComp<CompLauncherStorage>();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "Deploy cover",
                defaultDesc = "Designate an area for launching deployable cover.",
                icon = LaunchIcon,
                action = LaunchCoverArea,
                disabled = !CanLaunchNow
            };
        }

        private void LaunchCoverArea()
        {
            Map map = Map;
            IntVec3 position = Position;
            IEnumerable<IntVec3> targetCells = GenRadial.RadialCellsAround(position, launcherExtension.launchRadius, true);

            // Open a selector to allow the player to choose a specific cell
            Find.Selector.ClearSelection();
            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetLocations = true,
                validator = targetCell =>
                    targetCell.Cell.InBounds(map) &&
                    targetCell.Cell.Standable(map) &&
                    targetCell.Cell.GetFirstBuilding(map) == null &&
                    targetCells.Contains(targetCell.Cell),
            }, delegate (LocalTargetInfo target)
            {
                MakeCover(target.Cell, map);
                lastLaunchTick = Find.TickManager.TicksGame;
            });
        }

        public void MakeCover(IntVec3 target, Map map)
        {
            Thing coverTemp = compLauncherStorage.GetDirectlyHeldThings().RandomElement();

            if (coverTemp != null)
            {
                Building_InflatableCover inflatableCover = ThingMaker.MakeThing(CoreDefOf.SZ_DeployableCover, coverTemp.Stuff) as Building_InflatableCover;

                if (inflatableCover != null)
                {
                    inflatableCover.CoverStartCell = this.Position;
                    inflatableCover.CoverDestCell = target;

                    GenSpawn.Spawn(inflatableCover, inflatableCover.CoverDestCell, map);
                    inflatableCover.SetFaction(Faction.OfPlayer);
                }

                compLauncherStorage.innerContainer.Remove(coverTemp);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastLaunchTick, "lastLaunchTick", -1);
        }
    }
}
