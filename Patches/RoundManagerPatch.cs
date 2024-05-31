using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LethalLib.Modules.Enemies;
using static SCP956.SCP956;
using Unity.Netcode;
using System.Linq;
using static UnityEngine.ParticleSystem.PlaybackState;
using Unity.Mathematics;
using GameNetcodeStuff;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;
        public static bool firstTime = true;

        [HarmonyPrefix]
        [HarmonyPatch("SpawnEnemyFromVent")]
        public static bool SpawnEnemyFromVentPreFix(EnemyVent vent)
        {
            logger.LogDebug(vent.enemyTypeIndex);
            logger.LogDebug(vent.enemyType);

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (vent.enemyType.enemyName == "SCP-956")
                {
                    // Getting random scrap spawn

                    List<RandomScrapSpawn> list = UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>().Where(x => x.spawnUsed == false).ToList();
                    int index = UnityEngine.Random.Range(0, list.Count);
                    RandomScrapSpawn randomScrapSpawn = list[index];
                    Vector3 pos = randomScrapSpawn.transform.position;
                    if (randomScrapSpawn.spawnedItemsCopyPosition)
                    {
                        list.RemoveAt(index);
                    }
                    else
                    {
                        pos = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(randomScrapSpawn.transform.position, randomScrapSpawn.itemSpawnRange, RoundManager.Instance.navHit);
                    }

                    // Spawning

                    logger.LogDebug("Spawning");
                    RoundManager.Instance.SpawnEnemyOnServer(pos + Vector3.up * 0.5f, UnityEngine.Random.Range(0f, 360f), vent.enemyTypeIndex);
                    Debug.Log("Spawned enemy from vent");
                    vent.OpenVentClientRpc();
                    vent.occupied = false;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("DespawnPropsAtEndOfRound")]
        private static void DespawnPropsAtEndOfRoundPatch()
        {
            firstTime = true;
            try
            {
                logger.LogDebug("In DespawnPropsAtEndOfRoundPatch");
                PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
                PlayerControllerBPatch.playerFrozen = false;
                IngamePlayerSettings.Instance.playerInput.ActivateInput();
                StartOfRound.Instance.localPlayerController.disableLookInput = false;

                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    NetworkHandler.Instance.FrozenPlayers.Clear();
                }

                if (config956Behavior.Value != 3)
                {
                    PlayerAge = (int)UnityEngine.Random.Range(18, configMaxAge.Value);
                    NetworkHandler.Instance.ChangePlayerSizeServerRpc(StartOfRound.Instance.localPlayerController.actualClientId, 1f);
                }
            }
            catch (Exception)
            {
                logger.LogError("Error in DespawnPropsAtEndOfRound");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("SpawnInsideEnemiesFromVentsIfReady")]
        private static void SpawnInsideEnemiesFromVentsIfReadyPatch()
        {
            if (PlayerAge < 12 && firstTime)
            {
                NetworkHandler.Instance.SpawnPinataServerRpc(); // TODO: Needs testing
                firstTime = false;
            }
        }
    }
}