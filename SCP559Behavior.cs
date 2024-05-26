using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SCP956.SCP956;

namespace SCP956
{
    internal class SCP559Behavior : PhysicsProp
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                if (PlayerAge == 11) { return; }
                HUDManager.Instance.UIAudio.PlayOneShot(CandleBlowsfx, 1f);
                SCP956.PlayerAge = 11;

                NetworkHandler.Instance.ChangePlayerSizeServerRpc(StartOfRound.Instance.localPlayerController.actualClientId, 0.8f);

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
                    logger.LogDebug("Spawning SCP-559");
                    NetworkHandler.Instance.SpawnItemServerRpc(itemProperties.itemName, newScrapValue, pos, Quaternion.identity, true);
                }

                // Blow out the candles

                PlayerControllerB tempPlayer = playerHeldBy;
                int newScrapValue2 = scrapValue / 2;
                playerHeldBy.DespawnHeldObject();
                Item Cake = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Cake").FirstOrDefault();
                logger.LogDebug("Spawning cake");
                NetworkHandler.Instance.SpawnItemServerRpc(Cake.itemName, newScrapValue2, tempPlayer.transform.position, Quaternion.identity);
                
                // Spawn SCP-956 if not already spawned
                if (!StartOfRound.Instance.inShipPhase)
                {
                    EnemyAI scp = RoundManager.Instance.SpawnedEnemies.Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault();
                    
                    if (scp == null)
                    {
                        NetworkHandler.Instance.SpawnPinataServerRpc();
                    }
                }
            }
        }
    }
}
// TODO: Add more candles to the cake so it equals 11
// TODO: Make the pitch of the player go up when you blow out the candles