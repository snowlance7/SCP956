using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using Steamworks.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Rendering;

namespace SCP956
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    [BepInDependency(LethalNetworkAPI.MyPluginInfo.PLUGIN_GUID)]
    public class SCP956 : BaseUnityPlugin
    {
        private const string modGUID = "Snowlance.Pinata";
        private const string modName = "Pinata";
        private const string modVersion = "0.1.0";

        public static SCP956 PluginInstance;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(modGUID);
        public static int PlayerAge;
        public System.Random random;


        public static AssetBundle? ModAssets;

        public static AudioClip? WarningSoundsfx;
        public static AudioClip? BoneCracksfx;
        public static AudioClip? PlayerDeathsfx;
        public static AudioClip? CandyCrunchsfx;
        public static AudioClip? CandleBlowsfx;



        

        // SCP-956 Configs
        // Rarity Configs || ONLY WORKS WITH BIRTHDAYMODE AND RANDOM AGE GAMEMODES
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

        // General Configs
        public static ConfigEntry<int> config956Behavior;
        public static ConfigEntry<float> config956Radius;
        public static ConfigEntry<int> configMaxAge;
        public static ConfigEntry<bool> configPlayWarningSound;
        public static ConfigEntry<float> configActivationTime;
        public static ConfigEntry<float> configActivationTimeCandy;

        // SCP0956-1 Configs
        public static ConfigEntry<int> config9561MinValue;
        public static ConfigEntry<int> config9561MaxValue;

        // SCP-559 Configs
        public static ConfigEntry<int> config559Rarity;
        public static ConfigEntry<int> config559MinValue;
        public static ConfigEntry<int> config559MaxValue;

        // SCP-330 Configs
        //public static ConfigEntry<int> config330Rarity;

        private void Awake()
        {
            if (PluginInstance == null)
            {
                PluginInstance = this;
            }

            LoggerInstance = PluginInstance.Logger;

            harmony.PatchAll();

            NetworkHandler.Init();
            InitializeNetworkBehaviours();

            // Configs
            // Rarity
            configExperimentationLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "ExperimentationLevelRarity", 30, "Experimentation Level Rarity"); // TEMP
            configAssuranceLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "AssuranceLevelRarity", 40, "Assurance Level Rarity");
            configVowLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "VowLevelRarity", 20, "Vow Level Rarity");
            configOffenseLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "OffenseLevelRarity", 30, "Offense Level Rarity");
            configMarchLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "MarchLevelRarity", 20, "March Level Rarity");
            configRendLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "RendLevelRarity", 50, "Rend Level Rarity");
            configDineLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "DineLevelRarity", 25, "Dine Level Rarity");
            configTitanLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "TitanLevelRarity", 33, "Titan Level Rarity");
            configModdedLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "ModdedLevelRarity", 30, "Modded Level Rarity");
            configOtherLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 0-1)", "OtherLevelRarity", 30, "Other Level Rarity");

            // General Configs

            config956Behavior = Config.Bind("General", "SCP-956 Behavior", 4, "Determines SCP'S behavior when spawned\nBehaviors:\n" + // TEMP
                "1 - Default: Kills players under the age of 12.\n" +
                "2 - Secret Lab: Candy causes random effects but 956 targets players holding candy and under the age of 12. Candy spawns naturally.\n" +
                "3 - Random Age: Everyone has a random age at the start of the game. 956 will target players under 12.\n" +
                "4 - All: 956 targets all players.");
            config956Radius = Config.Bind("General", "ActivationRadius", 15f, "The radius around 956 that will activate 956."); // TEMP
            configMaxAge = Config.Bind("General", "Max Age", 50, "The maximum age of a player that is decided at the beginning of a game. Useful for random age behavior. Minimum age is 5 on random age behavior, and 18 on all other behaviors");
            configPlayWarningSound = Config.Bind("General", "Play Warning Sound", true, "Play warning sound when inside 956s radius and conditions are met.");
            configActivationTime = Config.Bind("General", "Activation Time", 6f, "How long it takes for 956 to activate.");
            configActivationTimeCandy = Config.Bind("General", "Activation Time Candy", 20f, "How long it takes for 956 to activate when holding candy. Only used when behavior is 2.");

            // SCP-956-1 Configs
            config9561MinValue = Config.Bind("SCP-956-1", "SCP-956-1 Min Value", 0, "The minimum scrap value of the candy");
            config9561MaxValue = Config.Bind("SCP-956-1", "SCP-956-1 Max Value", 15, "The maximum scrap value of the candy");

            // SCP-559 Configs

            config559Rarity = Config.Bind("SCP-559", "Rarity", 40, "How often SCP-559 will spawn."); // TEMP
            config559MinValue = Config.Bind("SCP-559", "SCP-559 Min Value", 100, "The minimum scrap value of SCP-559."); // TODO: Make sure all configs are added and work
            config559MaxValue = Config.Bind("SCP-559", "SCP-559 Max Value", 150, "The maximum scrap value of SCP-559.");

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

            // Getting Audio

            WarningSoundsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Pinata/Audio/956WarningShort.wav");
            if (WarningSoundsfx == null) { LoggerInstance.LogError("Error: Couldnt get audio from assets"); return; }
            BoneCracksfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Pinata/Audio/bone-crack.mp3");
            if (BoneCracksfx == null) { LoggerInstance.LogError("Error: Couldnt get audio from assets"); return; }
            PlayerDeathsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Pinata/Audio/Pinata_attack.mp3");
            if (PlayerDeathsfx == null) { LoggerInstance.LogError("Error: Couldnt get audio from assets"); return; }
            CandyCrunchsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Candy/Audio/Candy_Crunch.wav");
            if (CandyCrunchsfx == null) { LoggerInstance.LogError("Error: Couldnt get audio from assets"); return; }
            CandleBlowsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Cake/Audio/cake_candle_blow.wav");
            if (CandleBlowsfx == null) { LoggerInstance.LogError("Error: Couldnt get audio from assets"); return; }

            // Getting Cake
            Item Cake = ModAssets.LoadAsset<Item>("Assets/ModAssets/Cake/CakeItem.asset");
            if (Cake == null) { LoggerInstance.LogError("Error: Couldnt get cake from assets"); return; } // TODO: Not getting asset fix
            LoggerInstance.LogDebug($"Got Cake prefab");

            SCP559Behavior SCP559BehaviorScript = Cake.spawnPrefab.AddComponent<SCP559Behavior>();

            SCP559BehaviorScript.grabbable = true;
            SCP559BehaviorScript.itemProperties = Cake;
            Cake.minValue = config559MinValue.Value;
            Cake.maxValue = config559MaxValue.Value;

            NetworkPrefabs.RegisterNetworkPrefab(Cake.spawnPrefab);
            Utilities.FixMixerGroups(Cake.spawnPrefab);
            Items.RegisterScrap(Cake, config559Rarity.Value, Levels.LevelTypes.All);

            // Getting Candy // TODO: Simplify this
            CandyBehavior candyScript;

            Item CandyPink = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyPinkItem.asset");
            Item CandyPurple = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyPurpleItem.asset");
            Item CandyRed = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyRedItem.asset");
            Item CandyYellow = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyYellowItem.asset");

            candyScript = CandyPink.spawnPrefab.AddComponent<CandyBehavior>();
            candyScript.grabbable = true;
            candyScript.itemProperties = CandyPink;
            CandyPink.minValue = config9561MinValue.Value;
            CandyPink.maxValue = config9561MaxValue.Value;
            if (config956Behavior.Value == 2) { CandyPink.itemSpawnsOnGround = true; }
            NetworkPrefabs.RegisterNetworkPrefab(CandyPink.spawnPrefab);
            Utilities.FixMixerGroups(CandyPink.spawnPrefab);
            Items.RegisterItem(CandyPink);

            candyScript = CandyPurple.spawnPrefab.AddComponent<CandyBehavior>();
            candyScript.grabbable = true;
            candyScript.itemProperties = CandyPurple;
            CandyPurple.minValue = config9561MinValue.Value;
            CandyPurple.maxValue = config9561MaxValue.Value;
            if (config956Behavior.Value == 2) { CandyPurple.itemSpawnsOnGround = true; }
            NetworkPrefabs.RegisterNetworkPrefab(CandyPurple.spawnPrefab);
            Utilities.FixMixerGroups(CandyPurple.spawnPrefab);
            Items.RegisterItem(CandyPurple);

            candyScript = CandyRed.spawnPrefab.AddComponent<CandyBehavior>();
            candyScript.grabbable = true;
            candyScript.itemProperties = CandyRed;
            CandyRed.minValue = config9561MinValue.Value;
            CandyRed.maxValue = config9561MaxValue.Value;
            if (config956Behavior.Value == 2) { CandyRed.itemSpawnsOnGround = true; }
            NetworkPrefabs.RegisterNetworkPrefab(CandyRed.spawnPrefab);
            Utilities.FixMixerGroups(CandyRed.spawnPrefab);
            Items.RegisterItem(CandyRed);

            candyScript = CandyYellow.spawnPrefab.AddComponent<CandyBehavior>();
            candyScript.grabbable = true;
            candyScript.itemProperties = CandyYellow;
            CandyYellow.minValue = config9561MinValue.Value;
            CandyYellow.maxValue = config9561MaxValue.Value;
            if (config956Behavior.Value == 2) { CandyYellow.itemSpawnsOnGround = true; }
            NetworkPrefabs.RegisterNetworkPrefab(CandyYellow.spawnPrefab);
            Utilities.FixMixerGroups(CandyYellow.spawnPrefab);
            Items.RegisterItem(CandyYellow);

            // Getting enemy
            EnemyType Pinata = ModAssets.LoadAsset<EnemyType>("Assets/ModAssets/Pinata/Pinata.asset"); // TODO: Wont let me change rotation or anything in the unity editor
            if (Pinata == null) { LoggerInstance.LogError("Error: Couldnt get enemy from assets"); return; }
            LoggerInstance.LogDebug($"Got SCP-956 prefab");
            TerminalNode PinataTN = ModAssets.LoadAsset<TerminalNode>("Assets/ModAssets/Pinata/Bestiary/PinataTN.asset");
            TerminalKeyword PinataTK = ModAssets.LoadAsset<TerminalKeyword>("Assets/ModAssets/Pinata/Bestiary/PinataTK.asset"); // TODO: Make wireframe video

            LoggerInstance.LogDebug("Setting rarities");
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

            if (config956Behavior.Value > 2) { Pinata.spawningDisabled = false; }
            LoggerInstance.LogDebug("Registering enemy network prefab...");
            NetworkPrefabs.RegisterNetworkPrefab(Pinata.enemyPrefab);
            LoggerInstance.LogDebug("Registering enemy...");
            Enemies.RegisterEnemy(Pinata, SCP956LevelRarities/*, null, PinataTN, PinataTK*/);
            
            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        public static List<SpawnableEnemyWithRarity> GetEnemies()
        {
            LoggerInstance.LogDebug("Getting enemies");
            List<SpawnableEnemyWithRarity> enemies = new List<SpawnableEnemyWithRarity>();
            enemies = GameObject.Find("Terminal")
                .GetComponentInChildren<Terminal>()
                .moonsCatalogueList
                .SelectMany(x => x.Enemies.Concat(x.DaytimeEnemies).Concat(x.OutsideEnemies))
                .Where(x => x != null && x.enemyType != null && x.enemyType.name != null)
                .GroupBy(x => x.enemyType.name, (k, v) => v.First())
                .ToList();

            LoggerInstance.LogDebug($"Enemy types: {enemies.Count}");
            return enemies;
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
