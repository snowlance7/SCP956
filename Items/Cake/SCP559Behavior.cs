using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956.Items.Cake
{
    internal class SCP559Behavior : PhysicsProp
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable 0649
        public AudioClip CandleBlowSFX = null!;
#pragma warning restore 0649

        public override void ItemActivate(bool used, bool buttonDown = true) // TODO: Find a way to remove the grab animation so blowing out the candles is instant
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                /*if (StartOfRound.Instance.inShipPhase)
                {
                    HUDManager.Instance.DisplayTip("", "You cant blow out the candles while in orbit");
                    return;
                }*/

                playerHeldBy.itemAudio.PlayOneShot(CandleBlowSFX, 1f);
                if (localPlayer == playerHeldBy && config559ReversesAgeReblow.Value && localPlayerIsYoung)
                {
                    ChangePlayerAge(PlayerOriginalAge);
                }
                else if (!localPlayerIsYoung) { ChangePlayerAge(11); }

                // Spawn cake somewhere else

                if (!StartOfRound.Instance.inShipPhase)
                {
                    List<RandomScrapSpawn> list = FindObjectsOfType<RandomScrapSpawn>().Where(x => x.spawnUsed == false).ToList();
                    int index = Random.Range(0, list.Count);
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

                    int newScrapValue = GetComponent<GrabbableObject>().scrapValue * 50;
                    logger.LogDebug("Spawning SCP-559");
                    NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, itemProperties.name, newScrapValue, pos, Quaternion.identity);
                }

                // Blow out the candles

                PlayerControllerB tempPlayer = playerHeldBy;
                int newScrapValue2 = scrapValue / 2;
                playerHeldBy.DespawnHeldObject();

                // Spawn cake
                Item Cake = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.name == "Cake559Item").FirstOrDefault();
                logger.LogDebug("Spawning cake");
                NetworkHandler.Instance.SpawnItemServerRpc(tempPlayer.actualClientId, Cake.name, newScrapValue2, tempPlayer.transform.position, Quaternion.identity, true);

                // Spawn SCP-956 if not already spawned
                if (!StartOfRound.Instance.inShipPhase && configEnablePinata.Value)
                {
                    EnemyAI scp = RoundManager.Instance.SpawnedEnemies.OfType<SCP956AI>().FirstOrDefault();

                    if (scp == null)
                    {
                        NetworkHandler.Instance.SpawnPinataServerRpc();
                    }
                }
            }
        }
    }
}
// TODO: Make the pitch of the player go up when you blow out the candles