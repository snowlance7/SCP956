using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;
        private static bool firstDay = true;

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        public static void StartPatch()
        {
            logger.LogDebug("In StartPatch");
            SCP956.PluginInstance.random = new Random(StartOfRound.Instance.randomMapSeed);
            PlayerControllerBPatch.playerFrozen = false;
            NetworkHandler.UnfortunatePlayers.Value = new List<ulong>();

            /*if (firstDay)
            {
                logger.LogDebug("In StartPatch first day");
                if (SCP956.config956Behavior.Value == 3)
                {
                    SCP956.PlayerAge = SCP956.PluginInstance.random.Next(5, SCP956.configMaxAge.Value);

                    if (SCP956.PlayerAge < 12)
                    {
                        NetworkHandler.clientEventShrinkPlayer.InvokeAllClients(true);
                    }
                }
                else
                {
                    SCP956.PlayerAge = SCP956.PluginInstance.random.Next(18, SCP956.configMaxAge.Value);
                }

                logger.LogDebug($"{StartOfRound.Instance.localPlayerController.playerUsername}'s age is {SCP956.PlayerAge}");
                firstDay = false;
            }*/
        }

        [HarmonyPostfix]
        [HarmonyPatch("firstDayAnimation")]
        public static void firstDayAnimationPatch()
        {
            logger.LogDebug("In firstDayAnimationPatch");
            firstDay = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("ReviveDeadPlayers")]
        public static void ReviveDeadPlayersPatch()
        {
            PlayerControllerBPatch.playerFrozen = false;
        }
    }
}
