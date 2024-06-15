using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956
{
    internal class SCP559Behavior : PhysicsProp
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;
        private static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public override void ItemActivate(bool used, bool buttonDown = true) // TODO: Find a way to remove the grab animation so blowing out the candles is instant
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                StatusEffectController.Instance.BlowOutCandles();

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

                    int newScrapValue = GetComponent<GrabbableObject>().scrapValue * 50;
                    logger.LogDebug("Spawning SCP-559");
                    NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, itemProperties.itemName, newScrapValue, pos, Quaternion.identity, true);
                }

                // Blow out the candles

                PlayerControllerB tempPlayer = playerHeldBy;
                int newScrapValue2 = scrapValue / 2;
                playerHeldBy.DespawnHeldObject();

                // Spawn cake
                Item Cake = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "Cake").FirstOrDefault();
                logger.LogDebug("Spawning cake");
                NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, Cake.itemName, newScrapValue2, tempPlayer.transform.position, Quaternion.identity, false, true);
                
                // Spawn SCP-956 if not already spawned
                if (!StartOfRound.Instance.inShipPhase && configEnablePinata.Value)
                {
                    EnemyAI scp = RoundManager.Instance.SpawnedEnemies.Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault();
                    
                    if (scp == null)
                    {
                        if (configSecretLab.Value && localPlayer.isInsideFactory) { NetworkHandler.Instance.SpawnPinataNearbyServerRpc(localPlayer.transform.position); }
                        else { NetworkHandler.Instance.SpawnPinataServerRpc(); }
                    }
                }
            }
        }
    }
}
// TODO: Make the pitch of the player go up when you blow out the candles