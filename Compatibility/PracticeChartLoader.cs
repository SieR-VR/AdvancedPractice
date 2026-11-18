using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedPractice.Compatibility
{
    internal sealed class PracticeChart
    {
        internal BeatmapKey Key;
        internal float Bpm, Duration, DefaultNjs;
        internal IReadonlyBeatmapData Data;
    }
    internal static class PracticeChartLoader
    {
        internal static async Task<PracticeChart> Load(PracticeViewController view, CancellationToken token)
        {
#if BEAT_SABER_1_29_0
            var difficulty = AdvancedPracticeController.Field<IDifficultyBeatmap>(view, "_difficultyBeatmap");
            var player = AdvancedPracticeController.Field<PlayerDataModel>(view, "_playerDataModel");
            var key = new BeatmapKey(difficulty);
            var data = await difficulty.GetBeatmapDataAsync(difficulty.GetEnvironmentInfo(), player.playerData.playerSpecificSettings);
            token.ThrowIfCancellationRequested();
            return new PracticeChart { Key = key, Bpm = difficulty.level.beatsPerMinute, Duration = difficulty.level.songDuration,
                DefaultNjs = DefaultNjs(key.difficulty, difficulty.noteJumpMovementSpeed), Data = data };
#else
            var key = AdvancedPracticeController.Field<BeatmapKey>(view, "_beatmapKey");
            var level = AdvancedPracticeController.Field<BeatmapLevel>(view, "_beatmapLevel");
            var entitlement = AdvancedPracticeController.Field<BeatmapLevelsEntitlementModel>(view, "_beatmapLevelsEntitlementModel");
            var version = await entitlement.GetLevelDataVersionAsync(key.levelId, token);
            var loaded = await AdvancedPracticeController.Field<BeatmapLevelsModel>(view, "_beatmapLevelsModel").LoadBeatmapLevelDataAsync(key.levelId, version, token);
            if (loaded.isError) throw new InvalidOperationException("Could not load beatmap data.");
            var data = await new BeatmapDataLoader().LoadBeatmapDataAsync(loaded.beatmapLevelData, key, level.beatsPerMinute, false,
#if BEAT_SABER_1_37_1
                null,
#else
                null, null,
#endif
                version, null, null, false);
            token.ThrowIfCancellationRequested();
            if (data == null) throw new InvalidOperationException("Beatmap data unavailable.");
            return new PracticeChart { Key = key, Bpm = level.beatsPerMinute, Duration = level.songDuration,
                DefaultNjs = DefaultNjs(key.difficulty, level.GetDifficultyBeatmapData(key.beatmapCharacteristic, key.difficulty).noteJumpMovementSpeed), Data = data };
#endif
        }
        private static float DefaultNjs(BeatmapDifficulty difficulty, float njs) => njs > 0 ? njs : BeatmapDifficultyMethods.DefaultNoteJumpMovementSpeed(difficulty);
        internal static string SceneLevelId(GameplayCoreSceneSetupData scene)
        {
#if BEAT_SABER_1_29_0
            return new BeatmapKey(scene.difficultyBeatmap).levelId;
#else
            return scene.beatmapKey.levelId;
#endif
        }
    }
}
