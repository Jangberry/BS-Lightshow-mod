using HarmonyLib;
using BS_Lightshow_mod.Lighting;

/// <summary>
/// See https://github.com/pardeike/Harmony/wiki for a full reference on Harmony.
/// </summary>
namespace BS_Lightshow_mod.Patch
{
    /// <summary>
    /// This patches LightSwitchEventEffect.SetColor(Color arg1)
    /// </summary>
    [HarmonyPatch(typeof(BeatmapObjectCallbackController), "Start")]
    public class LightingEventsSubscriber
    {
        [HarmonyAfter(new string[] { "com.aeroluna.BeatSaber.CustomJSONData" })]
        private static void Postfix(BeatmapObjectCallbackController __instance, IReadonlyBeatmapData ____beatmapData)
        {
            Plugin.Log?.Debug("Registering callback");
            Translator.Start();
            Plugin.beatmapData = ____beatmapData;
            Plugin.CallbackController = __instance;
            Plugin.callbackData = Plugin.CallbackController.AddBeatmapEventCallback(Translator.HandleBeatmapEventCallback, Plugin.connection.delay);
        }
    }

    [HarmonyPatch(typeof(PlatformLeaderboardViewController), "DidActivate")]
    // The leaderboard is activated right after the map is loaded, and reactivated when the map is reselected even when it's in cache, so for my usage it's the best fit
    // (moreover PlatformLeaderboardViewController also has a beatmapdifficulty interface)
    internal class NewLevelSelected
    {
        [HarmonyAfter(new string[] { "com.aeroluna.BeatSaber.CustomJSONData" })]
        private static void Postfix(IDifficultyBeatmap ____difficultyBeatmap)
        {
            Translator.NewMap(____difficultyBeatmap);
        }
    }
}