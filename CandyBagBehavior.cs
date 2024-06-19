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

        public static bool takingCandyOut = false;
        public Dictionary<string, List<bool>> CandyBag = new Dictionary<string, List<bool>> // TODO: IMPORTANT Make sure pinata candy isnt allowed in the candy bags
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

        public override void ItemActivate(bool used, bool buttonDown = true) // TODO: Test UI and change behavior so that the player can press Q to put a candy in the bag. in the ui the player can left click on a candy to eat it and right click to take it out of the bag
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

        public void CandySelected(string candyName) // TODO: TEST THIS
        {
            /*Item candy = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == candyName).FirstOrDefault();
            if (candy == null) { logger.LogError("Candy not found in bag"); return; }

            NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, candyName, 0, localPlayer.transform.position, Quaternion.identity, true);

            CandyBag[candyName]--;

            if (!CandyBag.Where(x => x.Value > 0).Any())
            {
                playerHeldBy.DespawnHeldObject();
            }*/
        }
    }
}