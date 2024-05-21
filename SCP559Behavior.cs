using BepInEx.Logging;
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
                playerHeldBy.movementAudio.PlayOneShot(CandleBlowsfx, 1f);
                SCP956.PlayerAge = 10;

                NetworkHandler.clientEventShrinkPlayer.InvokeAllClients(true);


                // Spawn cake somewhere else
                /*List<RandomScrapSpawn> list = (from s in UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>()
                                               where !s.spawnUsed
                                               select s).ToList();
                int index = PluginInstance.random.Next(0, list.Count);
                RandomScrapSpawn randomScrapSpawn = list[index];
                UnityEngine.Vector3 pos = randomScrapSpawn.transform.position;
                if (randomScrapSpawn.spawnedItemsCopyPosition)
                {
                    list.RemoveAt(index);
                }
                else
                {
                    pos = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(randomScrapSpawn.transform.position, randomScrapSpawn.itemSpawnRange, RoundManager.Instance.navHit); // TODO: Use for candy spawn
                }*/

                Vector3 pos = playerHeldBy.transform.position;
                int scrapValue = GetComponent<GrabbableObject>().scrapValue + 50;

                GameObject obj = UnityEngine.Object.Instantiate(itemProperties.spawnPrefab, pos + UnityEngine.Vector3.up * 0.5f, UnityEngine.Quaternion.identity, StartOfRound.Instance.propsContainer);
                playerHeldBy.DespawnHeldObject(); // TODO: Instead of this, just blow out the candles and reduce the value by half
                obj.GetComponent<GrabbableObject>().fallTime = 0f;
                obj.GetComponent<GrabbableObject>().SetScrapValue(scrapValue);
                obj.GetComponent<NetworkObject>().Spawn();
                obj.GetComponent<AudioSource>().PlayOneShot(CakeAppearsfx, 1f);
            }
        }
    }
}
// TODO: Add tooltips in hud for blowing out candles
// TODO: Add more candles to the cake so it equals 10
// TODO: Make it so when you blow out the candles, the fire on the candles go out
// TODO: Make the pitch go up when you blow out the candles