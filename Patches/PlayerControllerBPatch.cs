using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using static SCP956.Plugin;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private static float timeSinceLastCheck = 0f;
        private static bool warningStarted = false;
        public static bool playerFrozen = false;

        private static ManualLogSource logger = LoggerInstance;

        private static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        private static void UpdatePatch(ref bool ___inTerminalMenu, ref Transform ___thisPlayerBody, ref float ___fallValue)
        {
            if (___inTerminalMenu) // TODO: Test this
            {
                ___thisPlayerBody.position = new Vector3(___thisPlayerBody.position.x, 0.29f, ___thisPlayerBody.position.z);
                ___fallValue = 0f;
            }

            timeSinceLastCheck += Time.deltaTime;
            if (timeSinceLastCheck > 0.3f)
            {
                timeSinceLastCheck = 0f;

                if (playerFrozen) { return; }

                if (StartOfRound.Instance == null || localPlayer == null || !localPlayer.isPlayerControlled || localPlayer.isPlayerDead)
                {
                    if (playerFrozen) { playerFrozen = false; }
                    return;
                }

                if (StatusEffectController.Instance.infiniteSprintSeconds > 0) { localPlayer.sprintMeter = StatusEffectController.Instance.freezeSprintMeter; }

                AudioSource _audioSource = HUDManager.Instance.UIAudio;
                if (_audioSource == null) { logger.LogError("AudioSource is null"); return; }

                if (PlayerMeetsConditions(localPlayer))
                {
                    if (!warningStarted)
                    {
                        if (WarningSoundShortsfx == null || WarningSoundLongsfx == null) { logger.LogError("Warning sounds not set!"); return; }
                        if (config956Behavior.Value == 2 && IsPlayerHoldingCandy(localPlayer)) { _audioSource.clip = WarningSoundLongsfx; } else { _audioSource.clip = WarningSoundShortsfx; }
                        if (!configPlayWarningSound.Value) { _audioSource.volume = 0f; } else { _audioSource.volume = 1f; }
                        _audioSource.loop = false;
                        _audioSource.Play();

                        warningStarted = true;
                        logger.LogDebug("Warning started");
                    }

                    if (!_audioSource.isPlaying && warningStarted)
                    {
                        logger.LogDebug("Audio stopped");
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

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        private static void UpdatePrefix()
        {
            if (StatusEffectController.Instance.statusNegationSeconds > 0)
            {
                localPlayer.bleedingHeavily = false;
                localPlayer.criticallyInjured = false;
                localPlayer.isMovementHindered = 0;
                localPlayer.isExhausted = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerControllerB.SpawnDeadBody))]
        private static void SpawnDeadBodyPostfix(ref DeadBodyInfo ___deadBody)
        {
            if (Plugin.PlayerAge < 12)
            {
                ___deadBody.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
        private static void DamagePlayerPrefix(ref int damageNumber, ref CauseOfDeath causeOfDeath)
        {
            if (StatusEffectController.Instance.damageReductionSeconds > 0)
            {
                logger.LogDebug("Applying " + StatusEffectController.Instance.damageReductionPercent + "% damage reduction");
                float reductionPercent = StatusEffectController.Instance.damageReductionPercent / 100.0f;
                int reductionAmount = Convert.ToInt32(damageNumber * reductionPercent);
                int damageAfterReduction = damageNumber - reductionAmount;
                logger.LogDebug($"Initial damage: {damageNumber}, Damage reduction: {reductionAmount}, damage after reduction: {damageAfterReduction}");
                damageNumber = damageAfterReduction;
            }
            if (StatusEffectController.Instance.bulletProofMultiplier != 0 && causeOfDeath == CauseOfDeath.Gunshots)
            {
                float reductionPercent = StatusEffectController.Instance.bulletProofMultiplier * .10f;
                int reductionAmount = (int)(damageNumber * reductionPercent);
                int damageAfterReduction = damageNumber - reductionAmount;
                logger.LogDebug($"Initial damage: {damageNumber}, Damage reduction: {reductionAmount}, damage after reduction: {damageAfterReduction}");
                damageNumber = damageAfterReduction;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        private static void KillPlayerPostfix(PlayerControllerB __instance)
        {
            NetworkHandler.Instance.ChangePlayerSizeServerRpc(__instance.actualClientId, 1f);
            PlayerAge = PlayerOriginalAge;
            __instance.disableLookInput = false;
            IngamePlayerSettings.Instance.playerInput.ActivateInput();
            SCP330Behavior.noHands = false;
            SCP330Behavior.candyTaken = 0;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerControllerB.BeginGrabObject))]
        private static bool BeginGrabObjectPrefix()
        {
            if (SCP330Behavior.noHands)
            {
                HUDManager.Instance.DisplayTip("Cant grab item", "You dont have hands to grab with!");
                return false;
            }
            return true;
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