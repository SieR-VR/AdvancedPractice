using System.Linq;
using UnityEngine;
namespace AdvancedPractice.Compatibility
{
    internal sealed class NjsTimeline
    {
        private readonly float defaultNjs;
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
        private readonly NoteJumpSpeedEventData[] events;
        internal bool HasChanges => events.Length > 0;
#else
        internal bool HasChanges => false;
#endif
        internal NjsTimeline(IReadonlyBeatmapData data, float defaultNjs)
        {
            this.defaultNjs = defaultNjs;
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            events = data.GetBeatmapDataItems<NoteJumpSpeedEventData>(0).OrderBy(e => e.time).ToArray();
#endif
        }
        internal float ValueAt(float seconds)
        {
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            var current = events.LastOrDefault(e => e.time <= seconds);
            if (current == null) return defaultNjs;
            var next = events.FirstOrDefault(e => e.time > current.time);
            float relative = current.relativeNoteJumpSpeed;
            if (next != null && next.easeType != EaseType.None)
                relative = Mathf.LerpUnclamped(relative, next.relativeNoteJumpSpeed, Interpolation.Interpolate(Mathf.InverseLerp(current.time, next.time, seconds), next.easeType));
            return Mathf.Max(.01f, defaultNjs + relative);
#else
            return defaultNjs;
#endif
        }
    }
}
