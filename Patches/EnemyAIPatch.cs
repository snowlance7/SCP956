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
using Unity.Netcode;
using GameNetcodeStuff;
using static UnityEngine.ParticleSystem.PlaybackState;
using Unity.Mathematics;
using UnityEngine;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        [HarmonyPrefix]
        [HarmonyPatch("PlayerIsTargetable")]
        public static bool PlayerIsTargetablePrefix()
        {
            if (StatusEffectController.Instance.statusNegationActive)
            {
                return false;
            }
            return true;
        }
    }
}