using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;

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
        private int launchAmount = 1;

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
                defaultLabel = "Set launch amount",
                defaultDesc = "Designate how many cover pieces to launch.",
                icon = LaunchIcon,
                action = SetLaunchAmount,
                disabled = false,
            };

            yield return new Command_Action
            {
                defaultLabel = "Deploy cover",
                defaultDesc = "Designate an area for launching deployable cover.",
                icon = LaunchIcon,
                action = LaunchCover,
                disabled = !CanLaunchNow
            };
        }

        private void SetLaunchAmount()
        {
            int amountAvail = compLauncherStorage.innerContainer.Count;
            if (amountAvail > 0)
            {
                Find.WindowStack.Add(new Dialog_Slider("Select Launch Amount", 1, amountAvail, value =>
                {
                    launchAmount = Mathf.RoundToInt(value);
                }));
            }
        }

        private void LaunchCover()
        {
            Map map = Map;
            IntVec3 position = Position;
            IEnumerable<IntVec3> targetCells = GenRadial.RadialCellsAround(position, launcherExtension.launchRadius, true);

            Find.Selector.ClearSelection();
            Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetLocations = true,
                validator = targetCell =>
                    targetCell.Cell.InBounds(map) &&
                    targetCell.Cell.Standable(map) &&
                    targetCell.Cell.GetFirstBuilding(map) == null &&
                    !targetCell.Cell.Fogged(map)
            }, delegate (LocalTargetInfo target)
            {
                MakeFlyers(target.Cell, map);
                lastLaunchTick = Find.TickManager.TicksGame;
            });
        }

        public void MakeFlyers(IntVec3 target, Map map)
        {
            List<Thing> ammoList = compLauncherStorage.GetDirectlyHeldThings().ToList();
            int ammoToLaunch = Mathf.Min(launchAmount, ammoList.Count);

            for (int i = 0;  i < ammoToLaunch; i++)
            {
                Thing ammo = compLauncherStorage.GetDirectlyHeldThings().RandomElement();
                if (ammo != null)
                {
                    CoverFlyer flyer = ThingMaker.MakeThing(CoreDefOf.SW_CoverFlyer, ammo.Stuff) as CoverFlyer;
                    if (flyer != null)
                    {
                        flyer.CoverStartCell = this.Position;
                        flyer.CoverDestCell = GetRandomCellInRadius(target, 5, map);

                        GenSpawn.Spawn(flyer, flyer.CoverDestCell, map);
                    }
                    compLauncherStorage.innerContainer.Remove(ammo);
                }
            }
        }

        private IntVec3 GetRandomCellInRadius(IntVec3 center, int radius, Map map)
        {
            IEnumerable<IntVec3> cellsInRadius = GenRadial.RadialCellsAround(center, radius, true).Where(cell =>
                cell.IsValid && !cell.Fogged(map) && cell.Walkable(map) && cell.GetFirstBuilding(map) == null);
            return cellsInRadius.RandomElement();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastLaunchTick, "lastLaunchTick", -1);
            Scribe_Values.Look(ref launchAmount, "launchAmount", 1);
        }
    }
}
