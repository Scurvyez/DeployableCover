using UnityEngine;

namespace DeployableCover
{
    public static class EasingFuncs
    {
        public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseOutElastic(float t)
        {
            const float c4 = (2 * Mathf.PI) / 3;
            return t == 0 ? 0 : t == 1 ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;
        }
    }
}
