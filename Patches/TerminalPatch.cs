using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Netcode;
using static SCP956.Plugin;
using UnityEngine;
using System.Drawing;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatch
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Terminal.BeginUsingTerminal))]
        public static void BeginUsingTerminalPostfix()
        {
            if (localPlayerIsYoung)
            {
                localPlayer.thisPlayerBody.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Terminal.QuitTerminal))]
        public static void QuitTerminalPostfix()
        {
            if (localPlayerIsYoung)
            {
                localPlayer.thisPlayerBody.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            }
        }
    }
}
