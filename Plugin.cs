using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage.Settings;
using AdvancedPractice.Configuration;

namespace AdvancedPractice
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPA.Logging.Logger Log { get; private set; }
        private Harmony harmony;
        [Init]
        public void Init(IPA.Logging.Logger logger, Config config)
        {
            Log = logger;
            PluginConfig.Instance = config.Generated<PluginConfig>();
        }
        [OnStart]
        public void Start()
        {
            if (PluginManager.EnabledPlugins.Any(p => p.Id == "PracticePlugin"))
            {
                Log.Error("Remove PracticePlugin before enabling AdvancedPractice. No patches applied.");
                return;
            }
            harmony = new Harmony("AdvancedPractice");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            BSMLSettings.Instance.AddSettingsMenu("AdvancedPractice", "AdvancedPractice.Views.Settings.bsml", PluginConfig.Instance);
        }
        [OnExit]
        public void Exit()
        {
            harmony?.UnpatchSelf();
            if (harmony != null) BSMLSettings.Instance.RemoveSettingsMenu(PluginConfig.Instance);
        }
    }
}
