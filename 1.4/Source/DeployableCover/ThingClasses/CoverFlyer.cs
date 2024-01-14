using RimWorld;
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
        private FlyerExtension flyerExtension;
        private int scalingAnimationTicks;

        public IntVec3 CoverStartCell { get; set; }
        public IntVec3 CoverDestCell { get; set; }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            flyerExtension = def.GetModExtension<FlyerExtension>();
            spawnTime = Find.TickManager.TicksGame;
            ticksFlying = 0;
            curDrawPos = CoverStartCell.ToVector3() + flyerExtension.startDrawOffset;
            curScale = flyerExtension.minScale;
        }

        public override void Tick()
        {
            base.Tick();
            timeSinceSpawn = Find.TickManager.TicksGame - spawnTime;
            if (timeSinceSpawn < flyerExtension.maxFlyTicks)
            {
                ticksFlying++;
                float num = (float)ticksFlying / (float)flyerExtension.maxFlyTicks;
                float flyTimeStep = (float)timeSinceSpawn / flyerExtension.maxFlyTicks;
                float easedFlyTimeStep = EasingFuncs.EaseOutQuad(flyTimeStep);

                // Update currentDrawPos during movement phase
                Vector3 targetPos = CalculateCurrentPosition();
                curDrawPos = Vector3.Lerp(curDrawPos, targetPos, easedFlyTimeStep + ticksFlying)
                    + new Vector3(0f, 0f, flyerExtension.curveFactor) * GenMath.InverseParabola(num);
            }
            else if (timeSinceSpawn == flyerExtension.maxFlyTicks)
            {
                reachedDestination = true;
                DrawEffects();
                timeReachedDestination = Find.TickManager.TicksGame;
            }
            else if (reachedDestination)
            {
                timeSinceDestReached = Find.TickManager.TicksGame - timeReachedDestination;
                if (timeSinceDestReached < flyerExtension.maxInflateTicks)
                {
                    scalingAnimationTicks++;
                    float scaleTimeStep = (float)timeSinceDestReached / flyerExtension.maxInflateTicks;
                    float easedScaleTimeStep = EasingFuncs.EaseOutElastic(scaleTimeStep);
                    curScale = Mathf.Lerp(flyerExtension.minScale, flyerExtension.maxScale, easedScaleTimeStep);
                }
                else
                {
                    if (scalingAnimationTicks > 0)
                    {
                        // slight graphical flicker when despaning this and spawning new object :/
                        SpawnAndKill();
                    }
                }
            }
        }

        private Vector3 CalculateCurrentPosition()
        {
            float lerpFactor = (float)timeSinceSpawn / flyerExtension.maxFlyTicks;
            return Vector3.Lerp(CoverStartCell.ToVector3()
                + flyerExtension.startDrawOffset, CoverDestCell.ToVector3()
                + flyerExtension.destDrawOffset, EasingFuncs.EaseOutQuad(lerpFactor));
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
        
        private void SpawnAndKill()
        {
            Thing cover = ThingMaker.MakeThing(CoreDefOf.SZ_DeployableCover, this.Stuff);
            cover.Position = this.Position;
            GenSpawn.Spawn(cover, cover.Position, this.Map);
            cover.SetFaction(Map.ParentFaction);
            this.Destroy();
        }

        private void DrawEffects()
        {
            if (flyerExtension != null && flyerExtension.fleckDef != null)
            {
                FleckCreationData fCD = FleckMaker.GetDataStatic(curDrawPos, Map, flyerExtension.fleckDef);
                fCD.rotationRate = Rand.RangeInclusive(-240, 240);
                fCD.scale = flyerExtension.fleckMaxScale;
                fCD.def.graphicData.drawOffset = new Vector3(0, curDrawPos.y, 0);
                Map.flecks.CreateFleck(fCD);
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
