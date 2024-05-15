using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Extras;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static SCP956.SCP956;
using LethalLib;
using static LethalLib.Modules.Enemies;
using static UnityEngine.VFX.VisualEffectControlTrackController;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch("PingScan_performed")]
        public static void PingScan_performedPostFix()
        {
            logger.LogDebug("In PingScan_performedPostFix");
            //RoundManager.Instance.SpawnEnemyOnServer(StartOfRound.Instance.localPlayerController.transform.position, 0f, 2);
            //RoundManager.Instance.SpawnEnemyGameObject(StartOfRound.Instance.localPlayerController.transform.position, 0f, -1, GetEnemies().Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault().enemyType);
        }
    }
}