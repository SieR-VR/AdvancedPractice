using System;
using System.Threading.Tasks;
using HarmonyLib;
using HMUI;
using UnityEngine;

namespace AdvancedPractice.Compatibility
{
    internal enum PreviewResult { Loading, Played, Unavailable }

    internal static class PracticePreview
    {
        // Reuse vanilla's clip, loudness correction, crossfade and preview duration.
        private static readonly Action<PracticeViewController> playPreview =
            AccessTools.MethodDelegate<Action<PracticeViewController>>(
                AccessTools.DeclaredMethod(typeof(PracticeViewController), "PlayPreview", Type.EmptyTypes));

        internal static PreviewResult TryPlay(PracticeViewController view, float seconds)
        {
            var loading = AdvancedPracticeController.Field<Task>(view, "_getAudioClipTask");
            if (loading == null || !loading.IsCompleted) return PreviewResult.Loading;
            if (loading.Status != TaskStatus.RanToCompletion ||
                AdvancedPracticeController.Field<AudioClip>(view, "_audioClip") == null)
                return PreviewResult.Unavailable;

            // Audio loading can update the native bounds after our initial UI sync.
            float maxTime = AdvancedPracticeController.Field<float>(view, "_maxStartSongTime");
            var slider = AdvancedPracticeController.Field<TimeSlider>(view, "_songStartSlider");
            slider.value = maxTime > 0 ? Mathf.Clamp01(seconds / maxTime) : 0;
            playPreview(view);
            return PreviewResult.Played;
        }
    }
}
