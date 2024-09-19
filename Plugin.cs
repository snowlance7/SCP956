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
using System.Collections;
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
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_NAME = "SCP956";
        public const string PLUGIN_GUID = "Snowlance.SCP956";
        public const string PLUGIN_VERSION = "2.0.0";

        public static Plugin PluginInstance;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(PLUGIN_GUID);
        public static PlayerControllerB localPlayer { get { return GameNetworkManager.Instance.localPlayerController; } }

        public static List<string> CandyNames;

        public static AssetBundle? ModAssets;

        public static AudioClip? CakeDisappearsfx;

        public static bool localPlayerFrozen;
        public static bool localPlayerIsYoung { get { return PlayerAge < 12; } }
        public static int PlayerAge;
        public static int PlayerOriginalAge;

        // SCP-956 Configs
        public static ConfigEntry<bool> configEnablePinata;
        public static ConfigEntry<string> config956LevelRarities;
        public static ConfigEntry<string> config956CustomLevelRarities;
        public static ConfigEntry<bool> configTargetAllPlayers;
        public static ConfigEntry<float> config956ActivationRadius;
        public static ConfigEntry<float> configWarningSoundVolume;
        public static ConfigEntry<int> configHeadbuttDamage;
        public static ConfigEntry<float> configMaxTimeToKillPlayer;

        public static ConfigEntry<int> config956TeleportTime;
        public static ConfigEntry<int> config956TeleportRange;
        public static ConfigEntry<bool> config956TeleportNearPlayers;

        public static ConfigEntry<int> configMinAge;
        public static ConfigEntry<int> configMaxAge;
        public static ConfigEntry<bool> configRandomizeAgeAfterRound;

        // Candy Configs
        public static ConfigEntry<int> configCandyMinSpawn;
        public static ConfigEntry<int> configCandyMaxSpawn;
        public static ConfigEntry<bool> configEnableCandyBag;

        // SCP-559 Configs
        public static ConfigEntry<bool> configEnable559;
        public static ConfigEntry<string> config559LevelRarities;
        public static ConfigEntry<string> config559CustomLevelRarities;
        public static ConfigEntry<int> config559MinValue;
        public static ConfigEntry<int> config559MaxValue;
        public static ConfigEntry<int> config559HealAmount;
        public static ConfigEntry<bool> config559CakeReversesAge;
        public static ConfigEntry<bool> config559ReversesAgeReblow;

        // SCP-330 Configs
        public static ConfigEntry<bool> configEnable330;
        public static ConfigEntry<string> config330LevelRarities;
        public static ConfigEntry<string> config330CustomLevelRarities;

        // SCP-458 Configs
        public static ConfigEntry<bool> configEnable458;
        public static ConfigEntry<string> config458LevelRarities;
        public static ConfigEntry<string> config458CustomLevelRarities;
        public static ConfigEntry<int> config458MinValue;
        public static ConfigEntry<int> config458MaxValue;

        public static ConfigEntry<float> config458PizzaFillAmount;
        public static ConfigEntry<float> config458MetabolismRate;
        public static ConfigEntry<int> config458HealAmount;
        public static ConfigEntry<float> config458SprintMultiplier;
        public static ConfigEntry<float> config458CrouchMultiplier;
        public static ConfigEntry<float> config458DrainRate;

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
            configWarningSoundVolume = Config.Bind("SCP-956", "Warning Sound Volume", 1f, "The volume of SCP-956's warning sound. Range from 0 to 1");
            configHeadbuttDamage = Config.Bind("SCP-956", "Headbutt Damage", 50, "The amount of damage SCP-956 will do when using his headbutt attack.");
            configMaxTimeToKillPlayer = Config.Bind("SCP-956", "Max Time To Kill Player", 60f, "If SCP-956 doesnt kill a player in this amount of time, the player will die. (in lore people exposed to SCP-956 and moved away die from candy growing in their guts)");

            config956TeleportTime = Config.Bind("SCP-956", "Teleport Time", 60, "Time in seconds it takes for SCP-956 to teleport somewhere else when nobody is looking at it.");
            config956TeleportRange = Config.Bind("SCP-956", "Teleport Range", 300, "Max range around SCP-956 in which he will teleport.");
            config956TeleportNearPlayers = Config.Bind("SCP-956", "Teleport Near Players", true, "Should SCP-956 teleport around players?");

            configMinAge = Config.Bind("Player Age", "Min Age", 18, "The minimum age of a player that is decided at the beginning of a game.");
            configMaxAge = Config.Bind("Player Age", "Max Age", 70, "The maximum age of a player that is decided at the beginning of a game.");
            configRandomizeAgeAfterRound = Config.Bind("Player Age", "Randomize Age After Round", false, "Should the age of players be randomized after each round?");

            // Candy Configs
            configCandyMinSpawn = Config.Bind("Candy", "Min Candy Spawn", 5, "The minimum amount of candy to spawn when player dies to SCP-956");
            configCandyMaxSpawn = Config.Bind("Candy", "Max Candy Spawn", 30, "The maximum amount of candy to spawn when player dies to SCP-956");
            configEnableCandyBag = Config.Bind("Candy", "Enable Candy Bag", true, "Makes it so you can place candy into a separate bag.");
            
            // SCP-559 Configs
            configEnable559 = Config.Bind("SCP-559", "Enable SCP-559", true, "Set to false to disable spawning SCP-559.");
            config559LevelRarities = Config.Bind("SCP-559 Rarities", "Level Rarities", "ExperimentationLevel:25, AssuranceLevel:30, VowLevel:30, OffenseLevel:40, AdamanceLevel:45, MarchLevel:40, RendLevel:100, DineLevel:100, TitanLevel:50, ArtificeLevel:60, EmbrionLevel:25, All:40, Modded:40", "Rarities for each level. See default for formatting.");
            config559CustomLevelRarities = Config.Bind("SCP-559 Rarities", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities.");
            config559MinValue = Config.Bind("SCP-559", "SCP-559 Min Value", 50, "The minimum scrap value of SCP-559.");
            config559MaxValue = Config.Bind("SCP-559", "SCP-559 Max Value", 150, "The maximum scrap value of SCP-559.");
            config559HealAmount = Config.Bind("SCP-559", "Heal Amount", 10, "The amount of health SCP-559 will heal when eaten.");
            config559CakeReversesAge = Config.Bind("SCP-559", "Cake Reverses Age", false, "When the cake is eaten, it will reverse the players age back to their original age.");
            config559ReversesAgeReblow = Config.Bind("SCP-559", "Blowing out Reverses Age", false, "If you find another 559 cake and blow out the candles, it will reverse your age back to your original age.");

            // SCP-330 Configs
            configEnable330 = Config.Bind("SCP-330", "Enable SCP-330", true, "Set to false to disable spawning SCP-330.");
            config330LevelRarities = Config.Bind("SCP-330 Rarities", "Level Rarities", "ExperimentationLevel:30, AssuranceLevel:30, VowLevel:30, OffenseLevel:40, AdamanceLevel:45, MarchLevel:40, RendLevel:100, DineLevel:100, TitanLevel:50, ArtificeLevel:80, EmbrionLevel:45, All:30, Modded:30", "Rarities for each level. See default for formatting.");
            config330CustomLevelRarities = Config.Bind("SCP-330 Rarities", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities.");

            // SCP-458 Configs
            configEnable458 = Config.Bind("SCP-458", "Enable SCP-458", true, "Set to false to disable spawning SCP-458.");
            config458LevelRarities = Config.Bind("SCP-458 Rarities", "Level Rarities", "ExperimentationLevel:3, AssuranceLevel:4, VowLevel:4, OffenseLevel:7, AdamanceLevel:7, MarchLevel:7, RendLevel:20, DineLevel:25, TitanLevel:10, ArtificeLevel:13, EmbrionLevel:15, All:5, Modded:5", "Rarities for each level. See default for formatting.");
            config458CustomLevelRarities = Config.Bind("SCP-458 Rarities", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities.");
            config458MinValue = Config.Bind("SCP-458", "SCP-458 Min Value", 1, "The minimum scrap value of SCP-458.");
            config458MaxValue = Config.Bind("SCP-458", "SCP-458 Max Value", 750, "The maximum scrap value of SCP-458.");

            config458PizzaFillAmount = Config.Bind("SCP-458 Pizza", "Pizza Fill Amount", 0.1f, "How much of the fill meter eating a slice of pizza will fill up. Range from 0 to 1.");
            config458MetabolismRate = Config.Bind("SCP-458 Pizza", "Metabolism Rate", 0.01f, "How much of the fill meter that will be drained per x seconds. Range from 0 to 1.");
            config458HealAmount = Config.Bind("SCP-458 Pizza", "Heal Amount", 1, "The amount of health you will heal every time the fill meter is drained.");
            config458SprintMultiplier = Config.Bind("SCP-458 Pizza", "Sprint Multiplier", 2f, "How much the fill meter will be drained while sprinting. The Metabolism Rate will be multiplied by this value when sprinting.");
            config458CrouchMultiplier = Config.Bind("SCP-458 Pizza", "Crouch Multiplier", 3f, "How much the fill meter will be drained while crouching. The Metabolism Rate will be multiplied by this value when crouching and not moving.");
            config458DrainRate = Config.Bind("SCP-458 Pizza", "Drain Rate", 1.5f, "The Fill Meter will be drained by this many seconds.");

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
            CakeDisappearsfx = ModAssets.LoadAsset<AudioClip>("Assets/ModAssets/Cake/Audio/cake_disappear.wav");
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
                Item Cake = ModAssets.LoadAsset<Item>("Assets/ModAssets/Cake/Cake559Item.asset");
                if (Cake == null) { LoggerInstance.LogError("Error: Couldnt get cake from assets"); return; }
                LoggerInstance.LogDebug($"Got Cake prefab");

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(Cake.spawnPrefab);
                Utilities.FixMixerGroups(Cake.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(Cake);
            }

            // Getting SCP-330
            if (configEnable330.Value)
            {
                Item BowlOfCandy = ModAssets.LoadAsset<Item>("Assets/ModAssets/CandyBowl/CandyBowlItem.asset");
                if (BowlOfCandy == null) { LoggerInstance.LogError("Error: Couldnt get bowl of candy from assets"); return; }
                LoggerInstance.LogDebug($"Got bowl of candy prefab");

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(BowlOfCandy.spawnPrefab);
                Utilities.FixMixerGroups(BowlOfCandy.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(BowlOfCandy, GetLevelRarities(config330LevelRarities.Value), GetCustomLevelRarities(config330CustomLevelRarities.Value));

                Item BowlOfCandyP = ModAssets.LoadAsset<Item>("Assets/ModAssets/CandyBowl/CandyBowlPItem.asset");
                if (BowlOfCandyP == null) { LoggerInstance.LogError("Error: Couldnt get bowl of candy from assets"); return; }
                LoggerInstance.LogDebug($"Got bowl of candy P prefab");

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(BowlOfCandyP.spawnPrefab);
                Utilities.FixMixerGroups(BowlOfCandyP.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(BowlOfCandyP, GetLevelRarities(config330LevelRarities.Value), GetCustomLevelRarities(config330CustomLevelRarities.Value));
            }

            // Getting Candy

            Item CandyPink = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/PinkCandyItem.asset");
            if (CandyPink == null) { LoggerInstance.LogError("Error: Couldnt get CandyPink from assets"); return; }
            RegisterCandy(CandyPink);
            LoggerInstance.LogDebug($"Got CandyPink");
            Item CandyPurple = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/PurpleCandyItem.asset");
            if (CandyPurple == null) { LoggerInstance.LogError("Error: Couldnt get CandyPurple from assets"); return; }
            RegisterCandy(CandyPurple);
            LoggerInstance.LogDebug($"Got CandyPurple");
            Item CandyRed = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/RedCandyItem.asset");
            if (CandyRed == null) { LoggerInstance.LogError("Error: Couldnt get CandyRed from assets"); return; }
            RegisterCandy(CandyRed);
            LoggerInstance.LogDebug($"Got CandyRed");
            Item CandyYellow = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/YellowCandyItem.asset");
            if (CandyYellow == null) { LoggerInstance.LogError("Error: Couldnt get CandyYellow from assets"); return; }
            RegisterCandy(CandyYellow);
            LoggerInstance.LogDebug($"Got CandyYellow");
            Item CandyGreen = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/GreenCandyItem.asset");
            if (CandyGreen == null) { LoggerInstance.LogError("Error: Couldnt get CandyGreen from assets"); return; }
            RegisterCandy(CandyGreen);
            LoggerInstance.LogDebug($"Got CandyGreen");
            Item CandyBlue = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/BlueCandyItem.asset");
            if (CandyBlue == null) { LoggerInstance.LogError("Error: Couldnt get CandyBlue from assets"); return; }
            RegisterCandy(CandyBlue);
            LoggerInstance.LogDebug($"Got CandyBlue");
            Item CandyRainbow = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/RainbowCandyItem.asset");
            if (CandyRainbow == null) { LoggerInstance.LogError("Error: Couldnt get CandyRainbow from assets"); return; }
            RegisterCandy(CandyRainbow);
            LoggerInstance.LogDebug($"Got CandyRainbow");
            Item CandyBlack = ModAssets.LoadAsset<Item>("Assets/ModAssets/Candy/BlackCandyItem.asset");
            if (CandyBlack == null) { LoggerInstance.LogError("Error: Couldnt get CandyBlack from assets"); return; }
            RegisterCandy(CandyBlack);
            LoggerInstance.LogDebug($"Got CandyBlack");

            CandyNames = new List<string> { CandyPink.name, CandyPurple.name, CandyRed.name, CandyYellow.name, CandyGreen.name, CandyBlue.name, CandyRainbow.name, CandyBlack.name };

            // Getting Candy Bag
            if (configEnableCandyBag.Value)
            {
                Item CandyBag = ModAssets.LoadAsset<Item>("Assets/ModAssets/CandyBag/CandyBagItem.asset");
                if (CandyBag == null) { LoggerInstance.LogError("Error: Couldnt get CandyBag from assets"); return; }
                LoggerInstance.LogDebug($"Got CandyBag prefab");

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(CandyBag.spawnPrefab);
                Utilities.FixMixerGroups(CandyBag.spawnPrefab);
                LethalLib.Modules.Items.RegisterItem(CandyBag);
            }

            if (configEnable458.Value)
            {
                Item Pizza = ModAssets.LoadAsset<Item>("Assets/ModAssets/Pizza/SCP458Item.asset");
                if (Pizza == null) { LoggerInstance.LogError("Error: Couldnt get pizza from assets"); return; }
                LoggerInstance.LogDebug($"Got pizza prefab");

                Pizza.minValue = config458MinValue.Value;
                Pizza.maxValue = config458MaxValue.Value;

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(Pizza.spawnPrefab);
                Utilities.FixMixerGroups(Pizza.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(Pizza, GetLevelRarities(config458LevelRarities.Value), GetCustomLevelRarities(config458CustomLevelRarities.Value));
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
            Logger.LogInfo($"{PLUGIN_GUID} v{PLUGIN_VERSION} has loaded!");
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

        public static bool IsPlayerHoldingCandy(PlayerControllerB player)
        {
            foreach (GrabbableObject item in player.ItemSlots)
            {
                if (item == null) { continue; }
                if (CandyNames.Contains(item.itemProperties.name) || item.itemProperties.name == "CandyBagItem")
                {
                    return true;
                }
            }
            return false;
        }

        public static void ResetConditions(bool endOfRound = false)
        {
            localPlayerFrozen = false;
            FreezeLocalPlayer(false);
            StatusEffectController.Instance.bulletProofMultiplier = 0;
            SCP330Behavior.noHands = false;
            if (localPlayer.thisPlayerModelArms.enabled == false)
            {
                NetworkHandler.Instance.SetPlayerArmsVisibleServerRpc(localPlayer.actualClientId, true);
            }

            if (endOfRound && configRandomizeAgeAfterRound.Value)
            {
                ChangePlayerAge(UnityEngine.Random.Range(configMinAge.Value, configMaxAge.Value + 1));
                PlayerOriginalAge = PlayerAge;
                //SoundManager.Instance.playerVoicePitches[localPlayer.actualClientId]
            }
            else
            {
                ChangePlayerAge(PlayerOriginalAge);
            }

            if ((NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) && endOfRound)
            {
                SCP956AI.FrozenPlayers.Clear();
                SCP956AI.YoungPlayers.Clear();
                SCP956AI.PlayersRecievedWarning.Clear();
            }
        }

        public static void ChangePlayerAge(int ageChange)
        {
            PlayerAge = ageChange;

            if (localPlayerIsYoung)
            {
                NetworkHandler.Instance.ChangePlayerSizeServerRpc(localPlayer.actualClientId, 0.7f);
                /* // TODO: Make players voice higher in pitch if they are a child
                float num11 = StartOfRound.Instance.drunknessSideEffect.Evaluate(drunkness);
		        if (num11 > 0.15f)
		        {
			        SoundManager.Instance.playerVoicePitchTargets[playerClientId] = 1f + num11;
		        }
		        else
		        {
			        SoundManager.Instance.playerVoicePitchTargets[playerClientId] = 1f;
		        }
                 */
            }
            else
            {
                HUDManager.Instance.UIAudio.PlayOneShot(CakeDisappearsfx, 1f);
                NetworkHandler.Instance.ChangePlayerSizeServerRpc(localPlayer.actualClientId, 1f);
            }
        }

        public static void FreezeLocalPlayer(bool on)
        {
            localPlayerFrozen = on;
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
