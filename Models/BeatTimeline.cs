using System;
using System.Collections.Generic;
using System.Linq;
namespace AdvancedPractice.Models
{
    public sealed class BeatTimeline
    {
        public struct Change
        {
            public double Seconds, Bpm;
            public Change(double seconds, double bpm) { Seconds = seconds; Bpm = bpm; }
        }
        private readonly List<Change> changes;
        public BeatTimeline(double bpm, IEnumerable<Change> events)
        {
            if (!(bpm > 0) || double.IsInfinity(bpm)) throw new ArgumentOutOfRangeException(nameof(bpm));
            changes = new List<Change> { new Change(0, bpm) };
            foreach (var e in events.OrderBy(e => e.Seconds))
            {
                if (e.Seconds < 0 || !(e.Bpm > 0) || double.IsInfinity(e.Bpm)) continue;
                if (e.Seconds == changes[changes.Count - 1].Seconds) changes[changes.Count - 1] = e;
                else changes.Add(e);
            }
        }
        public double BpmAt(double seconds) => changes.Last(c => c.Seconds <= Math.Max(0, seconds)).Bpm;
        public double ToBeat(double seconds)
        {
            double beat = 0;
            for (int i = 0; i < changes.Count; i++)
            {
                double end = i + 1 < changes.Count ? Math.Min(seconds, changes[i + 1].Seconds) : seconds;
                if (end > changes[i].Seconds) beat += (end - changes[i].Seconds) * changes[i].Bpm / 60;
                if (end >= seconds) break;
            }
            return beat;
        }
        public double ToSeconds(double beat)
        {
            beat = Math.Max(0, beat);
            for (int i = 0; i < changes.Count; i++)
            {
                double span = i + 1 < changes.Count ? (changes[i + 1].Seconds - changes[i].Seconds) * changes[i].Bpm / 60 : double.PositiveInfinity;
                if (beat <= span) return changes[i].Seconds + beat * 60 / changes[i].Bpm;
                beat -= span;
            }
            throw new InvalidOperationException();
        }
    }
}
