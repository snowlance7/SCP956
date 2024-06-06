using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956
{
    internal class SCP330Behavior : PhysicsProp
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        private PlayerControllerB localPlayer
        {
            get
            {
                return StartOfRound.Instance.localPlayerController;
            }
        }

        public static int candyTaken = 0;
        public static bool noHands = false;

        public override void InteractItem()
        {
            //candy.spawnPrefab.gameObject.GetComponent<ScanNodeProperties>().headerText = "Candy"; // TODO: Do something like this when giving the player candy, or use getcomponentinparent
            logger.LogDebug("Interacting with SCP-330");

            if (candyTaken >= 2)
            {
                localPlayer.DamagePlayer(10);
                HUDManager.Instance.UpdateHealthUI(localPlayer.health, true);
                localPlayer.MakeCriticallyInjured(true); // TODO: Test this
                localPlayer.DropAllHeldItemsAndSync();
                noHands = true;

                StatusEffectController.Instance.DamagePlayerOverTime(5, 2, true);

                return;
            }

            List<Item> candies = StartOfRound.Instance.allItemsList.itemsList.Where(x => CandyNames.Contains(x.itemName)).ToList();
            logger.LogDebug($"Candy count: {candies.Count}");
            Item candy = candies[UnityEngine.Random.Range(0, candies.Count)];
            logger.LogDebug("Got Candy");
            int scrapValue = (int)UnityEngine.Random.Range(config9561MinValue.Value, config9561MaxValue.Value * RoundManager.Instance.scrapValueMultiplier);
            logger.LogDebug("Got scrapValue");

            NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, candy.itemName, scrapValue, transform.position, Quaternion.identity, false, true);
            logger.LogDebug("Spawned candy");
            candyTaken++;
            logger.LogDebug("Candy taken: " + candyTaken);
        }
    }
}
