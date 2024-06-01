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
    // TODO: Figure out how to determine if scrap spawn is on a table or on the floor, USE RAYCASTING
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        private static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        [HarmonyPostfix]
        [HarmonyPatch("PingScan_performed")]
        public static void PingScan_performedPostFix()
        {
            //StartOfRound.Instance.Debug_EnableTestRoomServerRpc(true);
            List<RandomScrapSpawn> list = UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>().Where(x => x.spawnUsed == false).ToList();
            RandomScrapSpawn randomScrapSpawn = list[0];
            /*int index = UnityEngine.Random.Range(0, list.Count);
            RandomScrapSpawn randomScrapSpawn = list[index];
            Vector3 pos = randomScrapSpawn.transform.position;
            if (randomScrapSpawn.spawnedItemsCopyPosition)
            {
                list.RemoveAt(index);
            }
            else
            {
                pos = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(randomScrapSpawn.transform.position, randomScrapSpawn.itemSpawnRange, RoundManager.Instance.navHit);
            }

            Item Cake = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Cake").FirstOrDefault();
            logger.LogDebug("Spawning cake");
            NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, Cake.itemName, 1000, pos, Quaternion.identity, true, false);*/





            //localPlayer.health = 5;
            //StatusEffectController.Instance.HealPlayer(10, true);
            //StatusEffectController.Instance.HealthRegen(5, 15);
            //StatusEffectController.Instance.StatusNegation(10, true);
            //StatusEffectController.Instance.DamageReduction(30, 20, true, true);
            //StatusEffectController.Instance.InfiniteSprint(10, true);
            //StatusEffectController.Instance.IncreasedMovementSpeed(10, 50);



            //logger.LogDebug("In PingScan_performedPostFix");
            //logger.LogDebug($"isMovementHindered: {localPlayer.isMovementHindered} movementHinderedPrev: {localPlayer.movementHinderedPrev} hinderedMultiplier: {localPlayer.hinderedMultiplier}");
            //StatusEffectController.Instance.HealthRegen(10, 15);
            //StatusEffectController.Instance.HealthRegen(20, 50);
            //logger.LogDebug(StartOfRound.Instance.localPlayerController.thisPlayerBody.position);
            //logger.LogDebug(PlayerAge);


            /*logger.LogDebug("In PingScan_performedPostFix");
            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
            {
                logger.LogDebug(enemy.enemyType.enemyName);
            }*/
            //RoundManager.Instance.SpawnEnemyOnServer(StartOfRound.Instance.localPlayerController.transform.position, 0f, 2);
            //RoundManager.Instance.SpawnEnemyGameObject(StartOfRound.Instance.localPlayerController.transform.position, 0f, -1, GetEnemies().Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault().enemyType);
            /*List<Item> candies = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "CandyRed" || x.itemName == "CandyPink" || x.itemName == "CandyYellow" || x.itemName == "CandyPurple").ToList();

            for (int i = 0; i < 10; i++)
            {
                Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInRadius(StartOfRound.Instance.localPlayerController.transform.position, 1, RoundManager.Instance.navHit);
                GameObject obj = UnityEngine.Object.Instantiate(candies[UnityEngine.Random.Range(0, 4)].spawnPrefab, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), StartOfRound.Instance.propsContainer);
                int newScrapValue = (int)UnityEngine.Random.Range(config9561MinValue.Value, config9561MaxValue.Value * RoundManager.Instance.scrapValueMultiplier);
                obj.GetComponent<GrabbableObject>().SetScrapValue(newScrapValue);
                obj.GetComponent<GrabbableObject>().fallTime = 0f;
                obj.GetComponent<NetworkObject>().Spawn();
            }

            Vector3 pos2 = RoundManager.Instance.GetRandomNavMeshPositionInRadius(StartOfRound.Instance.localPlayerController.transform.position, 1, RoundManager.Instance.navHit);
            GameObject obj2 = UnityEngine.Object.Instantiate(StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "CakeBlown").FirstOrDefault().spawnPrefab, pos2, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), StartOfRound.Instance.propsContainer);
            int scrapValue2 = (int)UnityEngine.Random.Range(config9561MinValue.Value, config9561MaxValue.Value * RoundManager.Instance.scrapValueMultiplier);
            obj2.GetComponent<GrabbableObject>().SetScrapValue(scrapValue2);
            obj2.GetComponent<GrabbableObject>().fallTime = 0f;
            obj2.GetComponent<NetworkObject>().Spawn();*/
        }
    }
}