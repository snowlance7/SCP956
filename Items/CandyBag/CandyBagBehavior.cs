using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using static SCP956.Plugin;

namespace SCP956.Items.CandyBag
{
    public class CandyBagBehavior : PhysicsProp
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable 0649
        public AudioSource ItemSFX = null!;
        public CandyBagUIController UIController = null!;
        public ScanNodeProperties ScanNode = null!;
#pragma warning restore 0649

        public Dictionary<string, int> CandyInBag = new Dictionary<string, int>()
        {
            {"Black Candy", 0},
            {"Blue Candy", 0 },
            {"Green Candy", 0 },
            {"Pink Candy", 0 },
            {"Purple Candy", 0 },
            {"Rainbow Candy", 0 },
            {"Red Candy", 0 },
            {"Yellow Candy", 0 }
        };

        public override void Start()
        {
            base.Start();
            ScanNode.subText = "Candy in bag: 0";
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                if (UIController.veMain.style.display == null)
                {
                    logger.LogError("veMain.style.display is null");
                    return;
                }

                if (UIController.veMain.style.display == DisplayStyle.None)
                {
                    logger.LogDebug("Showing UI");
                    UIController.ShowUI(CandyInBag);
                }
            }
        }

        public void UpdateScanNode()
        {
            int count = 0;
            foreach (var candy in CandyInBag.Values)
            {
                count += candy;
            }

            ScanNode.subText = $"Candy in bag: {count}";
        }

        public void CandySelected(string candyName, bool right)
        {
            if (CandyInBag[candyName] == 0) { return; }

            if (right)
            {
                logger.LogDebug("Taking candy out of bag");

                RemoveCandyFromBag(candyName, true);
            }
            else
            {
                logger.LogDebug("Eating candy from bag");

                RemoveCandyFromBag(candyName, false);

                ItemSFX.PlayOneShot(ItemSFX.clip, 1f);
                CandyBehavior.ActivateCandy(candyName);
            }

            UpdateScanNode();
        }

        public void AddCandyToBag(string candyName)
        {
            AddCandyToBagServerRpc(candyName);
        }

        public void RemoveCandyFromBag(string candyName, bool spawnCandy)
        {
            RemoveCandyFromBagServerRpc(candyName, spawnCandy);
        }

        // RPCs

        [ServerRpc(RequireOwnership = false)]
        private void AddCandyToBagServerRpc(string candyName)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                AddCandyToBagClientRpc(candyName);
            }
        }

        [ClientRpc]
        private void AddCandyToBagClientRpc(string candyName)
        {
            CandyInBag[candyName] += 1;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RemoveCandyFromBagServerRpc(string candyName, bool spawnCandy)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (spawnCandy)
                {
                    NetworkHandler.Instance.SpawnItemServerRpc(playerHeldBy.actualClientId, candyName, 0, default, default, true);
                }

                RemoveCandyFromBagClientRpc(candyName);
            }
        }

        [ClientRpc]
        private void RemoveCandyFromBagClientRpc(string candyName)
        {
            CandyInBag[candyName] -= 1;
        }
    }
}