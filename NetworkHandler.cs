using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956
{
    public List<PlayerControllerB> YoungPlayers
    {

    }

    public class NetworkHandler : NetworkBehaviour
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        public static NetworkHandler Instance { get; private set; }

        public static PlayerControllerB PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[id]]; }

        public NetworkList<ulong> YoungPlayersIds = new NetworkList<ulong>();
        public NetworkList<ulong> FrozenPlayersIds = new NetworkList<ulong>();

        public List<PlayerControllerB> YoungPlayers
        {
            get
            {
                return StartOfRound.Instance.allPlayerScripts.Where(x => YoungPlayersIds.Contains(x.actualClientId)).ToList();
            }
        }

        public List<PlayerControllerB> FrozenPlayers
        {
            get
            {
                return StartOfRound.Instance.allPlayerScripts.Where(x => FrozenPlayersIds.Contains(x.actualClientId)).ToList();
            }
        }

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
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnPinataServerRpc(Vector3? pos = null)
        {
            if ((NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) && Plugin.configEnablePinata.Value)
            {
                SpawnableEnemyWithRarity enemy = RoundManager.Instance.currentLevel.Enemies.Where(x => x.enemyType.name == "Pinata").FirstOrDefault();
                if (enemy == null) { logger.LogError("Pinata enemy not found"); return; }
                int index = RoundManager.Instance.currentLevel.Enemies.IndexOf(enemy);

                if (pos != null)
                {
                    logger.LogDebug("Spawning SCP-956 at: " + pos);
                    RoundManager.Instance.SpawnEnemyOnServer(pos.Value, Quaternion.identity.y, index);
                    return;
                }

                List<EnemyVent> vents = RoundManager.Instance.allEnemyVents.ToList();
                logger.LogDebug("Found vents: " + vents.Count);

                EnemyVent vent = vents[UnityEngine.Random.Range(0, vents.Count - 1)];
                logger.LogDebug("Selected vent: " + vent);

                vent.enemyTypeIndex = index;
                vent.enemyType = enemy.enemyType;
                logger.LogDebug("Updated vent with enemy type index: " + vent.enemyTypeIndex + " and enemy type: " + vent.enemyType);

                RoundManager.Instance.SpawnEnemyFromVent(vent);
                logger.LogDebug("Spawning SCP-956 from vent");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangePlayerSizeServerRpc(ulong clientId, float size)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (size < 1f)
                {
                    if (!YoungPlayersIds.Contains(clientId)) { YoungPlayersIds.Add(clientId); }
                }
                else
                {
                    if (YoungPlayersIds.Contains(clientId)) { YoungPlayersIds.Remove(clientId); }
                }

                ChangePlayerSizeClientRpc(clientId, size);
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
        public void SpawnItemServerRpc(ulong clientId, string _itemName, int newValue = 0, Vector3 pos = default, UnityEngine.Quaternion rot = default, bool grabItem = false)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Item item = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == _itemName).FirstOrDefault();
                logger.LogDebug("Got item");

                GameObject obj = UnityEngine.Object.Instantiate(item.spawnPrefab, pos, rot, StartOfRound.Instance.propsContainer);
                obj.GetComponent<GrabbableObject>().fallTime = 0;
                if (newValue != 0) { obj.GetComponent<GrabbableObject>().SetScrapValue(newValue); }
                logger.LogDebug($"Spawning item with weight: {obj.GetComponent<GrabbableObject>().itemProperties.weight}");
                obj.GetComponent<NetworkObject>().Spawn();

                if (grabItem)
                {
                    GrabObjectClientRpc(obj.GetComponent<NetworkObject>().NetworkObjectId, clientId);
                    logger.LogDebug("Grabbed obj");
                }
            }
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

        [ServerRpc(RequireOwnership = false)]
        public void DespawnDeadPlayerServerRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                PlayerControllerB player = PlayerFromId(clientId);
                if (player == null) { return; }

                UnityEngine.Object.Destroy(player.deadBody.gameObject);
            }
        }
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
            //networkPrefab.AddComponent<NetworkHandler>();
            //logger.LogDebug("Added component");

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
            logger.LogDebug("Added networkPrefab");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
            }
        }
    }
}