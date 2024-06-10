using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using Steamworks.Data;
using Steamworks.Ugc;
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
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "Snowlance.Pinata";
        private const string modName = "Pinata";
        private const string modVersion = "0.2.5";

        public static Plugin PluginInstance;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(modGUID);
        public static int PlayerAge;
        public static int PlayerOriginalAge;

        public static List<string> CandyNames;


        public static AssetBundle? ModAssets;

        public static AudioClip? WarningSoundShortsfx;
        public static AudioClip? WarningSoundLongsfx;
        public static AudioClip? BoneCracksfx;
        public static AudioClip? PlayerDeathsfx;
        public static AudioClip? CandyCrunchsfx;
        public static AudioClip? CandleBlowsfx;
        public static AudioClip? CakeAppearsfx;
        public static AudioClip? CakeDisappearsfx;
        public static AudioClip? EatCakesfx;



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
        public static ConfigEntry<bool> configEnablePinata;
        public static ConfigEntry<int> config956Behavior;
        public static ConfigEntry<float> config956Radius;
        public static ConfigEntry<int> configMaxAge;
        public static ConfigEntry<bool> configPlayWarningSound;
        public static ConfigEntry<int> configHeadbuttDamage;

        // SCP0956-1 Configs
        public static ConfigEntry<int> config9561MinValue;
        public static ConfigEntry<int> config9561MaxValue;
        public static ConfigEntry<int> config9561MinSpawn;
        public static ConfigEntry<int> config9561MaxSpawn;
        public static ConfigEntry<int> config9561DeathChance;

        // SCP-559 Configs
        public static ConfigEntry<bool> configEnable559;
        public static ConfigEntry<int> config559Rarity;
        public static ConfigEntry<int> config559MinValue;
        public static ConfigEntry<int> config559MaxValue;
        public static ConfigEntry<int> config559HealAmount;

        // SCP-330 Configs
        public static ConfigEntry<bool> configEnable330;
        public static ConfigEntry<int> config330Rarity;

        // Status Effect Configs
        public static ConfigEntry<bool> configEnableCustomStatusEffects;
        public static ConfigEntry<string> configCandyPurpleEffects;
        public static ConfigEntry<string> configCandyRedEffects;
        public static ConfigEntry<string> configCandyYellowEffects;
        public static ConfigEntry<string> configCandyGreenEffects;
        public static ConfigEntry<string> configCandyBlueEffects;

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
            configExperimentationLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "ExperimentationLevelRarity", 10, "Experimentation Level Rarity");
            configAssuranceLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "AssuranceLevelRarity", 10, "Assurance Level Rarity");
            configVowLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "VowLevelRarity", 10, "Vow Level Rarity");
            configOffenseLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "OffenseLevelRarity", 30, "Offense Level Rarity");
            configMarchLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "MarchLevelRarity", 50, "March Level Rarity");
            configRendLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "RendLevelRarity", 50, "Rend Level Rarity");
            configDineLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "DineLevelRarity", 50, "Dine Level Rarity");
            configTitanLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "TitanLevelRarity", 80, "Titan Level Rarity");
            configModdedLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "ModdedLevelRarity", 30, "Modded Level Rarity");
            configOtherLevelRarity = Config.Bind("Rarity (Doesnt work for behaviors 1-2)", "OtherLevelRarity", 30, "Other Level Rarity");

            // General Configs
            configEnablePinata = Config.Bind("General", "Enable Pinata", true, "Set to false to disable spawning pinata.");
            config956Behavior = Config.Bind("General", "SCP-956 Behavior", 1, "Determines SCP'S behavior when spawned\nBehaviors:\n" +
                "1 - Default: Kills players under the age of 12.\n" +
                "2 - Secret Lab: Candy causes random effects (coming soon) but 956 targets players holding candy and under the age of 12.\n" +
                "3 - Random Age: Everyone has a random age at the start of the game. 956 will target players under 12.\n" +
                "4 - All: 956 targets all players.");
            config956Radius = Config.Bind("General", "ActivationRadius", 15f, "The radius around 956 that will activate 956.");
            configMaxAge = Config.Bind("General", "Max Age", 60, "The maximum age of a player that is decided at the beginning of a game. Useful for random age behavior. Minimum age is 5 on random age behavior, and 18 on all other behaviors");
            configPlayWarningSound = Config.Bind("General", "Play Warning Sound", true, "Play warning sound when inside 956s radius and conditions are met.");
            configHeadbuttDamage = Config.Bind("General", "Headbutt Damage", 50, "The amount of damage SCP-956 will do when using his headbutt attack.");

            // Candy Configs
            config9561MinValue = Config.Bind("Candy", "Candy Min Value", 0, "The minimum scrap value of the candy");
            config9561MaxValue = Config.Bind("Candy", "Cany Max Value", 15, "The maximum scrap value of the candy");
            config9561MinSpawn = Config.Bind("Candy", "Min Candy Spawn", 5, "The minimum amount of candy to spawn when player dies to SCP-956");
            config9561MaxSpawn = Config.Bind("Candy", "Max Candy Spawn", 20, "The maximum amount of candy to spawn when player dies to SCP-956");
            config9561DeathChance = Config.Bind("Candy", "Death Chance", 5, "The chance of the Player being killed by pinata candy");
            
            // SCP-559 Configs
            configEnable559 = Config.Bind("SCP-559", "Enable SCP-559", true, "Set to false to disable spawning SCP-559.");
            config559Rarity = Config.Bind("SCP-559", "Rarity", 25, "How often SCP-559 will spawn.");
            config559MinValue = Config.Bind("SCP-559", "SCP-559 Min Value", 50, "The minimum scrap value of SCP-559.");
            config559MaxValue = Config.Bind("SCP-559", "SCP-559 Max Value", 150, "The maximum scrap value of SCP-559.");
            config559HealAmount = Config.Bind("SCP-559", "Heal Amount", 10, "The amount of health SCP-559 will heal when eaten.");

            // SCP-330 Configs
            configEnable330 = Config.Bind("SCP-330", "Enable SCP-330", true, "Set to false to disable spawning SCP-330.");
            config330Rarity = Config.Bind("SCP-330", "Rarity", 15, "How often SCP-330 will spawn.");

            // Status Effect Configs
            configEnableCustomStatusEffects = Config.Bind("Status Effects (Experimental)", "Enable Custom Status Effects", false, "Enable custom status effects");
            configCandyPurpleEffects = Config.Bind("Status Effects (Experimental)", "Candy Purple Effects", "DamageReduction:15,20,true;HealthRegen:2,10;", "Effects when eating purple candy. See README for more info.");
            configCandyRedEffects = Config.Bind("Status Effects (Experimental)", "Candy Red Effects", "HealthRegen:9,5;", "Effects when eating red candy. See README for more info.");
            configCandyYellowEffects = Config.Bind("Status Effects (Experimental)", "Candy Yellow Effects", "RestoreStamina:25;InfiniteSprint:8;IncreasedMovementSpeed:8,2,true,true;", "Effects when eating yellow candy. See README for more info.");
            configCandyGreenEffects = Config.Bind("Status Effects (Experimental)", "Candy Green Effects", "StatusNegation:30;HealthRegen:1,80;", "Effects when eating green candy. See README for more info.");
            configCandyBlueEffects = Config.Bind("Status Effects (Experimental)", "Candy Blue Effects", "HealPlayer:30,true;", "Effects when eating blue candy. See README for more info.");

            new StatusEffectController();

            // Loading Assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "scp956_assets"));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }
            LoggerInstance.LogDebug($"Got AssetBundle at: {Path.Combine(sAssemblyLocation, "scp956_assets")}");

            // Getting Audio
            WarningSoundShortsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Pinata/Audio/956WarningShort.mp3");
            WarningSoundLongsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Pinata/Audio/956WarningLong.mp3");
            BoneCracksfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Pinata/Audio/bone-crack.mp3");
            PlayerDeathsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Pinata/Audio/Pinata_attack.mp3");
            CandyCrunchsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Candy/Audio/Candy_Crunch.wav");
            CandleBlowsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Cake/Audio/cake_candle_blow.wav");
            CakeAppearsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Cake/Audio/cake_appear.wav");
            CakeDisappearsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Cake/Audio/cake_disappear.wav");
            EatCakesfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Cake/Audio/Cake_Eat.wav");
            if (EatCakesfx == null) { Logger.LogError("EatCakesfx is null"); }
            if (CandleBlowsfx == null) { Logger.LogError("CandleBlowsfx is null"); }
            if (CakeAppearsfx == null) { Logger.LogError("CakeAppearsfx is null"); }
            if (CakeDisappearsfx == null) { Logger.LogError("CakeDisappearsfx is null"); }
            if (BoneCracksfx == null) { Logger.LogError("BoneCracksfx is null"); }
            if (PlayerDeathsfx == null) { Logger.LogError("PlayerDeathsfx is null"); }
            if (CandyCrunchsfx == null) { Logger.LogError("CandyCrunchsfx is null"); }
            if (WarningSoundShortsfx == null) { Logger.LogError("WarningSoundShortsfx is null"); }
            if (WarningSoundLongsfx == null) { Logger.LogError("WarningSoundLongsfx is null"); }
            LoggerInstance.LogDebug($"Got sounds from assets");

            // Getting SCP-559
            if (configEnable559.Value)
            {
                Item SCP559 = ModAssets.LoadAsset<Item>("Assets/ModAssets/Cake/SCP559Item.asset");
                if (SCP559 == null) { LoggerInstance.LogError("Error: Couldnt get SCP559 from assets"); return; }
                LoggerInstance.LogDebug($"Got SCP559 prefab");

                SCP559Behavior SCP559BehaviorScript = SCP559.spawnPrefab.GetComponent<SCP559Behavior>();

                SCP559.minValue = config559MinValue.Value;
                SCP559.maxValue = config559MaxValue.Value;

                NetworkPrefabs.RegisterNetworkPrefab(SCP559.spawnPrefab);
                Utilities.FixMixerGroups(SCP559.spawnPrefab);
                Items.RegisterScrap(SCP559, config559Rarity.Value, Levels.LevelTypes.All);

                // Getting Cake
                Item Cake = ModAssets.LoadAsset<Item>("Assets/ModAssets/Cake/CakeItem.asset");
                if (Cake == null) { LoggerInstance.LogError("Error: Couldnt get cake from assets"); return; }
                LoggerInstance.LogDebug($"Got Cake prefab");

                NetworkPrefabs.RegisterNetworkPrefab(Cake.spawnPrefab);
                Utilities.FixMixerGroups(Cake.spawnPrefab);
                Items.RegisterScrap(Cake);
            }

            // Getting SCP-330
            if (configEnable330.Value)
            {
                Item BowlOfCandy = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/BowlOfCandyItem.asset");
                if (BowlOfCandy == null) { LoggerInstance.LogError("Error: Couldnt get bowl of candy from assets"); return; }
                LoggerInstance.LogDebug($"Got bowl of candy prefab");

                NetworkPrefabs.RegisterNetworkPrefab(BowlOfCandy.spawnPrefab);
                Utilities.FixMixerGroups(BowlOfCandy.spawnPrefab);
                Items.RegisterScrap(BowlOfCandy, config330Rarity.Value, Levels.LevelTypes.All);

                Item BowlOfCandyP = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/BowlOfCandyPItem.asset");
                if (BowlOfCandyP == null) { LoggerInstance.LogError("Error: Couldnt get bowl of candy from assets"); return; }
                LoggerInstance.LogDebug($"Got bowl of candy P prefab");

                NetworkPrefabs.RegisterNetworkPrefab(BowlOfCandyP.spawnPrefab);
                Utilities.FixMixerGroups(BowlOfCandyP.spawnPrefab);
                Items.RegisterScrap(BowlOfCandyP, config330Rarity.Value, Levels.LevelTypes.All);
            }

            // Getting Candy // TODO: Simplify this
            CandyBehavior candyScript;

            Item CandyPink = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyPinkItem.asset");
            if (CandyPink == null) { LoggerInstance.LogError("Error: Couldnt get candy from assets"); return; }
            LoggerInstance.LogDebug($"Got CandyPink prefab");
            Item CandyPurple = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyPurpleItem.asset");
            if (CandyPurple == null) { LoggerInstance.LogError("Error: Couldnt get candy from assets"); return; }
            LoggerInstance.LogDebug($"Got CandyPurple prefab");
            Item CandyRed = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyRedItem.asset");
            if (CandyRed == null) { LoggerInstance.LogError("Error: Couldnt get candy from assets"); return; }
            LoggerInstance.LogDebug($"Got CandyRed prefab");
            Item CandyYellow = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyYellowItem.asset");
            if (CandyYellow == null) { LoggerInstance.LogError("Error: Couldnt get candy from assets"); return; }
            LoggerInstance.LogDebug($"Got CandyYellow prefab");
            Item CandyGreen = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyGreenItem.asset");
            if (CandyGreen == null) { LoggerInstance.LogError("Error: Couldnt get candy from assets"); return; }
            LoggerInstance.LogDebug($"Got CandyGreen prefab");
            Item CandyBlue = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyBlueItem.asset");
            if (CandyBlue == null) { LoggerInstance.LogError("Error: Couldnt get candy from assets"); return; }
            LoggerInstance.LogDebug($"Got CandyBlue prefab");
            Item CandyRainbow = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyRainbowItem.asset");
            if (CandyRainbow == null) { LoggerInstance.LogError("Error: Couldnt get candy from assets"); return; }
            LoggerInstance.LogDebug($"Got CandyRainbow prefab");

            candyScript = CandyPink.spawnPrefab.GetComponent<CandyBehavior>();
            CandyPink.minValue = config9561MinValue.Value;
            CandyPink.maxValue = config9561MaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyPink.spawnPrefab);
            Utilities.FixMixerGroups(CandyPink.spawnPrefab);
            Items.RegisterScrap(CandyPink);

            candyScript = CandyPurple.spawnPrefab.GetComponent<CandyBehavior>();
            CandyPurple.minValue = config9561MinValue.Value;
            CandyPurple.maxValue = config9561MaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyPurple.spawnPrefab);
            Utilities.FixMixerGroups(CandyPurple.spawnPrefab);
            Items.RegisterScrap(CandyPurple);

            candyScript = CandyRed.spawnPrefab.GetComponent<CandyBehavior>();
            CandyRed.minValue = config9561MinValue.Value;
            CandyRed.maxValue = config9561MaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyRed.spawnPrefab);
            Utilities.FixMixerGroups(CandyRed.spawnPrefab);
            Items.RegisterScrap(CandyRed);

            candyScript = CandyYellow.spawnPrefab.GetComponent<CandyBehavior>();
            CandyYellow.minValue = config9561MinValue.Value;
            CandyYellow.maxValue = config9561MaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyYellow.spawnPrefab);
            Utilities.FixMixerGroups(CandyYellow.spawnPrefab);
            Items.RegisterScrap(CandyYellow);

            candyScript = CandyGreen.spawnPrefab.GetComponent<CandyBehavior>();
            CandyGreen.minValue = config9561MinValue.Value;
            CandyGreen.maxValue = config9561MaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyGreen.spawnPrefab);
            Utilities.FixMixerGroups(CandyGreen.spawnPrefab);
            Items.RegisterScrap(CandyGreen);

            candyScript = CandyBlue.spawnPrefab.GetComponent<CandyBehavior>();
            CandyBlue.minValue = config9561MinValue.Value;
            CandyBlue.maxValue = config9561MaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyBlue.spawnPrefab);
            Utilities.FixMixerGroups(CandyBlue.spawnPrefab);
            Items.RegisterScrap(CandyBlue);

            candyScript = CandyRainbow.spawnPrefab.GetComponent<CandyBehavior>();
            CandyRainbow.minValue = config9561MinValue.Value;
            CandyRainbow.maxValue = config9561MaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyRainbow.spawnPrefab);
            Utilities.FixMixerGroups(CandyRainbow.spawnPrefab);
            Items.RegisterScrap(CandyRainbow);

            CandyNames = new List<string> { CandyPink.itemName, CandyPurple.itemName, CandyRed.itemName, CandyYellow.itemName, CandyGreen.itemName, CandyBlue.itemName, CandyRainbow.itemName };

            // Getting enemy
            if (configEnablePinata.Value)
            {
                EnemyType Pinata = ModAssets.LoadAsset<EnemyType>("Assets/ModAssets/Pinata/Pinata.asset");
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
                Enemies.RegisterEnemy(Pinata, SCP956LevelRarities, null, PinataTN, PinataTK);
            }
            
            // Finished
            Logger.LogInfo($"{modGUID} v{modVersion} has loaded!");
        }

        private static void InitializeNetworkBehaviours()
        {
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
            LoggerInstance.LogDebug("Finished initializing network behaviours");
        }
    }
}
