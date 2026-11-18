using System.Collections.Generic;
namespace AdvancedPractice.Models
{
    public struct PracticeSelection
    {
        public float Beat, TargetBpm, Njs;
        public PracticeSelection(float beat, float targetBpm, float njs)
        {
            Beat = beat; TargetBpm = targetBpm; Njs = njs;
        }
    }
    // Keep canonical beat/BPM values, not slider positions or derived percentages.
    // A chart key includes level, characteristic and difficulty.
    public sealed class PracticeSelectionStore<TKey>
    {
        private readonly Dictionary<TKey, PracticeSelection> selections = new Dictionary<TKey, PracticeSelection>();
        public void Save(TKey chart, PracticeSelection selection) => selections[chart] = selection;
        public bool TryRestore(TKey chart, out PracticeSelection selection) => selections.TryGetValue(chart, out selection);
        public PracticeSelection RestoreOrCreate(TKey chart, float mapBpm, float defaultNjs)
        {
            return selections.TryGetValue(chart, out var saved) ? saved : new PracticeSelection(0, mapBpm, defaultNjs);
        }
    }
}
