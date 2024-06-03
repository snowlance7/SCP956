using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Netcode;
using static SCP956.SCP956;
using UnityEngine;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        public static void AwakePatch()
        {
            if (SCP956.config956Behavior.Value == 3)
            {
                if (configMaxAge.Value < 5) { configMaxAge.Value = 5; }
                PlayerAge = (int)UnityEngine.Random.Range(5, configMaxAge.Value);

                if (PlayerAge < 12)
                {
                    NetworkHandler.Instance.ChangePlayerSizeServerRpc(StartOfRound.Instance.localPlayerController.actualClientId, 0.8f);
                }
            }
            else
            {
                if (configMaxAge.Value < 18) { configMaxAge.Value = 18; }
                PlayerAge = (int)UnityEngine.Random.Range(18, configMaxAge.Value);
            }

            logger.LogDebug($"Age is {PlayerAge}");
        }
    }
}
