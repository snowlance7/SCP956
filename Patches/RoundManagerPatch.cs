using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LethalLib.Modules.Enemies;
using static SCP956.SCP956;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;
        public static bool firstTime = true;

        [HarmonyPrefix]
        [HarmonyPatch("SpawnEnemyFromVent")]
        public static bool SpawnEnemyFromVentPostFix(EnemyVent vent)
        {
            if (RoundManager.Instance.IsHost)
            {
                if (vent.enemyType.enemyName == "SCP-956")
                {
                    Vector3 position = RoundManager.Instance.GetRandomNavMeshPositionInRadius(vent.floorNode.position, 5); // TODO: TEST THIS figure out radius
                    RoundManager.Instance.SpawnEnemyOnServer(position, UnityEngine.Random.Range(0f, 360f), vent.enemyTypeIndex);
                    Debug.Log("Spawned enemy from vent");
                    vent.OpenVentClientRpc();
                    vent.occupied = false;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GenerateNewFloor")]
        public static void StartPatch()
        {
            logger.LogDebug("In GenerateNewFloorPatch");

            //SCP956.PluginInstance.random = new System.Random(StartOfRound.Instance.randomMapSeed);

            if (firstTime)
            {
                if (SCP956.config956Behavior.Value == 3)
                {
                    if (configMaxAge.Value < 5) { configMaxAge.Value = 5; }
                    //SCP956.PlayerAge = SCP956.PluginInstance.random.Next(5, SCP956.configMaxAge.Value);
                    PlayerAge = (int)UnityEngine.Random.Range(5, configMaxAge.Value);

                    if (PlayerAge < 12)
                    {
                        NetworkHandler.clientEventShrinkPlayer.InvokeAllClients(true);
                    }
                }
                else
                {
                    if (configMaxAge.Value < 18) { configMaxAge.Value = 18; }
                    //PlayerAge = PluginInstance.random.Next(18, configMaxAge.Value);
                    PlayerAge = (int)UnityEngine.Random.Range(18, configMaxAge.Value);
                }

                logger.LogDebug($"Age is {PlayerAge}");
                firstTime = false;
            }
        }
    }
}