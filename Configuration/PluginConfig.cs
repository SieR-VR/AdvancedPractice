using IPA.Config.Stores;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage.Attributes;
[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace AdvancedPractice.Configuration
{
    public class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        [UIValue("njs-step")]
        public virtual float NjsStep { get; set; } = 1f;
    }
}
