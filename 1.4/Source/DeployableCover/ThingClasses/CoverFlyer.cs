using UnityEngine;
using Verse;

namespace DeployableCover
{
    public class CoverFlyer : ThingWithComps
    {
        private float curScale;
        private int spawnTime;
        private int timeSinceSpawn;
        private int timeReachedDestination;
        private int timeSinceDestReached;
        private bool reachedDestination;
        private Vector3 curDrawPos;
        protected int ticksFlying;
        private CoverExtension coverExtension;

        public IntVec3 CoverStartCell { get; set; }
        public IntVec3 CoverDestCell { get; set; }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            coverExtension = def.GetModExtension<CoverExtension>();
            spawnTime = Find.TickManager.TicksGame;
            ticksFlying = 0;
            curDrawPos = CoverStartCell.ToVector3() + coverExtension.startDrawOffset;
            curScale = coverExtension.minScale;
        }

        public override void Tick()
        {
            base.Tick();
            timeSinceSpawn = Find.TickManager.TicksGame - spawnTime;
            if (timeSinceSpawn < coverExtension.maxFlyTicks)
            {
                ticksFlying++;
                float num = (float)ticksFlying / (float)coverExtension.maxFlyTicks;
                float flyTimeStep = (float)timeSinceSpawn / coverExtension.maxFlyTicks;
                float easedFlyTimeStep = EasingFuncs.EaseOutQuad(flyTimeStep);

                // Update currentDrawPos during movement phase
                Vector3 targetPos = CalculateCurrentPosition();
                curDrawPos = Vector3.Lerp(curDrawPos, targetPos, easedFlyTimeStep + ticksFlying)
                    + new Vector3(0f, 0f, coverExtension.curveFactor) * GenMath.InverseParabola(num);
            }
            else if (timeSinceSpawn == coverExtension.maxFlyTicks)
            {
                reachedDestination = true;
                timeReachedDestination = Find.TickManager.TicksGame;
            }
            else
            {
                if (reachedDestination)
                {
                    timeSinceDestReached = Find.TickManager.TicksGame - timeReachedDestination;
                    if (timeSinceDestReached < coverExtension.maxInflateTicks)
                    {
                        // Transition to scaling phase only if the destination has been reached
                        float scaleTimeStep = (float)timeSinceDestReached / coverExtension.maxInflateTicks;
                        float easedscaleTimeStep = EasingFuncs.EaseOutElastic(scaleTimeStep);
                        curScale = Mathf.Lerp(coverExtension.minScale, coverExtension.maxScale, easedscaleTimeStep);

                        TrySpawnAndKill();
                    }
                }
            }
        }

        private Vector3 CalculateCurrentPosition()
        {
            float lerpFactor = (float)timeSinceSpawn / coverExtension.maxFlyTicks;
            return Vector3.Lerp(CoverStartCell.ToVector3()
                + coverExtension.startDrawOffset, CoverDestCell.ToVector3()
                + coverExtension.destDrawOffset, EasingFuncs.EaseOutQuad(lerpFactor));
        }

        public override void Draw()
        {
            Vector3 drawPos = curDrawPos;
            float scaleY = Mathf.Lerp(-1f, 0f, curScale);
            drawPos.z += def.graphicData.drawSize.y * scaleY / 2;
            drawPos.y = def.altitudeLayer.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Rotation.AsQuat, new Vector3(curScale, 1f, curScale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0, null, 0, null, false, false, false);
        }
        
        private void TrySpawnAndKill()
        {
            Thing cover = ThingMaker.MakeThing(CoreDefOf.SZ_DeployableCover, this.Stuff);
            if (cover != null)
            {
                cover.Position = this.Position;
                GenSpawn.Spawn(cover, cover.Position, this.Map);
                cover.SetFaction(Faction);
                this.Destroy();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref curScale, "curScale", 1f);
            Scribe_Values.Look(ref spawnTime, "spawnTime", 0);
            Scribe_Values.Look(ref curDrawPos, "curDrawPos");
            Scribe_Values.Look(ref timeSinceSpawn, "timeSinceSpawn", 0);
            Scribe_Values.Look(ref reachedDestination, "reachedDestination", false);
            Scribe_Values.Look(ref timeReachedDestination, "timeReachedDestination", 0);
            Scribe_Values.Look(ref timeSinceDestReached, "timeSinceDestReached", 0);
        }
    }
}
