using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void UpdatePatch(ref bool ___inTerminalMenu, ref Transform ___thisPlayerBody, ref float ___fallValue)
        {
            if (SCP956.PlayerAge < 12 && ___inTerminalMenu)
            {
                ___thisPlayerBody.position = new Vector3(___thisPlayerBody.position.x, ___thisPlayerBody.position.y + 0.7f, ___thisPlayerBody.position.z);
                ___fallValue = 0f;
            }
        }

        [HarmonyPatch("SpawnDeadBody")]
        [HarmonyPrefix]
        private static void DeadPlayer(ref DeadBodyInfo ___deadBody)
        {
            if (SCP956.PlayerAge < 12)
            {
                ___deadBody.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }
        }
    }
}
