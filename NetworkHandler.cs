using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using GameNetcodeStuff;
using LethalNetworkAPI;
using UnityEngine;

namespace SCP956
{
    internal static class NetworkHandler
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        public static PlayerControllerB CurrentClient
        {
            get
            {
                return StartOfRound.Instance.localPlayerController;
            }
        }

        public static LethalNetworkVariable<List<ulong>> UnfortunatePlayers = new LethalNetworkVariable<List<ulong>>(identifier: "playersToDie");

        public static LethalClientEvent clientEventShrinkPlayer = new LethalClientEvent(identifier: "shrinkPlayer");

        public static LethalClientMessage<Vector3> clientMessageSpawnEnemy = new LethalClientMessage<Vector3>(identifier: "spawnEnemy");
        public static LethalServerMessage<Vector3> serverMessageSpawnEnemy = new LethalServerMessage<Vector3>(identifier: "spawnEnemy");

        public static void Init()
        {
            UnfortunatePlayers.Value = new List<ulong>();
            clientEventShrinkPlayer.OnReceivedFromClient += ReceivedFromClientShrinkPlayer;
            serverMessageSpawnEnemy.OnReceived += ReceivedFromClientSpawnEnemy;
        }

        private static void ReceivedFromClientSpawnEnemy(Vector3 position, ulong clientid)
        {
            throw new NotImplementedException(); // TODO: Finish this
        }

        private static void ReceivedFromClientShrinkPlayer(ulong clientid)
        {
            logger.LogDebug("ReceivedFromClientShrinkPlayer()");
            logger.LogDebug($"{clientid}");
            PlayerControllerB playerHeldBy = clientid.GetPlayerController();

            playerHeldBy.thisPlayerBody.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            //playerHeldBy.playerGlobalHead.localScale = new Vector3(2f, 2f, 2f);
            //playerHeldBy.usernameBillboard.position = new Vector3(playerHeldBy.usernameBillboard.position.x, playerHeldBy.usernameBillboard.position.y + 0.23f, playerHeldBy.usernameBillboard.position.z);
            //playerHeldBy.usernameBillboard.localScale *= 1.5f;
            //playerHeldBy.gameplayCamera.transform.GetChild(0).position = new Vector3(playerHeldBy.gameplayCamera.transform.GetChild(0).position.x, playerHeldBy.gameplayCamera.transform.GetChild(0).position.y - 0.026f, playerHeldBy.gameplayCamera.transform.GetChild(0).position.z + 0.032f);
        }
    }
}