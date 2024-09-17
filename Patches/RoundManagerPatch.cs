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

        [HarmonyPostfix]
        [HarmonyPatch("DespawnPropsAtEndOfRound")]
        private static void DespawnPropsAtEndOfRoundPostfix() // TODO: Check if this is run for all clients
        {
            logger.LogDebug("In DespawnPropsAtEndOfRoundPatch");
            ResetConditions(endOfRound: true);
            firstTime = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("SpawnInsideEnemiesFromVentsIfReady")]
        private static void SpawnInsideEnemiesFromVentsIfReadyPostfix()
        {
            try
            {
                if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                {
                    if (!StartOfRound.Instance.inShipPhase && firstTime && SCP956AI.YoungPlayers.Count > 0 && configEnablePinata.Value)
                    {
                        if (RoundManager.Instance.SpawnedEnemies.OfType<SCP956AI>().FirstOrDefault() == null)
                        {
                            NetworkHandler.Instance.SpawnPinataServerRpc();
                            firstTime = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError("Error when spawning pinata from vent: " + e);
            }
        }
    }
}