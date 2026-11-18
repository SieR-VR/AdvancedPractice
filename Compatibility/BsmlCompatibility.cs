using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Settings;
using HMUI;
namespace AdvancedPractice.Compatibility
{
    internal static class BsmlCompatibility
    {
        internal static BSMLSettings Settings =>
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
            BSMLSettings.instance;
#else
            BSMLSettings.Instance;
#endif
        internal static RangeValuesTextSlider GetSlider(this SliderSetting setting) =>
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
            setting.slider;
#else
            setting.Slider;
#endif
        internal static float GetIncrements(this SliderSetting setting) =>
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
            setting.increments;
#else
            setting.Increments;
#endif
        internal static void SetIncrements(this SliderSetting setting, float value)
        {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
            setting.increments = value;
#else
            setting.Increments = value;
#endif
        }
        internal static void DisableAutoApply(this SliderSetting setting)
        {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1
            setting.updateOnChange = false;
#else
            setting.UpdateOnChange = false;
#endif
        }
    }
}
