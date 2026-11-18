using System;
namespace AdvancedPractice.Models
{
    public static class PracticeMath
    {
        public static float ClampBpm(float target, float mapBpm) => Math.Max(mapBpm * .1f, Math.Min(mapBpm * 5f, target));
        public static float SpeedMultiplier(float targetBpm, float mapBpm) => ClampBpm(targetBpm, mapBpm) / mapBpm;
    }
}
