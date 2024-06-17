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

        public Dictionary<string, int> CandyBag = new Dictionary<string, int> // TODO: IMPORTANT Make sure pinata candy isnt allowed in the candy bags
        {
            { "Blue Candy", 0 },
            { "Green Candy", 0 },
            { "Pink Candy", 0 },
            { "Purple Candy", 0 },
            { "Rainbow Candy", 0 },
            { "Red Candy", 0 },
            { "Yellow Candy", 0 },
            { "Other", 0 }
        };

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

        public void CandySelected(string candyName) // TODO: TEST THIS
        {
            Item candy = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == candyName).FirstOrDefault();
            if (candy == null) { logger.LogError("Candy not found in bag"); return; }

            NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, candyName, 0, localPlayer.transform.position, Quaternion.identity, false, true);

            CandyBag[candyName]--;

            if (!CandyBag.Where(x => x.Value > 0).Any())
            {
                playerHeldBy.DespawnHeldObject();
            }
        }
    }
}