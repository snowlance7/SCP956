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

        public Dictionary<string, int> CandyInBag
        {
            get
            {
                return new Dictionary<string, int>()
                {
                    {"BlackCandyItem", BlackCandyItem.Value },
                    {"BlueCandyItem", BlueCandyItem.Value },
                    {"GreenCandyItem", GreenCandyItem.Value },
                    {"PinkCandyItem", PinkCandyItem.Value },
                    {"PurpleCandyItem", PurpleCandyItem.Value },
                    {"RainbowCandyItem", RainbowCandyItem.Value },
                    {"RedCandyItem", RedCandyItem.Value },
                    {"YellowCandyItem", YellowCandyItem.Value }
                };
            }
        }

        public NetworkVariable<int> BlackCandyItem = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> BlueCandyItem = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> GreenCandyItem = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> PinkCandyItem = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> PurpleCandyItem = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> RainbowCandyItem = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> RedCandyItem = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> YellowCandyItem = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /*public Dictionary<string, int> CandyInBag = new Dictionary<string, int>()
        {
            {"BlackCandyItem", 0 },
            {"BlueCandyItem", 0 },
            {"GreenCandyItem", 0 },
            {"PinkCandyItem", 0 },
            {"PurpleCandyItem", 0 },
            {"RainbowCandyItem", 0 },
            {"RedCandyItem", 0 },
            {"YellowCandyItem", 0 }
        };*/

        public override void Start()
        {
            base.Start();
            ScanNode.subText = "Candy in bag: 0";
        }

        public override void GrabItem()
        {
            base.GrabItem();
            if (!NetworkObject.IsOwner)
            {
                ChangeOwnershipServerRpc(playerHeldBy.actualClientId);
            }
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
                    LogIfDebug("Showing UI");
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

        public void CandySelected(string name, bool right)
        {
            LogIfDebug("Selecting candy: " + name);
            if (CandyInBag[name] == 0) { return; }

            if (right)
            {
                LogIfDebug("Taking candy out of bag");

                RemoveCandyFromBag(name, true);
            }
            else
            {
                LogIfDebug("Eating candy from bag");

                RemoveCandyFromBag(name, false);

                ItemSFX.PlayOneShot(ItemSFX.clip, 1f);
                CandyBehavior.ActivateCandy(name);
            }

            UpdateScanNodeServerRpc();
        }

        public void AddCandyToBag(string name)
        {
            //AddCandyToBagServerRpc(name);
            switch (name)
            {
                case "BlackCandyItem":
                    BlackCandyItem.Value += 1;
                    break;
                case "BlueCandyItem":
                    BlueCandyItem.Value += 1;
                    break;
                case "GreenCandyItem":
                    GreenCandyItem.Value += 1;
                    break;
                case "PinkCandyItem":
                    PinkCandyItem.Value += 1;
                    break;
                case "PurpleCandyItem":
                    PurpleCandyItem.Value += 1;
                    break;
                case "RainbowCandyItem":
                    RainbowCandyItem.Value += 1;
                    break;
                case "RedCandyItem":
                    RedCandyItem.Value += 1;
                    break;
                case "YellowCandyItem":
                    YellowCandyItem.Value += 1;
                    break;
                default:
                    Debug.LogWarning("Unknown candy type: " + name);
                    break;
            }
        }

        public void RemoveCandyFromBag(string name, bool spawnCandy)
        {
            //RemoveCandyFromBagServerRpc(name, spawnCandy);
            switch (name)
            {
                case "BlackCandyItem":
                    BlackCandyItem.Value -= 1;
                    break;
                case "BlueCandyItem":
                    BlueCandyItem.Value -= 1;
                    break;
                case "GreenCandyItem":
                    GreenCandyItem.Value -= 1;
                    break;
                case "PinkCandyItem":
                    PinkCandyItem.Value -= 1;
                    break;
                case "PurpleCandyItem":
                    PurpleCandyItem.Value -= 1;
                    break;
                case "RainbowCandyItem":
                    RainbowCandyItem.Value -= 1;
                    break;
                case "RedCandyItem":
                    RedCandyItem.Value -= 1;
                    break;
                case "YellowCandyItem":
                    YellowCandyItem.Value -= 1;
                    break;
                default:
                    Debug.LogWarning("Unknown candy type: " + name);
                    break;
            }

            if (spawnCandy)
            {
                //NetworkHandler.Instance.SpawnItemServerRpc(playerHeldBy.actualClientId, name, 0, default, default, true);
            }
        }

        // RPCs

        [ServerRpc(RequireOwnership = false)]
        private void ChangeOwnershipServerRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                NetworkObject.ChangeOwnership(clientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void UpdateScanNodeServerRpc()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                UpdateScanNodeClientRpc();
            }
        }

        [ClientRpc]
        private void UpdateScanNodeClientRpc()
        {
            UpdateScanNode();
        }

        /*[ServerRpc(RequireOwnership = false)]
        private void AddCandyToBagServerRpc(string name)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                AddCandyToBagClientRpc(name);
            }
        }

        [ClientRpc]
        private void AddCandyToBagClientRpc(string name)
        {
            CandyInBag[name] += 1;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RemoveCandyFromBagServerRpc(string name, bool spawnCandy)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (spawnCandy)
                {
                    NetworkHandler.Instance.SpawnItemServerRpc(playerHeldBy.actualClientId, name, 0, default, default, true);
                }

                RemoveCandyFromBagClientRpc(name);
            }
        }

        [ClientRpc]
        private void RemoveCandyFromBagClientRpc(string name)
        {
            CandyInBag[name] -= 1;
        }*/
    }
}