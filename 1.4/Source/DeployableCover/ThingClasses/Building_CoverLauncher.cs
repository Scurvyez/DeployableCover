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
        private Color emptyColor;
        private Color fullColor;
        private static readonly Material unilledBarMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, 0.5f));
        private static readonly Texture2D launchIcon = ContentFinder<Texture2D>.Get("DeployableCover/UI/Commands/LaunchCover");
        private static readonly Texture2D amountToLaunchIcon = ContentFinder<Texture2D>.Get("DeployableCover/UI/Commands/AmountToLaunch");
        private static readonly Texture2D fillStorageIcon = ContentFinder<Texture2D>.Get("DeployableCover/UI/Commands/FillStorage");
        private static readonly Texture2D emptyStorageIcon = ContentFinder<Texture2D>.Get("DeployableCover/UI/Commands/EmptyStorage");
        private LauncherExtension launcherExtension;
        private CompPowerTrader compPowerTrader;
        private CompLauncherStorage compLauncherStorage;
        private int lastLaunchTick = -1;
        private int launchAmount = 1;
        protected List<StuffCategoryDef> debugStuffCategories = new List<StuffCategoryDef>();

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
            debugStuffCategories.Add(StuffCategoryDefOf.Fabric);
            debugStuffCategories.Add(StuffCategoryDefOf.Leathery);
            emptyColor = compLauncherStorage.Props.barEmptyColor;
            fullColor = compLauncherStorage.Props.barFullColor;
        }

        public override void Draw()
        {
            base.Draw();
            DrawStorageBar();
        }

        private void DrawStorageBar()
        {
            GenDraw.FillableBarRequest bar = default;
            bar.center = DrawPos + new Vector3(0f, 0.1f, 0.325f);
            bar.size = new Vector2(1f, 0.35f);
            bar.margin = 0.05f;
            bar.fillPercent = compLauncherStorage.innerContainer.Count / (float)compLauncherStorage.Props.maxChargeStorage;
            bar.unfilledMat = unilledBarMat;
            bar.rotation = Rot4.East;
            bar.filledMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(emptyColor, fullColor, bar.fillPercent));
            GenDraw.DrawFillableBar(bar);
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
                icon = amountToLaunchIcon,
                action = SetLaunchAmount,
                disabled = false,
            };

            yield return new Command_Action
            {
                defaultLabel = "Deploy cover",
                defaultDesc = "Designate an area for launching deployable cover.",
                icon = launchIcon,
                action = LaunchCover,
                disabled = !CanLaunchNow
            };

            if (DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Fill storage",
                    defaultDesc = "Fill all available space with charges.",
                    icon = fillStorageIcon,
                    action = FillStorage,
                };

                yield return new Command_Action
                {
                    defaultLabel = "Empty storage",
                    defaultDesc = "Removes all stored charges.",
                    icon = emptyStorageIcon,
                    action = EmptyStorage,
                };
            }
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
                    targetCell.Cell.InBounds(map) && // cell is in bounds
                    targetCell.Cell.Standable(map) && // can be stood in by a pawn
                    targetCell.Cell.GetFirstBuilding(map) == null && // has no building in it
                    targetCell.Cell.GetRoof(map) == null && // has no roof overhead
                    !targetCell.Cell.Fogged(map) // isn't fogged
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
                        flyer.CoverDestCell = GetRandomCellInRadius(target, launcherExtension.targetRadius, map);

                        GenSpawn.Spawn(flyer, flyer.CoverDestCell, map);
                    }
                    compLauncherStorage.innerContainer.Remove(ammo);
                }
            }
        }

        private IntVec3 GetRandomCellInRadius(IntVec3 center, int radius, Map map)
        {
            IEnumerable<IntVec3> cellsInRadius = GenRadial.RadialCellsAround(center, radius, true).Where(cell =>
                cell.IsValid && // is valid
                !cell.Fogged(map) && // is not fogged
                cell.Walkable(map) && // is walkable
                cell.GetFirstBuilding(map) == null); // has no building in it
            return cellsInRadius.RandomElement();
        }

        private void FillStorage()
        {
            int maxStorage = compLauncherStorage.Props.maxChargeStorage;
            while (compLauncherStorage.innerContainer.Count < maxStorage)
            {
                ThingDef randomStuffDef = GetRandomStuffDef();
                Thing ammo = ThingMaker.MakeThing(CoreDefOf.SZ_CoverCharge, randomStuffDef);

                if (!compLauncherStorage.innerContainer.TryAdd(ammo))
                {
                    break;
                }
            }
        }

        private void EmptyStorage()
        {
            compLauncherStorage.innerContainer.Clear();
        }

        private ThingDef GetRandomStuffDef()
        {
            List<ThingDef> metalStuffDefs = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.IsStuff && def.stuffProps != null && 
                    (def.stuffProps.categories.Contains(StuffCategoryDefOf.Fabric) || def.stuffProps.categories.Contains(StuffCategoryDefOf.Leathery))).ToList();

            if (metalStuffDefs.Count > 0)
            {
                return metalStuffDefs.RandomElement();
            }
            else
            {
                return GetRandomStuffDef();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastLaunchTick, "lastLaunchTick", -1);
            Scribe_Values.Look(ref launchAmount, "launchAmount", 1);
        }
    }
}
