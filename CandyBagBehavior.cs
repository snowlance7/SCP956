using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
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

                RemoveCandyFromBagClientRpc(candyName, true);
            }
            else
            {
                logger.LogDebug("Eating candy from bag");

                bool pinataCandy = CandyBag[candyName].Last();
                RemoveCandyFromBagClientRpc(candyName, false);

                CandyBehavior.ActivateCandy(candyName, pinataCandy);
            }
        }

        // RPCs

        [ClientRpc]
        public void AddCandyToBagClientRpc(string candyName, bool pinataCandy)
        {
            CandyBag[candyName].Add(pinataCandy);
        }

        [ClientRpc]
        public void RemoveCandyFromBagClientRpc(string candyName, bool spawnCandy)
        {
            bool pinataCandy = CandyBag[candyName].Last();
            CandyBag[candyName].Remove(CandyBag[candyName].Last());

            if (spawnCandy)
            {
                Item item = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == candyName).FirstOrDefault();

                GameObject obj = UnityEngine.Object.Instantiate(item.spawnPrefab, playerHeldBy.transform.position, Quaternion.identity, StartOfRound.Instance.propsContainer);
                //if (newValue != 0) { obj.GetComponent<GrabbableObject>().SetScrapValue(newValue); }
                obj.GetComponent<NetworkObject>().Spawn();

                obj.GetComponent<CandyBehavior>().pinataCandy = pinataCandy;
                GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
                playerHeldBy.carryWeight += Mathf.Clamp(grabbable.itemProperties.weight - 1f, 0f, 10f); // TODO: Test this
                playerHeldBy.GrabObjectServerRpc(grabbable.NetworkObject);
                grabbable.parentObject = playerHeldBy.localItemHolder;
                if (localPlayer == playerHeldBy) { grabbable.GrabItemOnClient(); }
            }

            
            if (localPlayer == playerHeldBy && !CandyBag.Where(x => x.Value.Count() > 0).Any())
            {
                logger.LogDebug("Candy bag empty");
                DespawnItemInSlotOnClient(playerHeldBy.ItemSlots.IndexOf(this));
            }
        }
    }
}