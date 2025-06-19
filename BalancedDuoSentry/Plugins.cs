using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using VoidManager;
using VoidManager.MPModChecks;

namespace BalancedDuoSentry
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.USERS_PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Void Crew.exe")]
    [BepInDependency(VoidManager.MyPluginInfo.PLUGIN_GUID)]
    public class BepinPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "N/A")]

        // Brain Confessor Mk1: bc2dcf53afe3a90478b5d9fcffb1f523
        // Brain Benediction Mk1: 6b2c6be56b6678643bd0039305890622
        internal static readonly GUIDUnion SENTRY_FRIGATE_AUTO_BRAIN_WEAPON_GUID = new GUIDUnion("bc2dcf53afe3a90478b5d9fcffb1f523");
        private void Awake()
        {
            Log = Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }


    public class VoidManagerPlugin : VoidPlugin
    {
        public override MultiplayerType MPType => MultiplayerType.Host;

        public override string Author => MyPluginInfo.PLUGIN_AUTHORS;

        public override string Description => MyPluginInfo.PLUGIN_DESCRIPTION;

        public override string ThunderstoreID => MyPluginInfo.PLUGIN_THUNDERSTORE_ID;

        public override SessionChangedReturn OnSessionChange(SessionChangedInput input)
        {
            return new SessionChangedReturn() {SetMod_Session = true};
        }
    }
}