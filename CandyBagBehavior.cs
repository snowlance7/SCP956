using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static SCP956.Plugin;

namespace SCP956
{
    internal class CandyBagBehavior : PhysicsProp
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        private PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public Dictionary<string, List<bool>> CandyBag = new Dictionary<string, List<bool>>
        {
            { "Blue Candy", new List<bool>() },
            { "Green Candy", new List<bool>() },
            { "Pink Candy", new List < bool >() },
            { "Purple Candy", new List < bool >() },
            { "Rainbow Candy", new List<bool>() },
            { "Red Candy", new List<bool>() },
            { "Yellow Candy", new List<bool>() },
            { "Black Candy", new List<bool>() }
        };
        // TODO: Make sure this is working properly
        // TODO: Make it so it adds candy to the bag in a rpc so its synced and candy can be shared between players
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                CandyBagUIController uiController = GetComponent<CandyBagUIController>();
                logger.LogDebug("Got uiController");
                if (uiController.veMain.style.display == null)
                {
                    logger.LogDebug("veMain.style.display is null");
                    return;
                }

                if (uiController.veMain.style.display == DisplayStyle.None)
                {
                    logger.LogDebug("Showing UI");
                    uiController.ShowUI(CandyBag);
                }
            }
        }

        public void CandySelected(string candyName, bool right)
        {
            if (right)
            {
                logger.LogDebug("Taking candy out of bag");

                bool pinataCandy = CandyBag[candyName].Last();
                CandyBag[candyName].Remove(CandyBag[candyName].Last());

                NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, candyName, 0, localPlayer.transform.position, Quaternion.identity, true, pinataCandy);
            }
            else
            {
                logger.LogDebug("Eating candy from bag");

                bool pinataCandy = CandyBag[candyName].Last();
                CandyBag[candyName].Remove(CandyBag[candyName].Last());

                CandyBehavior.ActivateCandy(candyName, pinataCandy);
            }

            if (!CandyBag.Where(x => x.Value.Count() > 0).Any())
            {
                logger.LogDebug("Candy bag empty");
                playerHeldBy.DespawnHeldObject();
            }
        }
    }
}