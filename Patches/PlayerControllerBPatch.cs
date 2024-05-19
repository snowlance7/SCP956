using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using static SCP956.SCP956;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        private static float timeSinceLastCheck = 0f;
        private static bool warningStarted = false;
        public static bool playerFrozen = false;
        

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void UpdatePatch(PlayerControllerB __instance, ref bool ___inTerminalMenu, ref Transform ___thisPlayerBody, ref float ___fallValue)
        {
            if (playerFrozen)
            {
                StartOfRound.Instance.localPlayerUsingController = false;
                IngamePlayerSettings.Instance.playerInput.DeactivateInput();
                __instance.disableLookInput = true;
                return;
            }

            timeSinceLastCheck += Time.deltaTime;
            if (timeSinceLastCheck > 1f)
            {
                timeSinceLastCheck = 0f;

                if (PlayerMeetsConditions(__instance))
                {
                    if (!warningStarted)
                    {
                        __instance.statusEffectAudio.clip = WarningSoundsfx; // TODO: might need to change audio source later, might cause unexpected behavior
                        float pitch;
                        // TODO: This dont work
                        if (IsPlayerHoldingCandy(__instance) && SCP956AI.Behavior == 2) { pitch = WarningSoundsfx.length / configActivationTimeCandy.Value; } else { pitch = WarningSoundsfx.length / configActivationTime.Value; } // TODO: Make sure this works correctly
                        __instance.statusEffectAudio.pitch = pitch;
                        if (!configPlayWarningSound.Value) { __instance.movementAudio.volume = 0f; }
                        __instance.statusEffectAudio.Play();

                        warningStarted = true;
                    }

                    if (!__instance.movementAudio.isPlaying)
                    {
                        // Freeze player

                        playerFrozen = true;
                        warningStarted = false;
                        NetworkHandler.UnfortunatePlayers.Value.Add(__instance.actualClientId);
                    }
                }
            }
        }

        /*if (SCP956.PlayerAge < 12 && ___inTerminalMenu)
        {
            ___thisPlayerBody.position = new Vector3(___thisPlayerBody.position.x, ___thisPlayerBody.position.y + 0.7f, ___thisPlayerBody.position.z);
            ___fallValue = 0f;
        }*/

        [HarmonyPrefix]
        [HarmonyPatch("SpawnDeadBody")]
        private static void DeadPlayer(ref DeadBodyInfo ___deadBody)
        {
            if (SCP956.PlayerAge < 12)
            {
                ___deadBody.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }
        }
    }
}
