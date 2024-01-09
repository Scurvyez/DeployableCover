using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace DeployableCover
{
    public class JobDriver_LoadCharges : JobDriver
    {
        private int durationTicks = 120;
        public Building_CoverLauncher LauncherBuilding => job.targetA.Thing as Building_CoverLauncher;
        public CompLauncherStorage LauncherComp => LauncherBuilding.GetComp<CompLauncherStorage>();
        protected Thing ChargeToLoad => job.targetB.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(LauncherBuilding, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(ChargeToLoad, job, 1, -1, null, errorOnFailed);
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            AddEndCondition(() => LauncherComp.NeedsLoading ? JobCondition.Ongoing : JobCondition.Succeeded);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_General.Wait(durationTicks).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
                .WithProgressBarToilDelay(TargetIndex.A);
            yield return FinalizeLoading();
        }

        private Toil FinalizeLoading()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                LauncherComp?.TryLoadCharges(ChargeToLoad);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
