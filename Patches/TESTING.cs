/*using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static SCP956.Plugin;

namespace SCP956.Patches
{
    [HarmonyPatch]
    internal class TESTING : MonoBehaviour
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {

        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            string msg = __instance.chatTextField.text;
            string[] args = msg.Split(" ");
            LogIfDebug(msg);


            // Comment these out
            if (args[0] == "/setVoice") // TODO: Test this
            {
                //SoundManager.Instance.playerVoicePitchTargets[(int)((IntPtr)localPlayer.playerClientId)] = float.Parse(args[1]);
                //SoundManager.Instance.SetPlayerPitch(float.Parse(args[2]), unchecked((int)localPlayer.playerClientId));
                //StatusEffectController.Instance.pitchAdjust = float.Parse(args[1]);
                //SoundManager.Instance.SetPlayerPitch(float.Parse(args[1]), (int)localPlayer.actualClientId);
            }
        }

        public static List<SpawnableEnemyWithRarity> GetEnemies()
        {
            LogIfDebug("Getting enemies");
            List<SpawnableEnemyWithRarity> enemies = new List<SpawnableEnemyWithRarity>();
            enemies = GameObject.Find("Terminal")
                .GetComponentInChildren<Terminal>()
                .moonsCatalogueList
                .SelectMany(x => x.Enemies.Concat(x.DaytimeEnemies).Concat(x.OutsideEnemies))
                .Where(x => x != null && x.enemyType != null && x.enemyType.name != null)
                .GroupBy(x => x.enemyType.name, (k, v) => v.First())
                .ToList();

            LogIfDebug($"Enemy types: {enemies.Count}");
            return enemies;
        }
    }
}*/