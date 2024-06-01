using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using static SCP956.SCP956;

namespace SCP956
{
    public class NetworkHandler : NetworkBehaviour
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        public static NetworkHandler Instance { get; private set; }
        public static PlayerControllerB CurrentClient { get { return StartOfRound.Instance.localPlayerController; } }

        public static PlayerControllerB PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[id]]; }

        public NetworkList<ulong> FrozenPlayers = new NetworkList<ulong>();
        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            }

            Instance = this;
            base.OnNetworkSpawn();
        }

        [ClientRpc]
        private void GrabObjectClientRpc(ulong id, ulong clientId)
        {
            if (clientId == CurrentClient.actualClientId)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[clientId]];
                GrabbableObject grabbableItem = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject.GetComponent<GrabbableObject>();

                player.carryWeight += Mathf.Clamp(grabbableItem.itemProperties.weight - 1f, 0f, 10f);
                player.GrabObjectServerRpc(grabbableItem.NetworkObject);
                grabbableItem.parentObject = player.localItemHolder;
                grabbableItem.GrabItemOnClient();
            }
        }

        [ClientRpc]
        private void ChangePlayerSizeClientRpc(ulong clientId, float size)
        {
            PlayerControllerB playerHeldBy = StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[clientId]];

            playerHeldBy.thisPlayerBody.localScale = new Vector3(size, size, size);
            //playerHeldBy.playerGlobalHead.localScale = new Vector3(2f, 2f, 2f);
            //playerHeldBy.usernameBillboard.position = new Vector3(playerHeldBy.usernameBillboard.position.x, playerHeldBy.usernameBillboard.position.y + 0.23f, playerHeldBy.usernameBillboard.position.z);
            //playerHeldBy.usernameBillboard.localScale *= 1.5f;
            //playerHeldBy.gameplayCamera.transform.GetChild(0).position = new Vector3(playerHeldBy.gameplayCamera.transform.GetChild(0).position.x, playerHeldBy.gameplayCamera.transform.GetChild(0).position.y - 0.026f, playerHeldBy.gameplayCamera.transform.GetChild(0).position.z + 0.032f);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnPinataServerRpc() // TODO: Causing errors during generatenewfloor patch
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                SpawnableEnemyWithRarity enemy = RoundManager.Instance.currentLevel.Enemies.Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault();
                logger.LogDebug("Found enemy: " + enemy);

                List<EnemyVent> vents = RoundManager.Instance.allEnemyVents.ToList();
                logger.LogDebug("Found vents: " + vents.Count);

                EnemyVent vent = vents[UnityEngine.Random.Range(0, vents.Count)];
                logger.LogDebug("Selected vent: " + vent);

                vent.enemyTypeIndex = RoundManager.Instance.currentLevel.Enemies.IndexOf(enemy);
                vent.enemyType = enemy.enemyType;
                logger.LogDebug("Updated vent with enemy type index: " + vent.enemyTypeIndex + " and enemy type: " + vent.enemyType);

                RoundManager.Instance.SpawnEnemyFromVent(vent);
                logger.LogDebug("Spawned SCP-956 from vent");
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
                FrozenPlayers.Add(clientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnItemServerRpc(ulong clientId, string _itemName, int newValue, Vector3 pos, UnityEngine.Quaternion rot, bool playCakeSFX = false, bool grabItem = false)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Item item = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == _itemName).FirstOrDefault();

                GameObject obj = UnityEngine.Object.Instantiate(item.spawnPrefab, pos, rot, StartOfRound.Instance.propsContainer);
                obj.GetComponent<GrabbableObject>().fallTime = 0f;
                obj.GetComponent<GrabbableObject>().SetScrapValue(newValue);
                obj.GetComponent<NetworkObject>().Spawn();

                if (grabItem)
                {
                    GrabObjectClientRpc(obj.GetComponent<NetworkObject>().NetworkObjectId, clientId);
                }

                if (playCakeSFX)
                {
                    obj.GetComponent<AudioSource>().PlayOneShot(CakeAppearsfx);
                }
            }
        } // TODO: Figure out how to spawn cake in players hand instead of on the floor
    }

    [HarmonyPatch]
    public class NetworkObjectManager
    {
        static GameObject networkPrefab;
        private static ManualLogSource logger = SCP956.LoggerInstance;

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            if (networkPrefab != null)
                return;

            networkPrefab = (GameObject)SCP956.ModAssets.LoadAsset("Assets/ModAssets/Pinata/NetworkHandler.prefab");
            networkPrefab.AddComponent<NetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}