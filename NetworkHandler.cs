using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using static Netcode.Transports.Facepunch.FacepunchTransport;

namespace SCP956
{
    public class NetworkHandler : NetworkBehaviour
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        public static NetworkHandler Instance { get; private set; }
        public static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public static PlayerControllerB PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[id]]; }

        /*// Secret Lab // TODO: Add config syncing
        public NetworkVariable<bool> configSecretLab = new NetworkVariable<bool>();
        public NetworkVariable<int> config956SpawnRadius = new NetworkVariable<int>();
        public NetworkVariable<int> config956TeleportTime = new NetworkVariable<int>();
        public NetworkVariable<int> config956TeleportRange = new NetworkVariable<int>();

        // Rarity
        public NetworkVariable<int> configExperimentationLevelRarity = new NetworkVariable<int>();
        public NetworkVariable<int> configAssuranceLevelRarity = new NetworkVariable<int>();
        public NetworkVariable<int> configVowLevelRarity = new NetworkVariable<int>();
        public NetworkVariable<int> configOffenseLevelRarity = new NetworkVariable<int>();
        public NetworkVariable<int> configMarchLevelRarity = new NetworkVariable<int>();
        public NetworkVariable<int> configRendLevelRarity = new NetworkVariable<int>();
        public NetworkVariable<int> configDineLevelRarity = new NetworkVariable<int>();
        public NetworkVariable<int> configTitanLevelRarity = new NetworkVariable<int>();
        public NetworkVariable<int> configModdedLevelRarity = new NetworkVariable<int>();
        public NetworkVariable<int> configOtherLevelRarity = new NetworkVariable<int>();

        // SCP-956 Configs
        public NetworkVariable<bool> configEnablePinata = new NetworkVariable<bool>();
        public NetworkVariable<bool> configTargetAllPlayers = new NetworkVariable<bool>();
        public NetworkVariable<float> config956ActivationRadius = new NetworkVariable<float>();
        public NetworkVariable<int> configMinAge = new NetworkVariable<int>();
        public NetworkVariable<int> configMaxAge = new NetworkVariable<int>();
        public NetworkVariable<bool> configPlayWarningSound = new NetworkVariable<bool>();
        public NetworkVariable<int> configHeadbuttDamage = new NetworkVariable<int>();

        // Candy Configs
        public NetworkVariable<int> configCandyMinSpawn = new NetworkVariable<int>();
        public NetworkVariable<int> configCandyMaxSpawn = new NetworkVariable<int>();
        public NetworkVariable<int> configCandyDeathChance = new NetworkVariable<int>();
        public NetworkVariable<bool> configEnableCandyBag = new NetworkVariable<bool>();

        // SCP-559 Configs
        public NetworkVariable<bool> configEnable559 = new NetworkVariable<bool>();
        public NetworkVariable<int> config559Rarity = new NetworkVariable<int>();
        public NetworkVariable<int> config559MinValue = new NetworkVariable<int>();
        public NetworkVariable<int> config559MaxValue = new NetworkVariable<int>();
        public NetworkVariable<int> config559HealAmount = new NetworkVariable<int>();

        // SCP-330 Configs
        public NetworkVariable<bool> configEnable330 = new NetworkVariable<bool>();
        public NetworkVariable<int> config330Rarity = new NetworkVariable<int>();

        // Status Effect Configs
        public NetworkVariable<bool> configEnableCustomStatusEffects = new NetworkVariable<bool>();
        public NetworkVariable<string> configCandyPurpleEffects = new NetworkVariable<string>();
        public NetworkVariable<string> configCandyRedEffects = new NetworkVariable<string>();
        public NetworkVariable<string> configCandyYellowEffects = new NetworkVariable<string>();
        public NetworkVariable<string> configCandyGreenEffects = new NetworkVariable<string>();
        public NetworkVariable<string> configCandyBlueEffects = new NetworkVariable<string>();*/

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
                logger.LogDebug("Despawned network object");
            }

            Instance = this;
            logger.LogDebug("set instance to this");
            base.OnNetworkSpawn();
            logger.LogDebug("base.OnNetworkSpawn");

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) { Plugin.FrozenPlayers = new List<PlayerControllerB>(); }

            /*if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                configSecretLab.Value = Plugin.configSecretLab.Value;
                config956SpawnRadius.Value = Plugin.config956SpawnRadius.Value;
                config956TeleportTime.Value = Plugin.config956TeleportTime.Value;
                config956TeleportRange.Value = Plugin.config956TeleportRange.Value;

                configExperimentationLevelRarity.Value = Plugin.configExperimentationLevelRarity.Value;
                configAssuranceLevelRarity.Value = Plugin.configAssuranceLevelRarity.Value;
                configVowLevelRarity.Value = Plugin.configVowLevelRarity.Value;
                configOffenseLevelRarity.Value = Plugin.configOffenseLevelRarity.Value;
                configMarchLevelRarity.Value = Plugin.configMarchLevelRarity.Value;
                configRendLevelRarity.Value = Plugin.configRendLevelRarity.Value;
                configDineLevelRarity.Value = Plugin.configDineLevelRarity.Value;
                configTitanLevelRarity.Value = Plugin.configTitanLevelRarity.Value;
                configModdedLevelRarity.Value = Plugin.configModdedLevelRarity.Value;
                configOtherLevelRarity.Value = Plugin.configOtherLevelRarity.Value;

                configEnablePinata.Value = Plugin.configEnablePinata.Value;
                configTargetAllPlayers.Value = Plugin.configTargetAllPlayers.Value;
                config956ActivationRadius.Value = Plugin.config956ActivationRadius.Value;
                configMinAge.Value = Plugin.configMinAge.Value;
                configMaxAge.Value = Plugin.configMaxAge.Value;
                configPlayWarningSound.Value = Plugin.configPlayWarningSound.Value;
                configHeadbuttDamage.Value = Plugin.configHeadbuttDamage.Value;

                configCandyMinSpawn.Value = Plugin.configCandyMinSpawn.Value;
                configCandyMaxSpawn.Value = Plugin.configCandyMaxSpawn.Value;
                configCandyDeathChance.Value = Plugin.configCandyDeathChance.Value;
                configEnableCandyBag.Value = Plugin.configEnableCandyBag.Value;

                configEnable559.Value = Plugin.configEnable559.Value;
                config559Rarity.Value = Plugin.config559Rarity.Value;
                config559MinValue.Value = Plugin.config559MinValue.Value;
                config559MaxValue.Value = Plugin.config559MaxValue.Value;
                config559HealAmount.Value = Plugin.config559HealAmount.Value;

                configEnable330.Value = Plugin.configEnable330.Value;
                config330Rarity.Value = Plugin.config330Rarity.Value;

                configEnableCustomStatusEffects.Value = Plugin.configEnableCustomStatusEffects.Value;
                configCandyPurpleEffects.Value = Plugin.configCandyPurpleEffects.Value;
                configCandyRedEffects.Value = Plugin.configCandyRedEffects.Value;
                configCandyYellowEffects.Value = Plugin.configCandyYellowEffects.Value;
                configCandyGreenEffects.Value = Plugin.configCandyGreenEffects.Value;
                configCandyBlueEffects.Value = Plugin.configCandyBlueEffects.Value;
            }*/
        }

        [ClientRpc]
        private void GrabObjectClientRpc(ulong id, ulong clientId) // TODO: Figure out how to turn off grab animation
        {
            if (clientId == localPlayer.actualClientId)
            {
                if (localPlayer.ItemSlots.Where(x => x == null).Any())
                {
                    GrabbableObject grabbableItem = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject.GetComponent<GrabbableObject>();
                    logger.LogDebug($"Grabbing item with weight: {grabbableItem.itemProperties.weight}");

                    localPlayer.GrabObjectServerRpc(grabbableItem.NetworkObject);
                    grabbableItem.parentObject = localPlayer.localItemHolder;
                    grabbableItem.GrabItemOnClient();
                }
            }
        }

        [ClientRpc]
        private void ChangePlayerSizeClientRpc(ulong clientId, float size)
        {
            PlayerControllerB playerHeldBy = StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[clientId]];

            playerHeldBy.thisPlayerBody.localScale = new Vector3(size, size, size);

            if (size < 1f)
            {
                playerHeldBy.movementSpeed = 5.7f;
                playerHeldBy.sprintTime = 17f;
            }
            else
            {
                playerHeldBy.movementSpeed = 4.6f;
                playerHeldBy.sprintTime = 11f;
            }
            //playerHeldBy.playerGlobalHead.localScale = new Vector3(2f, 2f, 2f);
            //playerHeldBy.usernameBillboard.position = new Vector3(playerHeldBy.usernameBillboard.position.x, playerHeldBy.usernameBillboard.position.y + 0.23f, playerHeldBy.usernameBillboard.position.z);
            //playerHeldBy.usernameBillboard.localScale *= 1.5f;
            //playerHeldBy.gameplayCamera.transform.GetChild(0).position = new Vector3(playerHeldBy.gameplayCamera.transform.GetChild(0).position.x, playerHeldBy.gameplayCamera.transform.GetChild(0).position.y - 0.026f, playerHeldBy.gameplayCamera.transform.GetChild(0).position.z + 0.032f);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnPinataServerRpc(Vector3 pos = default)
        {
            if ((NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) && Plugin.configEnablePinata.Value)
            {
                SpawnableEnemyWithRarity enemy = RoundManager.Instance.currentLevel.Enemies.Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault();
                if (enemy == null) { return; }
                int index = RoundManager.Instance.currentLevel.Enemies.IndexOf(enemy);

                if (pos != default)
                {
                    logger.LogDebug("Spawning SCP-956 at: " + pos);
                    RoundManager.Instance.SpawnEnemyOnServer(pos, Quaternion.identity.y, index);
                    return;
                }

                List<EnemyVent> vents = RoundManager.Instance.allEnemyVents.ToList();
                logger.LogDebug("Found vents: " + vents.Count);

                EnemyVent vent = vents[UnityEngine.Random.Range(0, vents.Count)];
                logger.LogDebug("Selected vent: " + vent);

                vent.enemyTypeIndex = index;
                vent.enemyType = enemy.enemyType;
                logger.LogDebug("Updated vent with enemy type index: " + vent.enemyTypeIndex + " and enemy type: " + vent.enemyType);

                RoundManager.Instance.SpawnEnemyFromVent(vent);
                logger.LogDebug("Spawning SCP-956 from vent");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnPinataNearbyServerRpc(Vector3 playerPos)
        {
            if ((NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) && Plugin.configEnablePinata.Value)
            {
                Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(playerPos, Plugin.config956SpawnRadius.Value, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
                int index = RoundManager.Instance.currentLevel.Enemies.FindIndex(x => x.enemyType.enemyName == "SCP-956");
                RoundManager.Instance.SpawnEnemyOnServer(pos, UnityEngine.Random.Range(0f, 360f), index);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangePlayerSizeServerRpc(ulong clientId, float size)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                ChangePlayerSizeClientRpc(clientId, size);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddToFrozenPlayersListServerRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                logger.LogDebug($"Adding {clientId} to frozen players list");
                PlayerControllerB player = PlayerFromId(clientId);
                Plugin.FrozenPlayers.Add(player);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnItemServerRpc(ulong clientId, string _itemName, int newValue, Vector3 pos, UnityEngine.Quaternion rot, bool grabItem = false, bool pinataCandy = true)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Item item = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == _itemName).FirstOrDefault();
                logger.LogDebug("Got item");

                GameObject obj = UnityEngine.Object.Instantiate(item.spawnPrefab, pos, rot, StartOfRound.Instance.propsContainer);
                if (newValue != 0) { obj.GetComponent<GrabbableObject>().SetScrapValue(newValue); }
                logger.LogDebug($"Spawning item with weight: {obj.GetComponent<GrabbableObject>().itemProperties.weight}");
                obj.GetComponent<NetworkObject>().Spawn();

                if (Plugin.CandyNames.Contains(_itemName) && !pinataCandy) { obj.GetComponent<CandyBehavior>().pinataCandy = false; }

                if (grabItem)
                {
                    GrabObjectClientRpc(obj.GetComponent<NetworkObject>().NetworkObjectId, clientId);
                    logger.LogDebug("Grabbed obj");
                }

                if (_itemName == "SCP-559")
                {
                    obj.GetComponent<AudioSource>().PlayOneShot(Plugin.CakeAppearsfx); // TODO: Play all other audio in project like this?
                    logger.LogDebug("Played cake sfx");
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DespawnDeadPlayerServerRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts.Where(x => x.actualClientId == clientId).FirstOrDefault();
                if (player == null) { return; }

                UnityEngine.Object.Destroy(player.deadBody.gameObject);
            }
        }

        /*[ServerRpc(RequireOwnership = false)]
        public void SyncConfigsServerRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                SyncConfigsClientRpc(clientId);
            }
        }

        [ClientRpc]
        public void SyncConfigsClientRpc(ulong clientId)
        {
            if (clientId != localPlayer.actualClientId) { return; }

            Plugin.configSecretLab.Value = configSecretLab.Value;
            Plugin.config956SpawnRadius.Value = config956SpawnRadius.Value;
            Plugin.config956TeleportTime.Value = config956TeleportTime.Value;
            Plugin.config956TeleportRange.Value = config956TeleportRange.Value;

            Plugin.configExperimentationLevelRarity.Value = configExperimentationLevelRarity.Value;
            Plugin.configAssuranceLevelRarity.Value = configAssuranceLevelRarity.Value;
            Plugin.configVowLevelRarity.Value = configVowLevelRarity.Value;
            Plugin.configOffenseLevelRarity.Value = configOffenseLevelRarity.Value;
            Plugin.configMarchLevelRarity.Value = configMarchLevelRarity.Value;
            Plugin.configRendLevelRarity.Value = configRendLevelRarity.Value;
            Plugin.configDineLevelRarity.Value = configDineLevelRarity.Value;
            Plugin.configTitanLevelRarity.Value = configTitanLevelRarity.Value;
            Plugin.configModdedLevelRarity.Value = configModdedLevelRarity.Value;
            Plugin.configOtherLevelRarity.Value = configOtherLevelRarity.Value;

            Plugin.configEnablePinata.Value = configEnablePinata.Value;
            Plugin.configTargetAllPlayers.Value = configTargetAllPlayers.Value;
            Plugin.config956ActivationRadius.Value = config956ActivationRadius.Value;
            Plugin.configMinAge.Value = configMinAge.Value;
            Plugin.configMaxAge.Value = configMaxAge.Value;
            Plugin.configPlayWarningSound.Value = configPlayWarningSound.Value;
            Plugin.configHeadbuttDamage.Value = configHeadbuttDamage.Value;

            Plugin.configCandyMinSpawn.Value = configCandyMinSpawn.Value;
            Plugin.configCandyMaxSpawn.Value = configCandyMaxSpawn.Value;
            Plugin.configCandyDeathChance.Value = configCandyDeathChance.Value;
            Plugin.configEnableCandyBag.Value = configEnableCandyBag.Value;

            Plugin.configEnable559.Value = configEnable559.Value;
            Plugin.config559Rarity.Value = config559Rarity.Value;
            Plugin.config559MinValue.Value = config559MinValue.Value;
            Plugin.config559MaxValue.Value = config559MaxValue.Value;
            Plugin.config559HealAmount.Value = config559HealAmount.Value;

            Plugin.configEnable330.Value = configEnable330.Value;
            Plugin.config330Rarity.Value = config330Rarity.Value;

            Plugin.configEnableCustomStatusEffects.Value = configEnableCustomStatusEffects.Value;
            Plugin.configCandyPurpleEffects.Value = configCandyPurpleEffects.Value;
            Plugin.configCandyRedEffects.Value = configCandyRedEffects.Value;
            Plugin.configCandyYellowEffects.Value = configCandyYellowEffects.Value;
            Plugin.configCandyGreenEffects.Value = configCandyGreenEffects.Value;
            Plugin.configCandyBlueEffects.Value = configCandyBlueEffects.Value;
        }*/
    }

    [HarmonyPatch]
    public class NetworkObjectManager
    {
        static GameObject networkPrefab;
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            logger.LogDebug("Initializing network prefab...");
            if (networkPrefab != null)
                return;

            networkPrefab = (GameObject)Plugin.ModAssets.LoadAsset("Assets/ModAssets/Pinata/NetworkHandlerSCP956.prefab");
            logger.LogDebug("Got networkPrefab");
            networkPrefab.AddComponent<NetworkHandler>();
            logger.LogDebug("Added component");

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
            logger.LogDebug("Added networkPrefab");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                logger.LogDebug("Instantiated networkHandlerHost");
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
                logger.LogDebug("Spawned network object");
            }
        }
    }
}