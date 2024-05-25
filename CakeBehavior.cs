using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SCP956.SCP956;

namespace SCP956
{
    internal class CakeBehavior : PhysicsProp
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        internal static void GrabObject(GrabbableObject grabbableItem, PlayerControllerB player)
        {
            player.carryWeight += Mathf.Clamp(grabbableItem.itemProperties.weight - 1f, 0f, 10f);
            player.GrabObjectServerRpc(grabbableItem.NetworkObject);
            grabbableItem.parentObject = player.localItemHolder;
            grabbableItem.GrabItemOnClient();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                HUDManager.Instance.UIAudio.PlayOneShot(CandleBlowsfx, 1f);
                SCP956.PlayerAge = 11;

                NetworkHandler.Instance.ShrinkPlayer(StartOfRound.Instance.localPlayerController.actualClientId);

                // Spawn cake somewhere else

                if (!StartOfRound.Instance.inShipPhase)
                {
                    List<RandomScrapSpawn> list = UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>().Where(x => x.spawnUsed == false).ToList();
                    int index = UnityEngine.Random.Range(0, list.Count);
                    RandomScrapSpawn randomScrapSpawn = list[index];
                    UnityEngine.Vector3 pos = randomScrapSpawn.transform.position;
                    if (randomScrapSpawn.spawnedItemsCopyPosition)
                    {
                        list.RemoveAt(index);
                    }
                    else
                    {
                        pos = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(randomScrapSpawn.transform.position, randomScrapSpawn.itemSpawnRange, RoundManager.Instance.navHit);
                    }

                    int newScrapValue = GetComponent<GrabbableObject>().scrapValue + 50;

                    GameObject obj = UnityEngine.Object.Instantiate(itemProperties.spawnPrefab, pos + UnityEngine.Vector3.up * 0.5f, UnityEngine.Quaternion.identity, StartOfRound.Instance.propsContainer);
                    obj.GetComponent<GrabbableObject>().fallTime = 0f;
                    obj.GetComponent<GrabbableObject>().SetScrapValue(newScrapValue);
                    obj.GetComponent<NetworkObject>().Spawn();
                    obj.GetComponent<AudioSource>().PlayOneShot(CakeAppearsfx, 1f);
                }

                // Blow out the candles // TODO: Doesnt work for other clients...

                PlayerControllerB tempPlayer = playerHeldBy;
                int newScrapValue2 = scrapValue / 2;
                playerHeldBy.DespawnHeldObject();
                Item CakeBlown = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "CakeBlown").FirstOrDefault();
                GameObject obj2 = UnityEngine.Object.Instantiate(CakeBlown.spawnPrefab);
                GrabbableObject cakeBlownGrabbable = obj2.GetComponent<GrabbableObject>();
                obj2.GetComponent<NetworkObject>().Spawn();
                GrabObject(cakeBlownGrabbable, tempPlayer);
                cakeBlownGrabbable.SetScrapValue(newScrapValue2);

                // Spawn SCP-956 if not already spawned
                if (!StartOfRound.Instance.inShipPhase)
                {
                    EnemyAI scp = RoundManager.Instance.SpawnedEnemies.Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault();
                    
                    if (scp == null)
                    {
                        SpawnableEnemyWithRarity enemy = RoundManager.Instance.currentLevel.Enemies.Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault();
                        int index = RoundManager.Instance.currentLevel.Enemies.IndexOf(enemy);

                        logger.LogDebug("Spawning SCP-956");
                        List<EnemyVent> unoccupiedVents = RoundManager.Instance.allEnemyVents.Where(x => x.occupied == false).ToList();
                        EnemyVent vent = unoccupiedVents[UnityEngine.Random.Range(0, unoccupiedVents.Count)];
                        vent.enemyTypeIndex = index;
                        vent.enemyType = enemy.enemyType;
                        RoundManager.Instance.SpawnEnemyFromVent(vent);
                    }
                }
            }
        }
    }
}
// TODO: Add tooltips in hud for blowing out candles
// TODO: Add more candles to the cake so it equals 11
// TODO: Make the pitch of the player go up when you blow out the candles