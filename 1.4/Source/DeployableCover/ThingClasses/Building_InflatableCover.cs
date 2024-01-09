using UnityEngine;
using Verse;

namespace DeployableCover
{
    public class Building_InflatableCover : Building
    {
        private float curScale;
        private int spawnTime;
        private int timeSinceSpawn;
        private int timeReachedDestination;
        private int timeSinceDestReached;
        private bool reachedDestination;
        private Vector3 curDrawPos;

        // flyer data
        public IntVec3 CoverStartCell { get; set; }
        public IntVec3 CoverDestCell { get; set; }
        protected int ticksFlying;

        private CoverExtension coverExtension;

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
            // check if the building has reached its destination and finished scaling
            if (reachedDestination && timeSinceDestReached >= coverExtension.maxInflateTicks)
            {
                // set curScale to maxScale if the building has finished scaling
                curScale = coverExtension.maxScale;
            }
            else
            {
                // if not, interpolate the scaleY based on the current scaling phase
                float scaleY = Mathf.Lerp(-1f, 0f, curScale);
                curDrawPos.z += def.graphicData.drawSize.y * scaleY / 2;
            }

            curDrawPos.y = def.altitudeLayer.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(curDrawPos, Rotation.AsQuat, new Vector3(curScale, 1f, curScale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0, null, 0, null, false, false, false);
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
