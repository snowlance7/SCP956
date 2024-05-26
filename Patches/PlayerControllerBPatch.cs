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
            if (___inTerminalMenu) // TODO: Test this
            {
                ___thisPlayerBody.position = new Vector3(___thisPlayerBody.position.x, 0.29f, ___thisPlayerBody.position.z);
                ___fallValue = 0f;
            }

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            if (playerFrozen || StartOfRound.Instance == null || StartOfRound.Instance.localPlayerController == null || !localPlayer.isPlayerControlled) { return; }

            timeSinceLastCheck += Time.deltaTime;
            if (timeSinceLastCheck > 0.3f)
            {
                timeSinceLastCheck = 0f;

                AudioSource _audioSource = HUDManager.Instance.UIAudio;

                if (PlayerMeetsConditions(localPlayer))
                {
                    logger.LogDebug($"Warning started: {warningStarted}");
                    if (!warningStarted)
                    {
                        if (WarningSoundShortsfx == null || WarningSoundLongsfx == null) { logger.LogError("Warning sounds not set!"); return; }
                        if (config956Behavior.Value == 2 && IsPlayerHoldingCandy(localPlayer)) { _audioSource.clip = WarningSoundLongsfx; } else { _audioSource.clip = WarningSoundShortsfx; }
                        if (!configPlayWarningSound.Value) { _audioSource.volume = 0f; } else { _audioSource.volume = 1f; }
                        _audioSource.loop = false;
                        _audioSource.Play();

                        warningStarted = true;
                    }

                    if (!_audioSource.isPlaying)
                    {
                        logger.LogDebug("audio stopped");
                        // Freeze localPlayer
                        playerFrozen = true;
                        warningStarted = false;
                        NetworkHandler.Instance.AddToFrozenPlayersListServerRpc(localPlayer.actualClientId);

                        IngamePlayerSettings.Instance.playerInput.DeactivateInput();
                        localPlayer.disableLookInput = true;
                        if (localPlayer.currentlyHeldObject != null) { localPlayer.DropItemAheadOfPlayer(); }
                    }
                }
                else if (warningStarted)
                {
                    logger.LogDebug("Warning ended");
                    warningStarted = false;
                    _audioSource.Stop();
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("SpawnDeadBody")]
        private static void SpawnDeadBodyPostfix(ref DeadBodyInfo ___deadBody)
        {
            if (SCP956.PlayerAge < 12)
            {
                ___deadBody.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }
        }

        public static bool IsPlayerHoldingCandy(PlayerControllerB player)
        {
            foreach (GrabbableObject item in player.ItemSlots)
            {
                if (item == null) { continue; }
                if (CandyNames.Contains(item.itemProperties.itemName))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool PlayerMeetsConditions(PlayerControllerB player)
        {
            if (PlayerAge < 12 || config956Behavior.Value == 4 || (config956Behavior.Value == 2 && IsPlayerHoldingCandy(player)))
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