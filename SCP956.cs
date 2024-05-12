using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        public static AssetBundle? ModAssets;

        // Rarity Configs
        public static ConfigEntry<int> configExperimentationLevelRarity;
        public static ConfigEntry<int> configAssuranceLevelRarity;
        public static ConfigEntry<int> configVowLevelRarity;
        public static ConfigEntry<int> configOffenseLevelRarity;
        public static ConfigEntry<int> configMarchLevelRarity;
        public static ConfigEntry<int> configRendLevelRarity;
        public static ConfigEntry<int> configDineLevelRarity;
        public static ConfigEntry<int> configTitanLevelRarity;
        public static ConfigEntry<int> configModdedLevelRarity;
        public static ConfigEntry<int> configOtherLevelRarity;

        

        private void Awake()
        {
            if (PluginInstance == null)
            {
                PluginInstance = this;
            }

            LoggerInstance = PluginInstance.Logger;

            harmony.PatchAll();

            InitializeNetworkBehaviours();

            // Configs
            // Rarity
            configExperimentationLevelRarity = Config.Bind("Rarity", "ExperimentationLevelRarity", 500, "Experimentation Level Rarity");
            configAssuranceLevelRarity = Config.Bind("Rarity", "AssuranceLevelRarity", 40, "Assurance Level Rarity");
            configVowLevelRarity = Config.Bind("Rarity", "VowLevelRarity", 20, "Vow Level Rarity");
            configOffenseLevelRarity = Config.Bind("Rarity", "OffenseLevelRarity", 30, "Offense Level Rarity");
            configMarchLevelRarity = Config.Bind("Rarity", "MarchLevelRarity", 20, "March Level Rarity");
            configRendLevelRarity = Config.Bind("Rarity", "RendLevelRarity", 50, "Rend Level Rarity");
            configDineLevelRarity = Config.Bind("Rarity", "DineLevelRarity", 25, "Dine Level Rarity");
            configTitanLevelRarity = Config.Bind("Rarity", "TitanLevelRarity", 33, "Titan Level Rarity");
            configModdedLevelRarity = Config.Bind("Rarity", "ModdedLevelRarity", 30, "Modded Level Rarity");
            configOtherLevelRarity = Config.Bind("Rarity", "OtherLevelRarity", 30, "Other Level Rarity");

            // TODO: Add NULL checks

            // Loading Assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "mod_assets"));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }
            LoggerInstance.LogDebug($"Got AssetBundle at: {Path.Combine(sAssemblyLocation, "mod_assets")}");

            // Getting enemy
            EnemyType SCP956 = ModAssets.LoadAsset<EnemyType>("Assets/ModAssets/SCP-956/956.asset"); // TODO: Wont let me change rotation or anything in the unity editor
            if (SCP956 == null) { LoggerInstance.LogError("Error: Couldnt get enemy from assets"); return; }
            LoggerInstance.LogDebug($"Got SCP-956 prefab");
            //var ExampleEnemyTN = ModAssets.LoadAsset<TerminalNode>("ExampleEnemyTN");
            //var ExampleEnemyTK = ModAssets.LoadAsset<TerminalKeyword>("ExampleEnemyTK");

            

            LoggerInstance.LogDebug("Settings rarities");
            var SCP956LevelRarities = new Dictionary<Levels.LevelTypes, int> {
                {Levels.LevelTypes.ExperimentationLevel, configExperimentationLevelRarity.Value},
                {Levels.LevelTypes.AssuranceLevel, configAssuranceLevelRarity.Value},
                {Levels.LevelTypes.VowLevel, configVowLevelRarity.Value},
                {Levels.LevelTypes.OffenseLevel, configOffenseLevelRarity.Value},
                {Levels.LevelTypes.MarchLevel, configMarchLevelRarity.Value},
                {Levels.LevelTypes.RendLevel, configRendLevelRarity.Value},
                {Levels.LevelTypes.DineLevel, configDineLevelRarity.Value},
                {Levels.LevelTypes.TitanLevel, configTitanLevelRarity.Value},
                {Levels.LevelTypes.All, configOtherLevelRarity.Value},
                {Levels.LevelTypes.Modded, configModdedLevelRarity.Value},
            };

            LoggerInstance.LogDebug("Registering enemy network prefab...");
            NetworkPrefabs.RegisterNetworkPrefab(SCP956.enemyPrefab);

            LoggerInstance.LogDebug("Registering enemy...");
            Enemies.RegisterEnemy(SCP956, SCP956LevelRarities);

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        private static void InitializeNetworkBehaviours()
        {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}
