using System;
using AdvancedPractice.Models;
static class Program
{
    sealed class HintNote
    {
        public double Time;
        public int Hand;
        public HintNote(double time, int hand) { Time = time; Hand = hand; }
    }
    static int assertions;
    static void Equal(double expected, double actual)
    {
        assertions++;
        if (Math.Abs(expected - actual) > .0001) throw new Exception($"Expected {expected}, got {actual}");
    }
    static void Main()
    {
        var timeline = new BeatTimeline(120, new[] { new BeatTimeline.Change(10, 180), new BeatTimeline.Change(20, 60) });
        Equal(20, timeline.ToBeat(10)); Equal(50, timeline.ToBeat(20));
        Equal(30, timeline.ToSeconds(60)); Equal(180, timeline.BpmAt(10));
        Equal(60, timeline.BpmAt(20)); Equal(0, timeline.ToSeconds(-10));
        for (int i = 0; i < 1000; i++) Equal(i / 10.0, timeline.ToBeat(timeline.ToSeconds(i / 10.0)));
        var duplicate = new BeatTimeline(120, new[] { new BeatTimeline.Change(0, 90), new BeatTimeline.Change(0, 150), new BeatTimeline.Change(4, 0) });
        Equal(150, duplicate.BpmAt(0)); Equal(10, duplicate.ToBeat(4));
        Equal(.5, PracticeMath.SpeedMultiplier(62.5f, 125));
        Equal(.1, PracticeMath.SpeedMultiplier(-10, 120)); Equal(5, PracticeMath.SpeedMultiplier(9999, 120));
        var selections = new PracticeSelectionStore<(string level, string characteristic, string difficulty)>();
        var expert = ("song-a", "Standard", "Expert");
        var expertPlus = ("song-a", "Standard", "ExpertPlus");
        var oneSaber = ("song-a", "OneSaber", "Expert");
        var otherSong = ("song-b", "Standard", "Expert");
        var initial = selections.RestoreOrCreate(expert, 155, 16);
        Equal(155, initial.TargetBpm);
        Equal(1, PracticeMath.SpeedMultiplier(initial.TargetBpm, 155));
        Equal(0, initial.Beat); Equal(16, initial.Njs);
        selections.Save(expert, new PracticeSelection(45, 150, 12.5f));
        if (!selections.TryRestore(expert, out var restored)) throw new Exception("Selection lost on return.");
        Equal(45, restored.Beat); Equal(150, restored.TargetBpm); Equal(12.5, restored.Njs);
        Equal(18.333333, timeline.ToSeconds(restored.Beat));
        Equal(150.0 / 180, PracticeMath.SpeedMultiplier(restored.TargetBpm, (float)timeline.BpmAt(timeline.ToSeconds(restored.Beat))));
        foreach (var chart in new[] { expertPlus, oneSaber, otherSong })
        {
            assertions++;
            if (selections.TryRestore(chart, out _)) throw new Exception("Selection leaked to another chart.");
        }
        selections.Save(expertPlus, new PracticeSelection(10, 90, 20));
        selections.Save(expert, new PracticeSelection(50, 155, 13));
        selections.TryRestore(expert, out restored);
        Equal(50, restored.Beat); Equal(155, restored.TargetBpm); Equal(13, restored.Njs);
        selections.TryRestore(expertPlus, out restored);
        Equal(10, restored.Beat); Equal(90, restored.TargetBpm); Equal(20, restored.Njs);
        var returned = selections.RestoreOrCreate(expert, 155, 16);
        Equal(155, returned.TargetBpm); Equal(50, returned.Beat); Equal(13, returned.Njs);
        Equal(200, selections.RestoreOrCreate(otherSong, 200, 18).TargetBpm);
        double[] noteTimes = { 1, 2, 2, 3, 5 };
        Equal(1, OpeningNotes.Find(noteTimes, 0, t => t)[0]);
        Equal(2, OpeningNotes.Find(noteTimes, 1.5, t => t).Length);
        Equal(2, OpeningNotes.Find(noteTimes, 2, t => t).Length);
        Equal(3, OpeningNotes.Find(noteTimes, 2.001, t => t)[0]);
        Equal(0, OpeningNotes.Find(noteTimes, 6, t => t).Length);
        Equal(0, OpeningNotes.Find(new double[0], 0, t => t).Length);
        Equal(5, OpeningNotes.Find(noteTimes, 5, t => t)[0]);
        HintNote[] staggered = { new HintNote(1, 0), new HintNote(1.5, 0), new HintNote(2, 1), new HintNote(3, 0), new HintNote(3, 1) };
        var hands = OpeningNotes.FindForHands(staggered, 0, n => n.Time, n => n.Hand);
        Equal(2, hands.Length); Equal(1, hands[0].Time); Equal(2, hands[1].Time);
        hands = OpeningNotes.FindForHands(staggered, 1.6, n => n.Time, n => n.Hand);
        Equal(3, hands[0].Time); Equal(2, hands[1].Time);
        hands = OpeningNotes.FindForHands(staggered, 3, n => n.Time, n => n.Hand);
        Equal(3, hands[0].Time); Equal(3, hands[1].Time);
        hands = OpeningNotes.FindForHands(new[] { new HintNote(1, 1) }, 0, n => n.Time, n => n.Hand);
        assertions++;
        if (hands[0] != null || hands[1] == null) throw new Exception("Missing hand must retain its empty slot.");
        hands = OpeningNotes.FindForHands(staggered, 4, n => n.Time, n => n.Hand);
        assertions++;
        if (hands[0] != null || hands[1] != null) throw new Exception("Past notes must not appear in hints.");
        hands = OpeningNotes.FindForHands(new[] { new HintNote(0, -1), new HintNote(2, 0), new HintNote(4, 1) }, 0, n => n.Time, n => n.Hand);
        Equal(2, hands[0].Time); Equal(4, hands[1].Time);
        Console.WriteLine($"Passed {assertions} assertions (timing, speed bounds, selection restore and chart isolation).");
    }
}
