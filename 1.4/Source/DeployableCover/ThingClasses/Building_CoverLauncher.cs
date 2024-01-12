﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace DeployableCover
{
    [StaticConstructorOnStartup]
    public class Building_CoverLauncher : Building, ITargetingSource
    {
        public static readonly Texture2D LaunchIcon = ContentFinder<Texture2D>.Get("DeployableCover/UI/Commands/LaunchCover");
        private LauncherExtension launcherExtension;
        private CompPowerTrader compPowerTrader;
        private CompLauncherStorage compLauncherStorage;
        private int lastLaunchTick = -1;

        private int TimeSinceLastLaunch => Find.TickManager.TicksGame - lastLaunchTick;

        /*private bool CanLaunchNow => compPowerTrader.PowerOn
            && TimeSinceLastLaunch > launcherExtension.cooldownTicks
            && compLauncherStorage.innerContainer.Count > 0;*/

        public virtual bool MultiSelect => true;
        public virtual bool CasterIsPawn => false;
        public virtual Pawn CasterPawn => null;
        public virtual bool IsMeleeAttack => false;
        public virtual bool HidePawnTooltips => true;
        public virtual bool Targetable => true;
        public virtual Thing Caster => this;
        public virtual Verb GetVerb => null;
        public virtual Texture2D UIIcon => null;
        public virtual TargetingParameters targetParams => new TargetingParameters
        {
            canTargetLocations = true
        };
        public virtual ITargetingSource DestinationSelector => null;

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
                action = ,
                disabled = !ValidateTarget() && !CanHitTarget()
            };
        }

        /*
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
                MakeFlyer(target.Cell, map);
                lastLaunchTick = Find.TickManager.TicksGame;
            });
        }
        */

        public virtual void OrderForceTarget(LocalTargetInfo target)
        {
            
        }

        public virtual bool CanHitTarget(LocalTargetInfo target)
        {
            if (Caster == null || !Caster.Spawned)
            {
                return false;
            }
            return true;
        }

        public virtual bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!target.Cell.InBounds(Map) && !target.Cell.Standable(Map) && target.Cell.GetFirstBuilding(Map) != null)
            {
                return false;
            }
            return true;
        }

        public virtual void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }

        public virtual void OnGUI(LocalTargetInfo target) /* FIX THIS LATER */
        {
            Texture2D icon = ((!target.IsValid) ? TexCommand.CannotShoot : ((!(UIIcon != BaseContent.BadTex)) ? TexCommand.Attack : UIIcon));
            GenUI.DrawMouseAttachment(icon);
        }

        public void MakeFlyer(IntVec3 target, Map map)
        {
            Thing ammo = compLauncherStorage.GetDirectlyHeldThings().RandomElement();
            if (ammo != null)
            {
                CoverFlyer flyer = ThingMaker.MakeThing(CoreDefOf.SW_CoverFlyer, ammo.Stuff) as CoverFlyer;
                if (flyer != null)
                {
                    flyer.CoverStartCell = this.Position;
                    flyer.CoverDestCell = target;

                    GenSpawn.Spawn(flyer, flyer.CoverDestCell, map);
                }
                compLauncherStorage.innerContainer.Remove(ammo);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastLaunchTick, "lastLaunchTick", -1);
        }
    }
}
