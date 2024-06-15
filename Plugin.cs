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

namespace SCP956 // TODO: Make sure wireframe video is working
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "Snowlance.Pinata";
        private const string modName = "Pinata";
        private const string modVersion = "0.3.0";

        public static Plugin PluginInstance;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(modGUID);
        public static int PlayerAge = 0;
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

        // Secret Lab Configs
        public static ConfigEntry<bool> configSecretLab;
        public static ConfigEntry<int> config956SpawnRadius;
        public static ConfigEntry<int> config956TeleportTime;
        public static ConfigEntry<int> config956TeleportRange;

        // SCP-956 Rarity Configs
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

        // SCP-956 Configs
        public static ConfigEntry<bool> configEnablePinata;
        public static ConfigEntry<bool> configTargetAllPlayers;
        public static ConfigEntry<float> config956ActivationRadius;
        public static ConfigEntry<int> configMinAge;
        public static ConfigEntry<int> configMaxAge;
        public static ConfigEntry<bool> configPlayWarningSound;
        public static ConfigEntry<int> configHeadbuttDamage;

        // SCP0956-1 Configs
        public static ConfigEntry<int> configCandyMinValue;
        public static ConfigEntry<int> configCandyMaxValue;
        public static ConfigEntry<int> configCandyMinSpawn;
        public static ConfigEntry<int> configCandyMaxSpawn;
        public static ConfigEntry<int> configCandyDeathChance;

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

            // Secret Lab
            configSecretLab = Config.Bind("Secret Lab", "Secret Lab", false, "Enables Secret Lab mode. SCP-956 will have a lot of the same functionality from SCP Secret Lab. Acts like a Hard mode. See README for more info."); // TODO: Make behavior just like SCP Secret Lab
            config956SpawnRadius = Config.Bind("Secret Lab", "956 Spawn Radius", 30, "Radius at which SCP-956 will spawn around the player when their age is below 12 or candy is collected.");
            config956TeleportTime = Config.Bind("Secret Lab", "956 Teleport Time", 60, "Time in seconds it takes for SCP-956 to teleport somewhere else when nobody is looking at it.");
            config956TeleportRange = Config.Bind("Secret Lab", "956 Teleport Range", 50, "Range at which SCP-956 will teleport.");

            // Rarity
            configExperimentationLevelRarity = Config.Bind("Rarity", "ExperimentationLevelRarity", 10, "Experimentation Level Rarity");
            configAssuranceLevelRarity = Config.Bind("Rarity", "AssuranceLevelRarity", 10, "Assurance Level Rarity");
            configVowLevelRarity = Config.Bind("Rarity", "VowLevelRarity", 10, "Vow Level Rarity");
            configOffenseLevelRarity = Config.Bind("Rarity", "OffenseLevelRarity", 30, "Offense Level Rarity");
            configMarchLevelRarity = Config.Bind("Rarity", "MarchLevelRarity", 50, "March Level Rarity");
            configRendLevelRarity = Config.Bind("Rarity", "RendLevelRarity", 50, "Rend Level Rarity");
            configDineLevelRarity = Config.Bind("Rarity", "DineLevelRarity", 50, "Dine Level Rarity");
            configTitanLevelRarity = Config.Bind("Rarity", "TitanLevelRarity", 80, "Titan Level Rarity");
            configModdedLevelRarity = Config.Bind("Rarity", "ModdedLevelRarity", 30, "Modded Level Rarity");
            configOtherLevelRarity = Config.Bind("Rarity", "OtherLevelRarity", 30, "Other Level Rarity");

            // SCP-956 Configs
            configEnablePinata = Config.Bind("SCP-956", "Enable SCP-956", true, "Set to false to disable spawning SCP-956.");
            configTargetAllPlayers = Config.Bind("SCP-956", "Target All Players", false, "Set to true if you want 956 to target all players regardless of conditions.");
            config956ActivationRadius = Config.Bind("SCP-956", "Activation Radius", 15f, "The radius in which SCP-956 will target players that meet the required conditions.");
            configMinAge = Config.Bind("SCP-956", "Min Age", 18, "The minimum age of a player that is decided at the beginning of a game.");
            configMaxAge = Config.Bind("SCP-956", "Max Age", 70, "The maximum age of a player that is decided at the beginning of a game.");
            configPlayWarningSound = Config.Bind("SCP-956", "Play Warning Sound", true, "Play warning sound when inside SCP-956's radius and conditions are met.");
            configHeadbuttDamage = Config.Bind("SCP-956", "Headbutt Damage", 50, "The amount of damage SCP-956 will do when using his headbutt attack.");

            // Candy Configs
            configCandyMinValue = Config.Bind("Candy", "Candy Min Value", 0, "The minimum scrap value of the candy");
            configCandyMaxValue = Config.Bind("Candy", "Cany Max Value", 15, "The maximum scrap value of the candy");
            configCandyMinSpawn = Config.Bind("Candy", "Min Candy Spawn", 5, "The minimum amount of candy to spawn when player dies to SCP-956");
            configCandyMaxSpawn = Config.Bind("Candy", "Max Candy Spawn", 30, "The maximum amount of candy to spawn when player dies to SCP-956");
            configCandyDeathChance = Config.Bind("Candy", "Death Chance", 5, "The chance of the Player being killed by pinata candy");
            
            // SCP-559 Configs
            configEnable559 = Config.Bind("SCP-559", "Enable SCP-559", true, "Set to false to disable spawning SCP-559.");
            config559Rarity = Config.Bind("SCP-559", "Rarity", 40, "How often SCP-559 will spawn.");
            config559MinValue = Config.Bind("SCP-559", "SCP-559 Min Value", 50, "The minimum scrap value of SCP-559.");
            config559MaxValue = Config.Bind("SCP-559", "SCP-559 Max Value", 150, "The maximum scrap value of SCP-559.");
            config559HealAmount = Config.Bind("SCP-559", "Heal Amount", 10, "The amount of health SCP-559 will heal when eaten.");

            // SCP-330 Configs
            configEnable330 = Config.Bind("SCP-330", "Enable SCP-330", true, "Set to false to disable spawning SCP-330.");
            config330Rarity = Config.Bind("SCP-330", "Rarity", 30, "How often SCP-330 will spawn.");

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
            CandyPink.minValue = configCandyMinValue.Value;
            CandyPink.maxValue = configCandyMaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyPink.spawnPrefab);
            Utilities.FixMixerGroups(CandyPink.spawnPrefab);
            Items.RegisterScrap(CandyPink);

            candyScript = CandyPurple.spawnPrefab.GetComponent<CandyBehavior>();
            CandyPurple.minValue = configCandyMinValue.Value;
            CandyPurple.maxValue = configCandyMaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyPurple.spawnPrefab);
            Utilities.FixMixerGroups(CandyPurple.spawnPrefab);
            Items.RegisterScrap(CandyPurple);

            candyScript = CandyRed.spawnPrefab.GetComponent<CandyBehavior>();
            CandyRed.minValue = configCandyMinValue.Value;
            CandyRed.maxValue = configCandyMaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyRed.spawnPrefab);
            Utilities.FixMixerGroups(CandyRed.spawnPrefab);
            Items.RegisterScrap(CandyRed);

            candyScript = CandyYellow.spawnPrefab.GetComponent<CandyBehavior>();
            CandyYellow.minValue = configCandyMinValue.Value;
            CandyYellow.maxValue = configCandyMaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyYellow.spawnPrefab);
            Utilities.FixMixerGroups(CandyYellow.spawnPrefab);
            Items.RegisterScrap(CandyYellow);

            candyScript = CandyGreen.spawnPrefab.GetComponent<CandyBehavior>();
            CandyGreen.minValue = configCandyMinValue.Value;
            CandyGreen.maxValue = configCandyMaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyGreen.spawnPrefab);
            Utilities.FixMixerGroups(CandyGreen.spawnPrefab);
            Items.RegisterScrap(CandyGreen);

            candyScript = CandyBlue.spawnPrefab.GetComponent<CandyBehavior>();
            CandyBlue.minValue = configCandyMinValue.Value;
            CandyBlue.maxValue = configCandyMaxValue.Value;
            NetworkPrefabs.RegisterNetworkPrefab(CandyBlue.spawnPrefab);
            Utilities.FixMixerGroups(CandyBlue.spawnPrefab);
            Items.RegisterScrap(CandyBlue);

            candyScript = CandyRainbow.spawnPrefab.GetComponent<CandyBehavior>();
            CandyRainbow.minValue = configCandyMinValue.Value;
            CandyRainbow.maxValue = configCandyMaxValue.Value;
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
                TerminalKeyword PinataTK = ModAssets.LoadAsset<TerminalKeyword>("Assets/ModAssets/Pinata/Bestiary/PinataTK.asset");

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
                
                LoggerInstance.LogDebug("Registering enemy network prefab...");
                NetworkPrefabs.RegisterNetworkPrefab(Pinata.enemyPrefab);
                LoggerInstance.LogDebug("Registering enemy...");
                Enemies.RegisterEnemy(Pinata, SCP956LevelRarities, null, PinataTN, PinataTK);
            }
            
            // Finished
            Logger.LogInfo($"{modGUID} v{modVersion} has loaded!");
        }

        public static bool IsPlayerHoldingCandy(PlayerControllerB player)
        {
            foreach (GrabbableObject item in player.ItemSlots)
            {
                if (item == null) { continue; }
                if (CandyNames.Contains(item.itemProperties.itemName))
                {
                    return true;
                }
            }
            return false;
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
