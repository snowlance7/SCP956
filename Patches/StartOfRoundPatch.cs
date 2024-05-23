using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Netcode;
using static SCP956.SCP956;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch("firstDayAnimation")]
        public static void firstDayAnimationPatch()
        {
            logger.LogDebug("In firstDayAnimationPatch");
            RoundManagerPatch.firstTime = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("ReviveDeadPlayers")]
        public static void ReviveDeadPlayersPatch()
        {
            logger.LogDebug("In ReviveDeadPlayersPatch");
            PlayerControllerBPatch.playerFrozen = false;
            IngamePlayerSettings.Instance.playerInput.ActivateInput();
            StartOfRound.Instance.localPlayerController.disableLookInput = false;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                NetworkHandler.Instance.FrozenPlayers.Clear();
            }
        }
    }
}
