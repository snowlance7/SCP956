using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using static SCP956.SCP956;
using Unity.Netcode;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private static float timeSinceLastCheck = 0f;
        private static bool warningStarted = false;
        public static bool playerFrozen = false;

        private static ManualLogSource logger = LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void UpdatePatch(ref bool ___inTerminalMenu, ref Transform ___thisPlayerBody, ref float ___fallValue)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) { return; } // TODO: Temporary testing
            if (playerFrozen || StartOfRound.Instance == null || StartOfRound.Instance.localPlayerController == null) { return; }
            PlayerControllerB __instance = StartOfRound.Instance.localPlayerController;
            if (!__instance.isPlayerControlled) { return; }

            if (SCP956.PlayerAge < 12 && ___inTerminalMenu)
            {
                ___thisPlayerBody.position = new Vector3(___thisPlayerBody.position.x, ___thisPlayerBody.position.y + 0.7f, ___thisPlayerBody.position.z);
                ___fallValue = 0f;
            }

            timeSinceLastCheck += Time.deltaTime;
            if (timeSinceLastCheck > 0.3f)
            {
                timeSinceLastCheck = 0f;
                if (PlayerMeetsConditions(__instance))
                {
                    if (!warningStarted)
                    {
                        __instance.movementAudio.clip = WarningSoundsfx; // TODO: might need to change audio source later, might cause unexpected behavior
                        float pitch;
                        if (config956Behavior.Value == 2 && IsPlayerHoldingCandy(__instance)) { pitch = WarningSoundsfx.length / configActivationTimeCandy.Value; } else { pitch = WarningSoundsfx.length / configActivationTime.Value; }
                        __instance.movementAudio.pitch = pitch; // TODO: This doesnt extend the audio clip like expected
                        if (!configPlayWarningSound.Value) { __instance.movementAudio.volume = 0f; } else { __instance.movementAudio.volume = 1f; }
                        __instance.movementAudio.Play();

                        warningStarted = true;
                    }

                    if (!__instance.movementAudio.isPlaying)
                    {
                        // Freeze player
                        playerFrozen = true;
                        warningStarted = false;
                        NetworkHandler.Instance.AddToFrozenPlayersListServerRpc(__instance.actualClientId);

                        //IngamePlayerSettings.Instance.playerInput.DeactivateInput(); // TODO: Testing change these back later
                        //__instance.disableLookInput = true;
                        //if (__instance.currentlyHeldObject != null) { __instance.DropItemAheadOfPlayer(); }
                    }
                }
                else if (warningStarted)
                {
                    warningStarted = false;
                    __instance.movementAudio.Stop();
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("SpawnDeadBody")]
        private static void SpawnDeadBodyPatch(ref DeadBodyInfo ___deadBody)
        {
            if (SCP956.PlayerAge < 12)
            {
                ___deadBody.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }
        }

        public static bool IsPlayerHoldingCandy(PlayerControllerB player)
        {
            logger.LogDebug(player.ItemSlots);
            logger.LogDebug(player.ItemSlots.Count());
            foreach (GrabbableObject item in player.ItemSlots)
            {
                if (item == null) { continue; }
                if (item.itemProperties.itemName == "CandyRed" || item.itemProperties.itemName == "CandyPink" || item.itemProperties.itemName == "CandyYellow" || item.itemProperties.itemName == "CandyPurple")
                {
                    return true;
                }
            }
            return false;
        }

        public static bool PlayerMeetsConditions(PlayerControllerB player)
        {
            if (PlayerAge < 12 || (config956Behavior.Value == 4) || (config956Behavior.Value == 2 && IsPlayerHoldingCandy(player)))
            {
                foreach (EnemyAI scp in RoundManager.Instance.SpawnedEnemies.Where(x => x.enemyType.enemyName == "SCP-956"))
                {
                    if (scp.PlayerIsTargetable(player) && Vector3.Distance(scp.transform.position, player.transform.position) <= config956Radius.Value)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
