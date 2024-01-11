using UnityEngine;
using Verse;
using Verse.Sound;

namespace DeployableCover
{
    public class Building_DeployableCover : Building
    {
        private int timeHit;
        private float curScale;
        private float originalScale;
        private int ticksScaling;
        private bool gotHit;
        private float damageAmount;

        private CoverExtension coverExtension;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            coverExtension = def.GetModExtension<CoverExtension>();
            ticksScaling = 0;
            originalScale = def.graphic.drawSize.x;
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            damageAmount = totalDamageDealt;
            if (totalDamageDealt > 0)
            {
                timeHit = Find.TickManager.TicksGame;
                gotHit = true;
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (gotHit)
            {
                int elapsedTicks = Find.TickManager.TicksGame - timeHit;
                if (elapsedTicks < coverExtension.wobbleDuration)
                {
                    float progress = (float)elapsedTicks / coverExtension.wobbleDuration;
                    float easedProgress = EasingFuncs.EaseOutElastic(progress);
                    curScale = Mathf.Lerp(coverExtension.minScale * (1f - Mathf.Clamp01(damageAmount)), originalScale, easedProgress);
                }
            }
            else
            {
                gotHit = false;
                curScale = originalScale;
            }
        }

        public override void Draw()
        {
            Vector3 drawPos = DrawPos;
            float scaleY = Mathf.Lerp(-1f, 0f, curScale);
            drawPos.z += def.graphicData.drawSize.y * scaleY / 2;
            drawPos.y = def.altitudeLayer.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Rotation.AsQuat, new Vector3(curScale, 1f, curScale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0);
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref timeHit, "timeHit", 0);
            Scribe_Values.Look(ref curScale, "curScale", 0f);
            Scribe_Values.Look(ref ticksScaling, "ticksScaling", 0);
            Scribe_Values.Look(ref gotHit, "gotHit", false);
        }
    }
}
