using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace SCP956
{
    public class NetworkHandler : NetworkBehaviour
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        public static NetworkHandler Instance { get; private set; }
        public static PlayerControllerB CurrentClient { get { return StartOfRound.Instance.localPlayerController; } }

        public NetworkList<ulong> FrozenPlayers = new NetworkList<ulong>();
        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            }

            Instance = this;
            FrozenPlayers.Clear();
            base.OnNetworkSpawn();
        }

        public void ShrinkPlayer(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                ShrinkPlayerClientRpc(clientId);
            }
            else
            {
                ShrinkPlayerServerRpc(clientId);
            }
        }

        public void AddToFrozenPlayersList(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                AddToListClientRpc(clientId);
            }
            else
            {
                AddToListServerRpc(clientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ShrinkPlayerServerRpc(ulong clientId)
        {
            ShrinkPlayerClientRpc(clientId);
        }

        [ClientRpc]
        private void ShrinkPlayerClientRpc(ulong clientId)
        {
            logger.LogDebug("ReceivedFromClientShrinkPlayer()");
            logger.LogDebug($"{clientId}");
            PlayerControllerB playerHeldBy = StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[clientId]];

            playerHeldBy.thisPlayerBody.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            //playerHeldBy.playerGlobalHead.localScale = new Vector3(2f, 2f, 2f);
            //playerHeldBy.usernameBillboard.position = new Vector3(playerHeldBy.usernameBillboard.position.x, playerHeldBy.usernameBillboard.position.y + 0.23f, playerHeldBy.usernameBillboard.position.z);
            //playerHeldBy.usernameBillboard.localScale *= 1.5f;
            //playerHeldBy.gameplayCamera.transform.GetChild(0).position = new Vector3(playerHeldBy.gameplayCamera.transform.GetChild(0).position.x, playerHeldBy.gameplayCamera.transform.GetChild(0).position.y - 0.026f, playerHeldBy.gameplayCamera.transform.GetChild(0).position.z + 0.032f);
        }

        [ServerRpc(RequireOwnership = false)]
        private void AddToListServerRpc(ulong clientId)
        {
            AddToListClientRpc(clientId);
        }

        [ClientRpc]
        private void AddToListClientRpc(ulong clientId)
        {
            FrozenPlayers.Add(clientId);
        }
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