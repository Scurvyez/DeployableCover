using System.Collections.Generic;
using Verse;

namespace DeployableCover
{
    public class CompLauncherStorage : ThingComp, IThingHolder
    {
        public CompProperties_LauncherStorage Props => (CompProperties_LauncherStorage)props;
        public ThingOwner innerContainer;

        public bool NeedsLoading => innerContainer.Count < Props.minRequiredCharges;
        public bool IsEmpty => innerContainer.Count == 0;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            innerContainer = new ThingOwner<Thing>(this);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public bool TryLoadCharges(Thing incoming)
        {
            if (innerContainer.Count == Props.maxChargeStorage)
            {
                return false;
            }
            else if (innerContainer.TryAddOrTransfer(incoming, canMergeWithExistingStacks: false))
            {
                return true;
            }
            return false;
        }

        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            string textExtra = "Stored charges: " + innerContainer.Count + " / " + Props.maxChargeStorage;
            return text + textExtra;
        }
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }
    }
}
