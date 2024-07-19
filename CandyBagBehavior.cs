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
    internal class CandyBagBehavior : PhysicsProp // TODO: Fix bug where putting candy in bag doesnt show in bag
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        public AudioSource ItemSFX;

        private PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public override void Start()
        {
            base.Start();

            ItemSFX.enabled = true;
        }

        public Dictionary<string, int> CandyBag = new Dictionary<string, int>
        {
            { "Blue Candy", 0 },
            { "Green Candy", 0 },
            { "Pink Candy", 0 },
            { "Purple Candy", 0 },
            { "Rainbow Candy", 0 },
            { "Red Candy", 0 },
            { "Yellow Candy", 0 },
            { "Black Candy", 0 }
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

                RemoveCandyFromBagClientRpc(candyName, false);

                CandyBehavior.ActivateCandy(candyName);
                ItemSFX.Play();
            }
        }

        // RPCs

        [ClientRpc]
        public void AddCandyToBagClientRpc(string candyName)
        {
            int count = CandyBag[candyName] + 1;
            CandyBag[candyName] = count;
        }

        [ClientRpc]
        public void RemoveCandyFromBagClientRpc(string candyName, bool spawnCandy)
        {
            int count = CandyBag[candyName] - 1;
            CandyBag[candyName] = count;

            if (spawnCandy)
            {
                Item item = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == candyName).FirstOrDefault();

                GameObject obj = UnityEngine.Object.Instantiate(item.spawnPrefab, playerHeldBy.transform.position, Quaternion.identity, StartOfRound.Instance.propsContainer);
                obj.GetComponent<NetworkObject>().Spawn();

                GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
                playerHeldBy.GrabObjectServerRpc(grabbable.NetworkObject);
                grabbable.parentObject = playerHeldBy.localItemHolder;
                if (localPlayer == playerHeldBy) { grabbable.GrabItemOnClient(); }
            }
        }
    }
}