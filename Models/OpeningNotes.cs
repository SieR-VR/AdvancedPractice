using System;
using System.Collections.Generic;
namespace AdvancedPractice.Models
{
    public static class OpeningNotes
    {
        public static T[] FindForHands<T>(IReadOnlyList<T> sortedNotes, double startSeconds, Func<T, double> time, Func<T, int> hand) where T : class
        {
            var result = new T[2];
            foreach (var note in sortedNotes)
            {
                if (time(note) < startSeconds - .0001) continue;
                int index = hand(note);
                if (index < 0 || index > 1 || result[index] != null) continue;
                result[index] = note;
                if (result[0] != null && result[1] != null) break;
            }
            return result;
        }
        public static T[] Find<T>(IReadOnlyList<T> sortedNotes, double startSeconds, Func<T, double> time)
        {
            const double tolerance = .0001;
            int low = 0, high = sortedNotes.Count;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (time(sortedNotes[middle]) < startSeconds - tolerance) low = middle + 1;
                else high = middle;
            }
            if (low == sortedNotes.Count) return new T[0];
            double firstTime = time(sortedNotes[low]);
            var result = new List<T>();
            while (low < sortedNotes.Count && Math.Abs(time(sortedNotes[low]) - firstTime) <= tolerance)
                result.Add(sortedNotes[low++]);
            return result.ToArray();
        }
    }
}
