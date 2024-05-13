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
            SCP956.PlayerBirthday = false;

            if (firstDay)
            {
                if (SCP956.config956Behavior.Value == 4)
                {
                    SCP956.PlayerAge = SCP956.PluginInstance.random.Next(5, 20);
                }
                else
                {
                    SCP956.PlayerAge = SCP956.PluginInstance.random.Next(20, 50);
                }

                firstDay = false;
            }

            if (SCP956.config956Behavior.Value == 3)
            {
                if (SCP956.PluginInstance.random.Next(0, 100) < 50)
                {
                    SCP956.PlayerBirthday = true;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("firstDayAnimation")]
        public static void firstDayAnimationPatch()
        {
            logger.LogDebug("In firstDayAnimationPatch");
            firstDay = true;
        }
    }
}
