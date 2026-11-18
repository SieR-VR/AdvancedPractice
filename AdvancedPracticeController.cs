using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AdvancedPractice.Configuration;
using AdvancedPractice.Models;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using HarmonyLib;
using HMUI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedPractice
{
    public class AdvancedPracticeController : MonoBehaviour
    {
        private static readonly PracticeSelectionStore<BeatmapKey> selections = new PracticeSelectionStore<BeatmapKey>();
        private BeatmapKey activeChart;
        private PracticeViewController view;
        private BeatTimeline timeline;
        private NoteJumpSpeedEventData[] njsEvents;
        private GameObject panel;
        private RectTransform playRect;
        private Vector2 originalPlayPosition;
        private bool playMoved;
        private readonly List<GameObject> hiddenNativeObjects = new List<GameObject>();
        private CancellationTokenSource cancellation;
        private float beat, speed, njs, defaultNjs, maxTime;
        private bool ready, syncing;
        internal static PracticeSettings SelectedSettings;
        internal static float SelectedNjs;
        internal static string SelectedLevel;
        [UIComponent("beat-slider")] private SliderSetting beatSlider = null;
        [UIComponent("speed-slider")] private SliderSetting speedSlider = null;
        [UIComponent("njs-slider")] private SliderSetting njsSlider = null;
        [UIComponent("start-info-text")] private TextMeshProUGUI startInfoText = null;
        [UIComponent("speed-info-text")] private TextMeshProUGUI speedInfoText = null;
        [UIValue("beat")] public float Beat { get => beat; set { if (syncing || !ready) return; beat = Mathf.Clamp(Mathf.Round(value), 0, (float)Math.Floor(timeline.ToBeat(maxTime))); Sync(); } }
        [UIValue("speed")] public float Speed { get => speed; set { if (syncing || !ready) return; speed = value; Sync(); } }
        [UIValue("njs")] public float Njs { get => njs; set { if (syncing || !ready) return; njs = Mathf.Clamp(value, 1, 50); Sync(); } }
        private float Time => (float)timeline.ToSeconds(beat);
        [UIValue("start-info")] public string StartInfo => $"{Time:F3} seconds  |  {timeline.BpmAt(Time):0.###} BPM" + (njsEvents.Length > 0 ? $"  |  NJS {MapNjs(Time):0.###}" : "");
        [UIValue("speed-info")] public string SpeedInfo => $"{speed / timeline.BpmAt(Time) * 100:0.###}%";
        [UIAction("beat-format")] public string BeatFormat(float value) => $"{value:0} beat";
        [UIAction("speed-format")] public string SpeedFormat(float value) => $"{speed:0.###} BPM";
        [UIAction("minus-ten")] private void MinusTen() { Speed -= 10; }
        [UIAction("minus-five")] private void MinusFive() { Speed -= 5; }
        [UIAction("minus-one")] private void MinusOne() { Speed -= 1; }
        [UIAction("plus-one")] private void PlusOne() { Speed += 1; }
        [UIAction("plus-five")] private void PlusFive() { Speed += 5; }
        [UIAction("plus-ten")] private void PlusTen() { Speed += 10; }
        [UIAction("reset-njs")] private void ResetNjs() { Njs = defaultNjs; }
        internal static T Field<T>(object owner, string name) => (T)AccessTools.Field(owner.GetType(), name).GetValue(owner);

        internal async Task Load(PracticeViewController owner)
        {
            cancellation?.Cancel();
            cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            ready = false;
            UnbindSliders();
            RestoreNativeControls();
            if (panel != null) { panel.SetActive(false); Destroy(panel); }
            view = owner;
            SelectedSettings = null;
            try
            {
                var key = Field<BeatmapKey>(view, "_beatmapKey");
                var level = Field<BeatmapLevel>(view, "_beatmapLevel");
                var entitlement = Field<BeatmapLevelsEntitlementModel>(view, "_beatmapLevelsEntitlementModel");
                var version = await entitlement.GetLevelDataVersionAsync(key.levelId, token);
                var loaded = await Field<BeatmapLevelsModel>(view, "_beatmapLevelsModel").LoadBeatmapLevelDataAsync(key.levelId, version, token);
                if (loaded.isError) throw new InvalidOperationException("Could not load beatmap data.");
                var data = await new BeatmapDataLoader().LoadBeatmapDataAsync(loaded.beatmapLevelData, key, level.beatsPerMinute, false, null, null, version, null, null, false);
                token.ThrowIfCancellationRequested();
                if (data == null) throw new InvalidOperationException("Beatmap data unavailable.");
                timeline = new BeatTimeline(level.beatsPerMinute, data.GetBeatmapDataItems<BPMChangeBeatmapEventData>(0).Select(e => new BeatTimeline.Change(e.time, e.bpm)));
                njsEvents = data.GetBeatmapDataItems<NoteJumpSpeedEventData>(0).OrderBy(e => e.time).ToArray();
                maxTime = Mathf.Max(0, level.songDuration - 1);
                defaultNjs = key.difficulty.NoteJumpMovementSpeed(level.GetDifficultyBeatmapData(key.beatmapCharacteristic, key.difficulty).noteJumpMovementSpeed, false);
                activeChart = key;
                // The game's practice speed persists globally across songs. Never use
                // that multiplier as the initial target BPM of a newly selected chart.
                var selection = selections.RestoreOrCreate(key, (float)timeline.BpmAt(0), defaultNjs);
                beat = Mathf.Clamp(selection.Beat, 0, (float)Math.Floor(timeline.ToBeat(maxTime)));
                speed = selection.TargetBpm;
                njs = selection.Njs;
                var root = new GameObject("AdvancedPracticeControls", typeof(RectTransform), typeof(LayoutElement));
                root.layer = 5;
                root.GetComponent<LayoutElement>().ignoreLayout = true;
                root.transform.SetParent(view.transform, false);
                panel = root;
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(105, 52);
                rect.anchoredPosition = new Vector2(0, -2);
                using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AdvancedPractice.Views.Practice.bsml")))
                    BSMLParser.Instance.Parse(reader.ReadToEnd(), root, this);
                UseFullWidthSlider(beatSlider);
                UseFullWidthSlider(speedSlider);
                beatSlider.Slider.maxValue = Mathf.Max(1, (float)Math.Floor(timeline.ToBeat(maxTime)));
                beatSlider.Increments = 1;
                njsSlider.Increments = Mathf.Clamp(PluginConfig.Instance.NjsStep, .1f, 10);
                if (float.IsNaN(njsSlider.Increments)) njsSlider.Increments = 1;
                int njsSteps = Mathf.FloorToInt(49 / njsSlider.Increments);
                njsSlider.Slider.maxValue = 1 + njsSteps * njsSlider.Increments;
                njsSlider.Slider.numberOfSteps = njsSteps + 1;
                beatSlider.Slider.numberOfSteps = Mathf.RoundToInt(beatSlider.Slider.maxValue) + 1;
                // Native controls retain their values for preview and Play handling.
                HideNativeControls();
                var content = (RectTransform)root.transform.GetChild(0);
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = Vector2.one;
                content.pivot = new Vector2(.5f, 1);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = Vector2.zero;
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                PlacePlayBelow(content);
                ready = true;
                BindSliders();
                SelectedLevel = key.levelId;
                Sync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested) return;
                Plugin.Log.Error(ex);
                ready = false;
                UnbindSliders();
                if (panel != null) { panel.SetActive(false); Destroy(panel); }
                RestoreNativeControls();
            }
        }
        private void BindSliders()
        {
            // Listen to user gestures directly; programmatic value assignments do not
            // emit this event. Sync's guard also blocks callbacks from range changes.
            beatSlider.UpdateOnChange = speedSlider.UpdateOnChange = njsSlider.UpdateOnChange = false;
            beatSlider.Slider.normalizedValueDidChangeEvent += BeatChanged;
            speedSlider.Slider.normalizedValueDidChangeEvent += SpeedChanged;
            njsSlider.Slider.normalizedValueDidChangeEvent += NjsChanged;
        }
        private void UnbindSliders()
        {
            if (beatSlider != null) beatSlider.Slider.normalizedValueDidChangeEvent -= BeatChanged;
            if (speedSlider != null) speedSlider.Slider.normalizedValueDidChangeEvent -= SpeedChanged;
            if (njsSlider != null) njsSlider.Slider.normalizedValueDidChangeEvent -= NjsChanged;
        }
        private void BeatChanged(TextSlider slider, float normalized) { Beat = beatSlider.Slider.ConvertFromNormalizedValue(normalized); }
        private void SpeedChanged(TextSlider slider, float normalized) { Speed = speedSlider.Slider.ConvertFromNormalizedValue(normalized); }
        private void NjsChanged(TextSlider slider, float normalized) { Njs = njsSlider.Slider.ConvertFromNormalizedValue(normalized); }
        private static void UseFullWidthSlider(SliderSetting setting)
        {
            // BSML's settings prefab reserves 52 units on the right for the slider.
            // These rows have separate headings, so give the slider the entire row.
            var title = setting.transform.Find("Title");
            if (title != null) title.gameObject.SetActive(false);
            var sliderRect = (RectTransform)setting.Slider.transform;
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.pivot = new Vector2(.5f, .5f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
        }
        private void PlacePlayBelow(RectTransform content)
        {
            playRect = (RectTransform)Field<Button>(view, "_playButton").transform;
            originalPlayPosition = playRect.anchoredPosition;
            playMoved = true;
            var corners = new Vector3[4];
            content.GetWorldCorners(corners);
            Vector3 bottom = playRect.parent.InverseTransformPoint((corners[0] + corners[3]) * .5f);
            Vector3 position = playRect.localPosition;
            position.y = bottom.y - 3f - playRect.rect.height * (1 - playRect.pivot.y);
            playRect.localPosition = position;
        }
        private void HideNativeControls()
        {
            // The vanilla section titles are siblings of the sliders, not their children.
            foreach (var text in view.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.transform.IsChildOf(panel.transform)) continue;
                string title = text.text.Trim();
                if (title == "Song Start" || title == "Speed") HideNativeObject(text.gameObject);
            }
            HideNativeObject(Field<TimeSlider>(view, "_songStartSlider").gameObject);
            HideNativeObject(Field<PercentSlider>(view, "_speedSlider").gameObject);
        }
        private void HideNativeObject(GameObject target)
        {
            if (!target.activeSelf) return;
            hiddenNativeObjects.Add(target);
            target.SetActive(false);
        }
        private void RestoreNativeControls()
        {
            if (playMoved && playRect != null) playRect.anchoredPosition = originalPlayPosition;
            playMoved = false;
            foreach (var target in hiddenNativeObjects)
                if (target != null) target.SetActive(true);
            hiddenNativeObjects.Clear();
        }
        private float MapNjs(float seconds)
        {
            var current = njsEvents.LastOrDefault(e => e.time <= seconds);
            if (current == null) return defaultNjs;
            var next = njsEvents.FirstOrDefault(e => e.time > current.time);
            float relative = current.relativeNoteJumpSpeed;
            if (next != null && next.easeType != EaseType.None)
                relative = Mathf.LerpUnclamped(relative, next.relativeNoteJumpSpeed, Interpolation.Interpolate(Mathf.InverseLerp(current.time, next.time, seconds), next.easeType));
            return Mathf.Max(.01f, defaultNjs + relative);
        }
        private void Sync()
        {
            if (!ready || syncing) return;
            syncing = true;
            try
            {
            float bpm = (float)timeline.BpmAt(Time);
            speed = PracticeMath.ClampBpm(speed, bpm);
            speedSlider.Slider.minValue = Mathf.Ceil(bpm * .1f);
            speedSlider.Slider.maxValue = Mathf.Max(speedSlider.Slider.minValue + 1, Mathf.Floor(bpm * 5));
            speedSlider.Slider.numberOfSteps = Mathf.RoundToInt((speedSlider.Slider.maxValue - speedSlider.Slider.minValue)) + 1;
            beatSlider.ReceiveValue(); speedSlider.ReceiveValue(); njsSlider.ReceiveValue();
            beatSlider.Slider.Refresh(); speedSlider.Slider.Refresh(); njsSlider.Slider.Refresh();
            float nativeMaxTime = Field<float>(view, "_maxStartSongTime");
            Field<TimeSlider>(view, "_songStartSlider").value = nativeMaxTime > 0 ? Time / nativeMaxTime : 0;
            var nativeSpeed = Field<PercentSlider>(view, "_speedSlider");
            nativeSpeed.minValue = .1f; nativeSpeed.maxValue = 5; nativeSpeed.numberOfSteps = 0;
            nativeSpeed.value = speed / bpm;
            startInfoText.text = StartInfo;
            speedInfoText.text = SpeedInfo;
            selections.Save(activeChart, new PracticeSelection(beat, speed, njs));
            }
            finally { syncing = false; }
        }
        internal void PreparePlay()
        {
            if (!ready) return;
            Sync();
            SelectedSettings = view.practiceSettings;
            SelectedNjs = njs;
        }
        private void OnDisable()
        {
            cancellation?.Cancel();
            ready = false;
            UnbindSliders();
            if (panel != null) panel.SetActive(false);
            RestoreNativeControls();
        }
        private void OnDestroy()
        {
            cancellation?.Cancel(); cancellation?.Dispose();
        }
    }

    [HarmonyPatch(typeof(PracticeViewController), "DidActivate")]
    internal static class MenuPatch
    {
        private static void Postfix(PracticeViewController __instance)
        {
            var controller = __instance.GetComponent<AdvancedPracticeController>() ?? __instance.gameObject.AddComponent<AdvancedPracticeController>();
            _ = controller.Load(__instance);
        }
    }
    [HarmonyPatch(typeof(PracticeViewController), "HandlePlayButtonPressed")]
    internal static class PlayPatch
    {
        private static void Prefix(PracticeViewController __instance) => __instance.GetComponent<AdvancedPracticeController>()?.PreparePlay();
    }
    [HarmonyPatch(typeof(GameplayCoreInstaller), "InstallBindings")]
    internal static class GameplayPatch
    {
        internal static bool Apply;
        private static void Prefix(GameplayCoreSceneSetupData ____sceneSetupData)
        {
            Apply = ____sceneSetupData.practiceSettings != null &&
                ReferenceEquals(____sceneSetupData.practiceSettings, AdvancedPracticeController.SelectedSettings) &&
                ____sceneSetupData.beatmapKey.levelId == AdvancedPracticeController.SelectedLevel;
        }
        private static Exception Finalizer(Exception __exception) { Apply = false; return __exception; }
    }
    [HarmonyPatch(typeof(BeatmapObjectSpawnController.InitData), MethodType.Constructor, new[] { typeof(float), typeof(int), typeof(float), typeof(BeatmapObjectSpawnMovementData.NoteJumpValueType), typeof(float) })]
    internal static class NjsPatch
    {
        private static void Prefix(ref float noteJumpMovementSpeed)
        {
            if (GameplayPatch.Apply) noteJumpMovementSpeed = AdvancedPracticeController.SelectedNjs;
        }
    }
}
