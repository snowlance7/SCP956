using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
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

            if (SCP956.config956Behavior.Value == 3) // TODO: Make sure this works, causes null reference exception, add logging
            {
                if (configMaxAge.Value < 5) { configMaxAge.Value = 5; } // TODO: Make sure this works
                SCP956.PlayerAge = SCP956.PluginInstance.random.Next(5, SCP956.configMaxAge.Value);

                if (SCP956.PlayerAge < 12)
                {
                    NetworkHandler.clientEventShrinkPlayer.InvokeAllClients(true);
                }
            }
            else
            {
                if (configMaxAge.Value < 18) { configMaxAge.Value = 18; }
                SCP956.PlayerAge = SCP956.PluginInstance.random.Next(18, SCP956.configMaxAge.Value);
            }

            logger.LogDebug($"{StartOfRound.Instance.localPlayerController.playerUsername}'s age is {SCP956.PlayerAge}");
        }

        [HarmonyPostfix]
        [HarmonyPatch("ReviveDeadPlayers")]
        public static void ReviveDeadPlayersPatch()
        {
            PlayerControllerBPatch.playerFrozen = false;
            NetworkHandler.UnfortunatePlayers.Value = new List<ulong>();
        }
    }
}
