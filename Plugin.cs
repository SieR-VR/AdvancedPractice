using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage.Settings;
using AdvancedPractice.Configuration;
using AdvancedPractice.Compatibility;
using BeatSaberMarkupLanguage.Util;

namespace AdvancedPractice
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPA.Logging.Logger Log { get; private set; }
        private Harmony harmony;
        private BSMLSettings registeredSettings;
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
            MainMenuAwaiter.MainMenuInitializing += RegisterSettings;
        }
        private void RegisterSettings()
        {
            var settings = BsmlCompatibility.Settings;
            if (settings == null || settings == registeredSettings) return;
            settings.AddSettingsMenu("AdvancedPractice", "AdvancedPractice.Views.Settings.bsml", PluginConfig.Instance);
            registeredSettings = settings;
        }
        [OnExit]
        public void Exit()
        {
            harmony?.UnpatchSelf();
            MainMenuAwaiter.MainMenuInitializing -= RegisterSettings;
            if (registeredSettings != null) registeredSettings.RemoveSettingsMenu(PluginConfig.Instance);
        }
    }
}
