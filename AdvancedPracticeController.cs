using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AdvancedPractice.Configuration;
using AdvancedPractice.Models;
using AdvancedPractice.Compatibility;
using AdvancedPractice.Views;
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
        private NjsTimeline njsTimeline;
        private NoteData[] notes;
        private NoteData[] visibleHintNotes;
        private GameObject noteHint;
        private TextMeshProUGUI noteHintLabel;
        private readonly List<GameObject> noteTiles = new List<GameObject>();
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
        [UIValue("start-info")] public string StartInfo => $"{Time:F3} seconds  |  {timeline.BpmAt(Time):0.###} BPM" + (njsTimeline.HasChanges ? $"  |  NJS {MapNjs(Time):0.###}" : "");
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
            if (noteHint != null) { noteHint.SetActive(false); Destroy(noteHint); }
            noteTiles.Clear();
            view = owner;
            SelectedSettings = null;
            try
            {
                var chart = await PracticeChartLoader.Load(view, token);
                var key = chart.Key;
                var data = chart.Data;
                timeline = new BeatTimeline(chart.Bpm, data.GetBeatmapDataItems<BPMChangeBeatmapEventData>(0).Select(e => new BeatTimeline.Change(e.time, e.bpm)));
                defaultNjs = chart.DefaultNjs;
                njsTimeline = new NjsTimeline(data, defaultNjs);
                notes = data.GetBeatmapDataItems<NoteData>(0)
                    .Where(n => n.gameplayType == NoteData.GameplayType.Normal || n.gameplayType == NoteData.GameplayType.BurstSliderHead)
                    .OrderBy(n => n.time).ThenBy(n => n.lineIndex).ToArray();
                maxTime = Mathf.Max(0, chart.Duration - 1);
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
                    Legato.Presentation.BsmlParser.Instance.Parse(reader.ReadToEnd(), root, this);
                UseFullWidthSlider(beatSlider);
                UseFullWidthSlider(speedSlider);
                beatSlider.GetSlider().maxValue = Mathf.Max(1, (float)Math.Floor(timeline.ToBeat(maxTime)));
                beatSlider.SetIncrements(1);
                njsSlider.SetIncrements(Mathf.Clamp(PluginConfig.Instance.NjsStep, .1f, 10));
                if (float.IsNaN(njsSlider.GetIncrements())) njsSlider.SetIncrements(1);
                int njsSteps = Mathf.FloorToInt(49 / njsSlider.GetIncrements());
                njsSlider.GetSlider().maxValue = 1 + njsSteps * njsSlider.GetIncrements();
                njsSlider.GetSlider().numberOfSteps = njsSteps + 1;
                beatSlider.GetSlider().numberOfSteps = Mathf.RoundToInt(beatSlider.GetSlider().maxValue) + 1;
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
                CreateNoteHint();
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
                if (noteHint != null) { noteHint.SetActive(false); Destroy(noteHint); }
                RestoreNativeControls();
            }
        }
        private void CreateNoteHint()
        {
            visibleHintNotes = null;
            // A sibling avoids the button's layout, animation and content sizing.
            noteHint = new GameObject("FirstNoteHint", typeof(RectTransform), typeof(LayoutElement));
            noteHint.layer = 5;
            noteHint.transform.SetParent(playRect.parent, false);
            noteHint.GetComponent<LayoutElement>().ignoreLayout = true;
            var hintRect = (RectTransform)noteHint.transform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(.5f, .5f);
            hintRect.pivot = new Vector2(1, .5f);
            hintRect.sizeDelta = new Vector2(22, 11);
            PositionNoteHint();
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.layer = 5;
            labelObject.transform.SetParent(noteHint.transform, false);
            noteHintLabel = labelObject.AddComponent<TextMeshProUGUI>();
            var playText = playRect.GetComponentInChildren<TextMeshProUGUI>();
            noteHintLabel.font = playText.font;
            noteHintLabel.fontSharedMaterial = playText.fontSharedMaterial;
            noteHintLabel.fontSize = 2.5f;
            noteHintLabel.alignment = TextAlignmentOptions.Center;
            noteHintLabel.raycastTarget = false;
            var labelRect = noteHintLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(.5f, 1);
            labelRect.sizeDelta = new Vector2(0, 4);
            labelRect.anchoredPosition = Vector2.zero;
        }
        private void PositionNoteHint()
        {
            // Position the hint's RIGHT edge outside Play's actual LEFT edge.
            noteHint.transform.position = playRect.TransformPoint(new Vector3(playRect.rect.xMin - 5f, playRect.rect.center.y, 0));
        }
        private void LateUpdate()
        {
            if (ready && noteHint != null && noteHint.activeSelf) PositionNoteHint();
        }
        private void UpdateNoteHint()
        {
            var firstNotes = OpeningNotes.FindForHands(notes, Time, n => n.time, n => n.colorType == ColorType.ColorA ? 0 : n.colorType == ColorType.ColorB ? 1 : -1);
            if (visibleHintNotes != null && firstNotes.SequenceEqual(visibleHintNotes)) return;
            visibleHintNotes = firstNotes;
            foreach (var tile in noteTiles) { tile.SetActive(false); Destroy(tile); }
            noteTiles.Clear();
            noteHintLabel.text = "Next L / R";
            int count = 2;
            for (int i = 0; i < count; i++)
            {
                var note = firstNotes[i];
                var tile = new GameObject("NoteHint", typeof(RectTransform), typeof(ImageView));
                tile.layer = 5;
                tile.transform.SetParent(noteHint.transform, false);
                noteTiles.Add(tile);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
                rect.pivot = new Vector2(.5f, 0);
                rect.sizeDelta = new Vector2(6, 6);
                rect.anchoredPosition = new Vector2((i - (count - 1) * .5f) * 7, 0);
                var image = tile.GetComponent<Image>();
                // Match BSML ImageTag: Unity's default UI shader is not suitable
                // for Beat Saber's world-space UI and bloom render path.
                image.material = Utilities.ImageResources.NoGlowMat;
                image.sprite = Utilities.ImageResources.WhitePixel;
                image.color = note == null ? new Color(.2f, .2f, .2f) : i == 0 ? new Color(.7f, .08f, .1f) : new Color(.05f, .35f, .8f);
                image.raycastTarget = false;
                if (note == null)
                {
                    var emptyObject = new GameObject("NoNote", typeof(RectTransform));
                    emptyObject.layer = 5;
                    emptyObject.transform.SetParent(tile.transform, false);
                    var emptyText = emptyObject.AddComponent<TextMeshProUGUI>();
                    emptyText.font = noteHintLabel.font;
                    emptyText.fontSharedMaterial = noteHintLabel.fontSharedMaterial;
                    emptyText.text = "None";
                    emptyText.fontSize = 1.5f;
                    emptyText.alignment = TextAlignmentOptions.Center;
                    emptyText.raycastTarget = false;
                    emptyText.rectTransform.anchorMin = Vector2.zero;
                    emptyText.rectTransform.anchorMax = Vector2.one;
                    emptyText.rectTransform.sizeDelta = Vector2.zero;
                    continue;
                }
                var arrow = new GameObject("CutDirection", typeof(RectTransform), typeof(NoteDirectionGraphic));
                arrow.layer = 5;
                arrow.transform.SetParent(tile.transform, false);
                var arrowRect = (RectTransform)arrow.transform;
                arrowRect.anchorMin = Vector2.zero; arrowRect.anchorMax = Vector2.one;
                arrowRect.sizeDelta = Vector2.zero;
                var graphic = arrow.GetComponent<NoteDirectionGraphic>();
                graphic.material = Utilities.ImageResources.NoGlowMat;
                graphic.Direction = note.cutDirection.Direction();
                graphic.AngleOffset = note.cutDirectionAngleOffset;
                graphic.color = Color.white;
                graphic.raycastTarget = false;
                graphic.SetVerticesDirty();
            }
        }
        private void BindSliders()
        {
            // Listen to user gestures directly; programmatic value assignments do not
            // emit this event. Sync's guard also blocks callbacks from range changes.
            beatSlider.DisableAutoApply(); speedSlider.DisableAutoApply(); njsSlider.DisableAutoApply();
            beatSlider.GetSlider().normalizedValueDidChangeEvent += BeatChanged;
            speedSlider.GetSlider().normalizedValueDidChangeEvent += SpeedChanged;
            njsSlider.GetSlider().normalizedValueDidChangeEvent += NjsChanged;
        }
        private void UnbindSliders()
        {
            if (beatSlider != null) beatSlider.GetSlider().normalizedValueDidChangeEvent -= BeatChanged;
            if (speedSlider != null) speedSlider.GetSlider().normalizedValueDidChangeEvent -= SpeedChanged;
            if (njsSlider != null) njsSlider.GetSlider().normalizedValueDidChangeEvent -= NjsChanged;
        }
        private void BeatChanged(TextSlider slider, float normalized) { Beat = beatSlider.GetSlider().ConvertFromNormalizedValue(normalized); }
        private void SpeedChanged(TextSlider slider, float normalized) { Speed = speedSlider.GetSlider().ConvertFromNormalizedValue(normalized); }
        private void NjsChanged(TextSlider slider, float normalized) { Njs = njsSlider.GetSlider().ConvertFromNormalizedValue(normalized); }
        private static void UseFullWidthSlider(SliderSetting setting)
        {
            // BSML's settings prefab reserves 52 units on the right for the slider.
            // These rows have separate headings, so give the slider the entire row.
            var title = setting.transform.Find("Title");
            if (title != null) title.gameObject.SetActive(false);
            var sliderRect = (RectTransform)setting.GetSlider().transform;
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
        private float MapNjs(float seconds) => njsTimeline.ValueAt(seconds);
        private void Sync()
        {
            if (!ready || syncing) return;
            syncing = true;
            try
            {
            float bpm = (float)timeline.BpmAt(Time);
            speed = PracticeMath.ClampBpm(speed, bpm);
            speedSlider.GetSlider().minValue = Mathf.Ceil(bpm * .1f);
            speedSlider.GetSlider().maxValue = Mathf.Max(speedSlider.GetSlider().minValue + 1, Mathf.Floor(bpm * 5));
            speedSlider.GetSlider().numberOfSteps = Mathf.RoundToInt((speedSlider.GetSlider().maxValue - speedSlider.GetSlider().minValue)) + 1;
            beatSlider.ReceiveValue(); speedSlider.ReceiveValue(); njsSlider.ReceiveValue();
            beatSlider.GetSlider().Refresh(); speedSlider.GetSlider().Refresh(); njsSlider.GetSlider().Refresh();
            float nativeMaxTime = Field<float>(view, "_maxStartSongTime");
            Field<TimeSlider>(view, "_songStartSlider").value = nativeMaxTime > 0 ? Time / nativeMaxTime : 0;
            var nativeSpeed = Field<PercentSlider>(view, "_speedSlider");
            nativeSpeed.minValue = .1f; nativeSpeed.maxValue = 5; nativeSpeed.numberOfSteps = 0;
            nativeSpeed.value = speed / bpm;
            startInfoText.text = StartInfo;
            speedInfoText.text = SpeedInfo;
            UpdateNoteHint();
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
            if (noteHint != null) noteHint.SetActive(false);
            RestoreNativeControls();
        }
        private void OnDestroy()
        {
            cancellation?.Cancel(); cancellation?.Dispose();
            if (noteHint != null) Destroy(noteHint);
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
                PracticeChartLoader.SceneLevelId(____sceneSetupData) == AdvancedPracticeController.SelectedLevel;
        }
        private static Exception Finalizer(Exception __exception) { Apply = false; return __exception; }
    }
    [HarmonyPatch]
    internal static class NjsPatch
    {
        private static MethodBase TargetMethod() => AccessTools.GetDeclaredConstructors(typeof(BeatmapObjectSpawnController.InitData)).Single(c => c.GetParameters().Any(p => p.Name == "noteJumpMovementSpeed"));
        private static void Prefix(ref float noteJumpMovementSpeed)
        {
            if (GameplayPatch.Apply) noteJumpMovementSpeed = AdvancedPracticeController.SelectedNjs;
        }
    }
}
