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

        [HarmonyPrefix]
        [HarmonyPatch("SpawnEnemyFromVent")]
        public static bool SpawnEnemyFromVentPreFix(EnemyVent vent)
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
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return true;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("DespawnPropsAtEndOfRound")]
        private static void DespawnPropsAtEndOfRoundPatch()
        {
            try // TODO: Temp fix until it's fixed
            {
                logger.LogDebug("In DespawnPropsAtEndOfRoundPatch");
                PlayerControllerBPatch.playerFrozen = false;
                SCP330Behavior.candyTaken = 0;
                if (!IngamePlayerSettings.Instance.playerInput.m_InputActive)
                {
                    IngamePlayerSettings.Instance.playerInput.ActivateInput();
                    localPlayer.disableLookInput = false;
                }
                StatusEffectController.Instance.bulletProofMultiplier = 0;
                SCP330Behavior.noHands = false;

                if (PlayerAge != PlayerOriginalAge)
                {
                    PlayerAge = PlayerOriginalAge;
                    HUDManager.Instance.UIAudio.PlayOneShot(CakeDisappearsfx, 1f);
                }
                if (PlayerAge >= 12)
                {
                    NetworkHandler.Instance.ChangePlayerSizeServerRpc(localPlayer.actualClientId, 1f);
                }

                if ((NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) && NetworkHandler.Instance.FrozenPlayers != null)
                {
                    NetworkHandler.Instance.FrozenPlayers.Clear();
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
            try
            {
                if (PlayerAge < 12 && configEnablePinata.Value)
                {
                    if (RoundManager.Instance.SpawnedEnemies.Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault() == null)
                    {
                        NetworkHandler.Instance.SpawnPinataServerRpc();
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }
    }
}