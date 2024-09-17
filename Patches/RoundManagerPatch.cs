using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LethalLib.Modules.Enemies;
using static SCP956.Plugin;
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
        private static ManualLogSource logger = Plugin.LoggerInstance;
        private static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        private static bool firstTime = true;

        [HarmonyPrefix]
        [HarmonyPatch("SpawnEnemyFromVent")]
        public static bool SpawnEnemyFromVentPreFix(EnemyVent vent) // TODO: Scrap this? May cause errors with other mods?
        {
            try // TODO: Temp fix until it's fixed
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
                            pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(randomScrapSpawn.transform.position, randomScrapSpawn.itemSpawnRange, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom); // TODO: Test this
                        }

                        // Spawning

                        logger.LogDebug("Spawning");
                        RoundManager.Instance.SpawnEnemyOnServer(pos, UnityEngine.Random.Range(0f, 360f), vent.enemyTypeIndex);
                        Debug.Log("Spawned pinata from vent");
                        vent.OpenVentClientRpc();
                        vent.occupied = false;
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError("Error with spawning pinata from vent: " + e);
                return true;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("DespawnPropsAtEndOfRound")]
        private static void DespawnPropsAtEndOfRoundPostfix()
        {
            logger.LogDebug("In DespawnPropsAtEndOfRoundPatch");
            ResetConditions(endOfRound: true);
        }

        [HarmonyPostfix]
        [HarmonyPatch("SpawnInsideEnemiesFromVentsIfReady")]
        private static void SpawnInsideEnemiesFromVentsIfReadyPostfix()
        {
            try // TODO: Temp fix until it's fixed
            {
                if (IsYoung && configEnablePinata.Value && firstTime)
                {
                    if (RoundManager.Instance.SpawnedEnemies.OfType<SCP956AI>().FirstOrDefault() == null)
                    {
                        NetworkHandler.Instance.SpawnPinataServerRpc();
                        firstTime = false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError("Error when spawning pinata from vent: " + e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(RoundManager.GenerateNewFloor))]
        private static void GenerateNewFloorPostfix()
        {
            logger.LogDebug("In GenerateNewFloorPostfix");
            firstTime = true;
            ResetConditions();

            // Setting player size

            if (IsYoung)
            {
                NetworkHandler.Instance.ChangePlayerSizeServerRpc(StartOfRound.Instance.localPlayerController.actualClientId, 0.7f);
                logger.LogDebug("Changed player size");
            }

            logger.LogInfo($"{StartOfRound.Instance.localPlayerController.playerUsername}'s age is {PlayerAge}");
        }
    }
}