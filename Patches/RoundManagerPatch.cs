using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LethalLib.Modules.Enemies;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        [HarmonyPrefix]
        [HarmonyPatch("SpawnEnemyFromVent")]
        public static bool SpawnEnemyFromVentPostFix(EnemyVent vent)
        {
            if (RoundManager.Instance.IsHost)
            {
                if (vent.enemyType.enemyName == "SCP-956")
                {
                    Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadius(vent.floorNode.position, 10); // TODO: TEST THIS figure out radius
                    RoundManager.Instance.SpawnEnemyOnServer(position, UnityEngine.Random.Range(0f, 360f), vent.enemyTypeIndex);
                    Debug.Log("Spawned enemy from vent");
                    vent.OpenVentClientRpc();
                    vent.occupied = false;
                    return false;
                }
            }
            return true;
        }
    }
}