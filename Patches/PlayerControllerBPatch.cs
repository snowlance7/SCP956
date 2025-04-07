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
using System.Collections;
using SCP956.Items;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private static ManualLogSource logger = LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        private static void UpdatePostfix(ref bool ___inTerminalMenu, ref Transform ___thisPlayerBody, ref float ___fallValue)
        {
            if (StatusEffectController.Instance.infiniteSprintSeconds > 0) { localPlayer.sprintMeter = StatusEffectController.Instance.freezeSprintMeter; }
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
            if (Plugin.localPlayerIsYoung)
            {
                ___deadBody.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
        private static void DamagePlayerPrefix(ref int damageNumber, ref CauseOfDeath causeOfDeath)
        {
            try
            {
                if (StatusEffectController.Instance.damageReductionSeconds > 0)
                {
                    LogIfDebug("Applying " + StatusEffectController.Instance.damageReductionPercent + "% damage reduction");
                    float reductionPercent = StatusEffectController.Instance.damageReductionPercent / 100.0f;
                    int reductionAmount = Convert.ToInt32(damageNumber * reductionPercent);
                    int damageAfterReduction = damageNumber - reductionAmount;
                    LogIfDebug($"Initial damage: {damageNumber}, Damage reduction: {reductionAmount}, damage after reduction: {damageAfterReduction}");
                    damageNumber = damageAfterReduction;
                }
                if (StatusEffectController.Instance.bulletProofMultiplier != 0 && causeOfDeath == CauseOfDeath.Gunshots)
                {
                    float reductionPercent = StatusEffectController.Instance.bulletProofMultiplier * .10f;
                    int reductionAmount = (int)(damageNumber * reductionPercent);
                    int damageAfterReduction = damageNumber - reductionAmount;
                    LogIfDebug($"Initial damage: {damageNumber}, Damage reduction: {reductionAmount}, damage after reduction: {damageAfterReduction}");
                    damageNumber = damageAfterReduction;
                }
            }
            catch (Exception e)
            {
                logger.LogError("Error in DamagePlayerPrefix: " + e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
        private static void KillPlayerPostfix(PlayerControllerB __instance)
        {
            if (__instance == localPlayer)
            {
                ResetConditions();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PlayerControllerB.BeginGrabObject))]
        private static bool BeginGrabObjectPrefix()
        {
            try
            {
                if (SCP330Behavior.noHands)
                {
                    HUDManager.Instance.DisplayTip("Cant grab item", "You dont have hands to grab with!", true);
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.LogError("Error in BeginGrabObjectPrefix: " + e);
                return true;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        private static void ConnectClientToPlayerObjectPostfix()
        {
            LogIfDebug("Getting age from server");
            NetworkHandler.Instance.GetAgeServerRpc(localPlayer.actualClientId);
        }
    }
}