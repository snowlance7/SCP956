using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SCP956
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class SCP956 : BaseUnityPlugin
    {
        private const string modGUID = "Snowlance.SCP956";
        private const string modName = "SCP956";
        private const string modVersion = "0.1.0";

        public static SCP956 PluginInstance;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(modGUID);

        public static AssetBundle? DNAssetBundle;

        private void Awake()
        {
            if (PluginInstance == null)
            {
                PluginInstance = this;
            }

            LoggerInstance = PluginInstance.Logger;



            harmony.PatchAll();

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }
    }
}
