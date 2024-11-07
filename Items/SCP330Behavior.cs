using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956.Items
{
    internal class SCP330Behavior : PhysicsProp 
        // bowl with pedestal: 
        // bowl: -0.21 0.12 -0.3
    {
        private static ManualLogSource logger = LoggerInstance;
        public static SCP330Behavior? Instance { get; private set; }

        private Dictionary<PlayerControllerB, int> PlayersCandyTaken = new Dictionary<PlayerControllerB, int>();
        public static bool noHands = false;

        public int localPlayerCandyTaken = 0;

#pragma warning disable 0649
        public AudioSource ItemSFX = null!;
        public ScanNodeProperties ScanNode = null!;
#pragma warning restore 0649

        const string standingGrabTooltip = "Take a piece of candy";
        const string crouchingGrabTooltip = "Pickup bowl of candy";

        public override void Start()
        {
            base.Start();
            ScanNode.subText = "Take no more than two please!!";
        }

        IEnumerator DelayedStart()
        {
            yield return new WaitUntil(() => NetworkObject.IsSpawned);

            if (Instance != null && NetworkObject.IsSpawned)
            {
                if (IsServerOrHost)
                {
                    logger.LogDebug("There is already a SCP-330 in the scene. Removing this one.");
                    NetworkObject.Despawn(true);
                }
            }
            else
            {
                Instance = this;
            }
        }

        public override void Update()
        {
            base.Update();

            if (itemProperties.name != "CandyBowlPItem")
            {
                if (localPlayer.isCrouching && config330AllowPickingUpWhenCrouched.Value)
                {
                    grabbable = true;
                    customGrabTooltip = crouchingGrabTooltip;
                }
                else
                {
                    grabbable = false;
                    customGrabTooltip = standingGrabTooltip;
                }
            }
        }

        public override void InteractItem()
        {
            if (config330AllowPickingUpWhenCrouched.Value && localPlayer.isCrouching) { return; }
            logger.LogDebug("Interacting with SCP-330");

            localPlayerCandyTaken += 1;
            logger.LogDebug("Candy taken: " + localPlayerCandyTaken);

            if (!PlayersCandyTaken.ContainsKey(localPlayer))
            {
                PlayersCandyTaken.Add(localPlayer, 0);
            }

            if (PlayersCandyTaken[localPlayer] >= 4 || (!localPlayerIsYoung && PlayersCandyTaken[localPlayer] >= 2))
            {
                logger.LogDebug("Player took too much candy!");
                localPlayer.DamagePlayer(10);
                HUDManager.Instance.UpdateHealthUI(localPlayer.health, true);
                localPlayer.MakeCriticallyInjured(true);
                localPlayer.DropAllHeldItemsAndSync();
                noHands = true;
                ItemSFX.Play();
                HUDManager.Instance.DisplayTip("Took too much candy", "You feel a sharp pain where your hands should be. They've been severed by an unknown force.");
                localPlayer.JumpToFearLevel(1f);
                
                if (config330ShowNoArmsWhenHandsCut.Value)
                {
                    NetworkHandler.Instance.SetPlayerArmsVisibleServerRpc(localPlayer.actualClientId, false);
                }

                StatusEffectController.Instance.DamagePlayerOverTime(5, 2, true);

                return;
            }

            NetworkHandler.Instance.SpawnItemServerRpc(localPlayer.actualClientId, CandyNames[UnityEngine.Random.Range(0, CandyNames.Count)], 0, transform.position, Quaternion.identity, true);
            logger.LogDebug("Spawned candy");
            PlayersCandyTaken[localPlayer] += 1;
            logger.LogDebug("Candy taken: " + PlayersCandyTaken[localPlayer]);
        }
    }
}
