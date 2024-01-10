using UnityEngine;
using Verse;

namespace DeployableCover
{
    public class FlyerExtension : DefModExtension
    {
        public float minScale = 0.15f;
        public float maxScale = 1.0f;
        public int maxFlyTicks = 1;
        public int maxInflateTicks = 1;
        public float curveFactor = 1f;
        public Vector3 startDrawOffset = new Vector3(0f, 0, 0f);
        public Vector3 destDrawOffset = new Vector3(0f, 0, 0f);
        public FleckDef fleckDef = null;
        public float fleckMaxScale = 1f;
    }
}
