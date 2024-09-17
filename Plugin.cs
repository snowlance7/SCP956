using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using SCP956.Items;
using SCP956.Items.Cake;
using SCP956.Patches;
using Steamworks.Data;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Rendering;

namespace SCP956
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin PluginInstance;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        //public static int PlayerAge = 0;
        //public static int PlayerOriginalAge;
        //public static bool IsYoung { get { if (PlayerAge < 12) { return true; } else { return false; } } }
        public static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public static List<string> CandyNames;

        public static AssetBundle? ModAssets;

        public static AudioClip? WarningSoundShortsfx;
        public static AudioClip? WarningSoundLongsfx;
        public static AudioClip? CakeDisappearsfx;

        // SCP-956 Configs
        public static ConfigEntry<bool> configEnablePinata;
        private ConfigEntry<string> config956LevelRarities;
        private ConfigEntry<string> config956CustomLevelRarities;
        public static ConfigEntry<bool> configTargetAllPlayers;
        public static ConfigEntry<float> config956ActivationRadius;
        public static ConfigEntry<int> configMinAge;
        public static ConfigEntry<int> configMaxAge;
        public static ConfigEntry<bool> configPlayWarningSound;
        public static ConfigEntry<int> configHeadbuttDamage;
        public static ConfigEntry<float> configMaxTimeToKillPlayer;
        public static ConfigEntry<int> config956TeleportTime;
        public static ConfigEntry<int> config956TeleportRange;

        // SCP0956-1 Configs
        public static ConfigEntry<int> configCandyMinSpawn;
        public static ConfigEntry<int> configCandyMaxSpawn;
        public static ConfigEntry<bool> configEnableCandyBag;

        // SCP-559 Configs
        public static ConfigEntry<bool> configEnable559;
        private ConfigEntry<string> config559LevelRarities;
        private ConfigEntry<string> config559CustomLevelRarities;
        public static ConfigEntry<int> config559MinValue;
        public static ConfigEntry<int> config559MaxValue;
        public static ConfigEntry<int> config559HealAmount;
        public static ConfigEntry<bool> config559CakeReversesAge;
        public static ConfigEntry<bool> config559ReversesAgeReblow;

        // SCP-330 Configs
        public static ConfigEntry<bool> configEnable330;
        private ConfigEntry<string> config330LevelRarities;
        private ConfigEntry<string> config330CustomLevelRarities;

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

            // SCP-956 Configs
            configEnablePinata = Config.Bind("SCP-956", "Enable SCP-956", true, "Set to false to disable spawning SCP-956.");
            config956LevelRarities = Config.Bind("SCP-956 Rarities", "Level Rarities", "ExperimentationLevel:10, AssuranceLevel:10, VowLevel:10, OffenseLevel:30, AdamanceLevel:50, MarchLevel:50, RendLevel:50, DineLevel:50, TitanLevel:80, ArtificeLevel:80, EmbrionLevel:100, All:30, Modded:30", "Rarities for each level. See default for formatting.");
            config956CustomLevelRarities = Config.Bind("SCP-956 Rarities", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities.");
            configTargetAllPlayers = Config.Bind("SCP-956", "Target All Players", false, "Set to true if you want 956 to target all players regardless of conditions.");
            config956ActivationRadius = Config.Bind("SCP-956", "Activation Radius", 15f, "The radius in which SCP-956 will target players that meet the required conditions.");
            configMinAge = Config.Bind("SCP-956", "Min Age", 18, "The minimum age of a player that is decided at the beginning of a game.");
            configMaxAge = Config.Bind("SCP-956", "Max Age", 70, "The maximum age of a player that is decided at the beginning of a game.");
            configPlayWarningSound = Config.Bind("SCP-956", "Play Warning Sound", true, "Play warning sound when inside SCP-956's radius and conditions are met.");
            configHeadbuttDamage = Config.Bind("SCP-956", "Headbutt Damage", 50, "The amount of damage SCP-956 will do when using his headbutt attack.");
            configMaxTimeToKillPlayer = Config.Bind("SCP-956", "Max Time To Kill Player", 60f, "If SCP-956 doesnt kill a player in this amount of time, the player will die. (in lore people exposed to SCP-956 and moved away die from candy growing in their guts)");

            config956TeleportTime = Config.Bind("SCP-956", "956 Teleport Time", 60, "Time in seconds it takes for SCP-956 to teleport somewhere else when nobody is looking at it.");
            config956TeleportRange = Config.Bind("SCP-956", "956 Teleport Range", 500, "Max range around SCP-956 in which he will teleport.");

            // Candy Configs
            configCandyMinSpawn = Config.Bind("Candy", "Min Candy Spawn", 5, "The minimum amount of candy to spawn when player dies to SCP-956");
            configCandyMaxSpawn = Config.Bind("Candy", "Max Candy Spawn", 30, "The maximum amount of candy to spawn when player dies to SCP-956");
            configEnableCandyBag = Config.Bind("Candy", "Enable Candy Bag", true, "Makes it so you can place candy into a separate bag.");
            
            // SCP-559 Configs
            configEnable559 = Config.Bind("SCP-559", "Enable SCP-559", true, "Set to false to disable spawning SCP-559.");
            config559LevelRarities = Config.Bind("SCP-559 Rarities", "Level Rarities", "ExperimentationLevel:25, AssuranceLevel:30, VowLevel:30, OffenseLevel:40, AdamanceLevel:45, MarchLevel:40, RendLevel:100, DineLevel:100, TitanLevel:50, ArtificeLevel:60, EmbrionLevel:25, All:40, Modded:40", "Rarities for each level. See default for formatting.");
            config559CustomLevelRarities = Config.Bind("SCP-559 Rarities", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities."); // TODO: Figure out scp level names
            config559MinValue = Config.Bind("SCP-559", "SCP-559 Min Value", 50, "The minimum scrap value of SCP-559.");
            config559MaxValue = Config.Bind("SCP-559", "SCP-559 Max Value", 150, "The maximum scrap value of SCP-559.");
            config559HealAmount = Config.Bind("SCP-559", "Heal Amount", 10, "The amount of health SCP-559 will heal when eaten.");
            config559CakeReversesAge = Config.Bind("SCP-559", "Cake Reverses Age", false, "When the cake is eaten, it will reverse the players age back to their original age.");
            config559ReversesAgeReblow = Config.Bind("SCP-559", "Blowing out Reverses Age", false, "If you find another 559 cake and blow out the candles, it will reverse your age back to your original age.");

            // SCP-330 Configs
            configEnable330 = Config.Bind("SCP-330", "Enable SCP-330", true, "Set to false to disable spawning SCP-330.");
            config330LevelRarities = Config.Bind("SCP-330 Rarities", "Level Rarities", "ExperimentationLevel:30, AssuranceLevel:30, VowLevel:30, OffenseLevel:40, AdamanceLevel:45, MarchLevel:40, RendLevel:100, DineLevel:100, TitanLevel:50, ArtificeLevel:80, EmbrionLevel:45, All:30, Modded:30", "Rarities for each level. See default for formatting.");
            config330CustomLevelRarities = Config.Bind("SCP-330 Rarities", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities."); // TODO: Figure out scp level names

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
            CakeDisappearsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Cake/Audio/cake_disappear.wav");
            if (WarningSoundShortsfx == null) { Logger.LogError("WarningSoundShortsfx is null"); }
            if (WarningSoundLongsfx == null) { Logger.LogError("WarningSoundLongsfx is null"); }
            if (CakeDisappearsfx == null) { Logger.LogError("CakeDisappearsfx is null"); }
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

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(SCP559.spawnPrefab);
                Utilities.FixMixerGroups(SCP559.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(SCP559, GetLevelRarities(config559LevelRarities.Value), GetCustomLevelRarities(config559CustomLevelRarities.Value));

                // Getting Cake
                Item Cake = ModAssets.LoadAsset<Item>("Assets/ModAssets/Cake/CakeItem.asset");
                if (Cake == null) { LoggerInstance.LogError("Error: Couldnt get cake from assets"); return; }
                LoggerInstance.LogDebug($"Got Cake prefab");

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(Cake.spawnPrefab);
                Utilities.FixMixerGroups(Cake.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(Cake);
            }

            // Getting SCP-330
            if (configEnable330.Value)
            {
                Item BowlOfCandy = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/BowlOfCandyItem.asset");
                if (BowlOfCandy == null) { LoggerInstance.LogError("Error: Couldnt get bowl of candy from assets"); return; }
                LoggerInstance.LogDebug($"Got bowl of candy prefab");

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(BowlOfCandy.spawnPrefab);
                Utilities.FixMixerGroups(BowlOfCandy.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(BowlOfCandy, GetLevelRarities(config330LevelRarities.Value), GetCustomLevelRarities(config330CustomLevelRarities.Value));

                Item BowlOfCandyP = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/BowlOfCandyPItem.asset");
                if (BowlOfCandyP == null) { LoggerInstance.LogError("Error: Couldnt get bowl of candy from assets"); return; }
                LoggerInstance.LogDebug($"Got bowl of candy P prefab");

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(BowlOfCandyP.spawnPrefab);
                Utilities.FixMixerGroups(BowlOfCandyP.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(BowlOfCandyP, GetLevelRarities(config330LevelRarities.Value), GetCustomLevelRarities(config330CustomLevelRarities.Value));
            }

            // Getting Candy

            Item CandyPink = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyPinkItem.asset");
            if (CandyPink == null) { LoggerInstance.LogError("Error: Couldnt get CandyPink from assets"); return; }
            RegisterCandy(CandyPink);
            LoggerInstance.LogDebug($"Got CandyPink");
            Item CandyPurple = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyPurpleItem.asset");
            if (CandyPurple == null) { LoggerInstance.LogError("Error: Couldnt get CandyPurple from assets"); return; }
            RegisterCandy(CandyPurple);
            LoggerInstance.LogDebug($"Got CandyPurple");
            Item CandyRed = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyRedItem.asset");
            if (CandyRed == null) { LoggerInstance.LogError("Error: Couldnt get CandyRed from assets"); return; }
            RegisterCandy(CandyRed);
            LoggerInstance.LogDebug($"Got CandyRed");
            Item CandyYellow = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyYellowItem.asset");
            if (CandyYellow == null) { LoggerInstance.LogError("Error: Couldnt get CandyYellow from assets"); return; }
            RegisterCandy(CandyYellow);
            LoggerInstance.LogDebug($"Got CandyYellow");
            Item CandyGreen = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyGreenItem.asset");
            if (CandyGreen == null) { LoggerInstance.LogError("Error: Couldnt get CandyGreen from assets"); return; }
            RegisterCandy(CandyGreen);
            LoggerInstance.LogDebug($"Got CandyGreen");
            Item CandyBlue = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyBlueItem.asset");
            if (CandyBlue == null) { LoggerInstance.LogError("Error: Couldnt get CandyBlue from assets"); return; }
            RegisterCandy(CandyBlue);
            LoggerInstance.LogDebug($"Got CandyBlue");
            Item CandyRainbow = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyRainbowItem.asset");
            if (CandyRainbow == null) { LoggerInstance.LogError("Error: Couldnt get CandyRainbow from assets"); return; }
            RegisterCandy(CandyRainbow);
            LoggerInstance.LogDebug($"Got CandyRainbow");
            Item CandyBlack = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/CandyBlackItem.asset");
            if (CandyBlack == null) { LoggerInstance.LogError("Error: Couldnt get CandyBlack from assets"); return; }
            RegisterCandy(CandyBlack);
            LoggerInstance.LogDebug($"Got CandyBlack");

            CandyNames = new List<string> { CandyPink.itemName, CandyPurple.itemName, CandyRed.itemName, CandyYellow.itemName, CandyGreen.itemName, CandyBlue.itemName, CandyRainbow.itemName, CandyBlack.itemName };

            // Getting Candy Bag
            if (configEnableCandyBag.Value)
            {
                Item CandyBag = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/BagOfCandyItem.asset");
                if (CandyBag == null) { LoggerInstance.LogError("Error: Couldnt get CandiesInBag from assets"); return; }
                LoggerInstance.LogDebug($"Got CandiesInBag");

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(CandyBag.spawnPrefab);
                Utilities.FixMixerGroups(CandyBag.spawnPrefab);
                LethalLib.Modules.Items.RegisterItem(CandyBag);
            }

            // Getting enemy
            if (configEnablePinata.Value)
            {
                EnemyType Pinata = ModAssets.LoadAsset<EnemyType>("Assets/ModAssets/Pinata/Pinata.asset");
                if (Pinata == null) { LoggerInstance.LogError("Error: Couldnt get enemy from assets"); return; }
                LoggerInstance.LogDebug($"Got SCP-956 prefab");
                TerminalNode PinataTN = ModAssets.LoadAsset<TerminalNode>("Assets/ModAssets/Pinata/Bestiary/PinataTN.asset");
                TerminalKeyword PinataTK = ModAssets.LoadAsset<TerminalKeyword>("Assets/ModAssets/Pinata/Bestiary/PinataTK.asset");

                LoggerInstance.LogDebug("Setting rarities");

                
                LoggerInstance.LogDebug("Registering enemy network prefab...");
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(Pinata.enemyPrefab);
                LoggerInstance.LogDebug("Registering enemy...");
                Enemies.RegisterEnemy(Pinata, GetLevelRarities(config956LevelRarities.Value), GetCustomLevelRarities(config956CustomLevelRarities.Value), PinataTN, PinataTK);
            }
            
            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        public Dictionary<Levels.LevelTypes, int> GetLevelRarities(string levelsString)
        {
            try
            {
                Dictionary<Levels.LevelTypes, int> levelRaritiesDict = new Dictionary<Levels.LevelTypes, int>();

                if (levelsString != null && levelsString != "")
                {
                    string[] levels = levelsString.Split(',');

                    foreach (string level in levels)
                    {
                        string[] levelSplit = level.Split(':');
                        if (levelSplit.Length != 2) { continue; }
                        string levelType = levelSplit[0].Trim();
                        string levelRarity = levelSplit[1].Trim();

                        if (Enum.TryParse<Levels.LevelTypes>(levelType, out Levels.LevelTypes levelTypeEnum) && int.TryParse(levelRarity, out int levelRarityInt))
                        {
                            levelRaritiesDict.Add(levelTypeEnum, levelRarityInt);
                        }
                        else
                        {
                            LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                        }
                    }
                }
                return levelRaritiesDict;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error: {e}");
                return null;
            }
        }

        public Dictionary<string, int> GetCustomLevelRarities(string levelsString)
        {
            try
            {
                Dictionary<string, int> customLevelRaritiesDict = new Dictionary<string, int>();

                if (levelsString != null)
                {
                    string[] levels = levelsString.Split(',');

                    foreach (string level in levels)
                    {
                        string[] levelSplit = level.Split(':');
                        if (levelSplit.Length != 2) { continue; }
                        string levelType = levelSplit[0].Trim();
                        string levelRarity = levelSplit[1].Trim();

                        if (int.TryParse(levelRarity, out int levelRarityInt))
                        {
                            customLevelRaritiesDict.Add(levelType, levelRarityInt);
                        }
                        else
                        {
                            LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                        }
                    }
                }
                return customLevelRaritiesDict;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error: {e}");
                return null;
            }
        }

        /*public static void DespawnItemInSlotOnClient(int itemSlot)
        {
            HUDManager.Instance.itemSlotIcons[itemSlot].enabled = false;
            localPlayer.DestroyItemInSlotAndSync(itemSlot);
        }*/

        public static bool IsPlayerHoldingCandy(PlayerControllerB player) // TODO: Test if this works on network
        {
            foreach (GrabbableObject item in player.ItemSlots)
            {
                if (item == null) { continue; }
                if (CandyNames.Contains(item.itemProperties.itemName) || item.itemProperties.itemName == "Candy Bag")
                {
                    return true;
                }
            }
            return false;
        }

        public static void ResetConditions(bool endOfRound = false)
        {
            PlayerControllerBPatch.playerFrozen = false;
            FreezeLocalPlayer(false);
            StatusEffectController.Instance.bulletProofMultiplier = 0;
            SCP330Behavior.noHands = false;
            localPlayer.thisPlayerModelArms.enabled = true;

            if (PlayerAge != PlayerOriginalAge)
            {
                ChangePlayerAge(PlayerOriginalAge);
            }

            if ((NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) && FrozenPlayers != null && endOfRound)
            {
                FrozenPlayers.Clear();
            }
        }

        public static void ChangePlayerAge(int ageChange)
        {
            if (IsYoung && ageChange >= 12)
            {
                HUDManager.Instance.UIAudio.PlayOneShot(CakeDisappearsfx, 1f);
                NetworkHandler.Instance.ChangePlayerSizeServerRpc(localPlayer.actualClientId, 1f);
            }
            else
            {
                NetworkHandler.Instance.ChangePlayerSizeServerRpc(localPlayer.actualClientId, 0.7f);
                // TODO: Make players voice higher in pitch if they are a child
            }
            PlayerAge = ageChange;
        }

        public static void FreezeLocalPlayer(bool on)
        {
            localPlayer.disableMoveInput = on;
            localPlayer.disableLookInput = on;
            localPlayer.disableInteract = on;
            if (on) { localPlayer.DropAllHeldItemsAndSync(); }
        }

        private void RegisterCandy(Item candy)
        {
            if (!configEnableCandyBag.Value) { candy.toolTips[1] = ""; }
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(candy.spawnPrefab);
            Utilities.FixMixerGroups(candy.spawnPrefab);
            LethalLib.Modules.Items.RegisterItem(candy);
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
