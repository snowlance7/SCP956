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
            logger.LogDebug("Interacting with SCP-330");

            if (candyTaken >= 2)
            {
                logger.LogDebug("Player took too much candy!");
                localPlayer.DamagePlayer(10);
                HUDManager.Instance.UpdateHealthUI(localPlayer.health, true);
                localPlayer.MakeCriticallyInjured(true);
                localPlayer.DropAllHeldItemsAndSync();
                noHands = true;
                HUDManager.Instance.UIAudio.PlayOneShot(BoneCracksfx, 1f);
                HUDManager.Instance.DisplayTip("Took too much candy", "You feel a sharp pain where your hands should be. They've been severed by an unknown force.");
                localPlayer.JumpToFearLevel(1f, true);
                // TODO: Make it so the players hands are no longer visible

                StatusEffectController.Instance.DamagePlayerOverTime(5, 2, true);

                return;
            }

            List<Item> candies = StartOfRound.Instance.allItemsList.itemsList.Where(x => CandyNames.Contains(x.itemName)).ToList();
            logger.LogDebug($"Candy count: {candies.Count}");
            Item candy = candies[UnityEngine.Random.Range(0, candies.Count)];
            logger.LogDebug("Got Candy");
            candy.spawnPrefab.GetComponent<CandyBehavior>().pinataCandy = false;
            logger.LogDebug("Set pinataCandy to false");
            int scrapValue = (int)UnityEngine.Random.Range(configCandyMinValue.Value, configCandyMaxValue.Value * RoundManager.Instance.scrapValueMultiplier);
            logger.LogDebug("Got scrapValue");
            NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, candy.itemName, scrapValue, transform.position, Quaternion.identity, false, true);
            logger.LogDebug("Spawned candy");
            candyTaken++;
            logger.LogDebug("Candy taken: " + candyTaken);
        }
    }
}
