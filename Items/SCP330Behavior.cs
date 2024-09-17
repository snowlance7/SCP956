using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956.Items
{
    internal class SCP330Behavior : PhysicsProp
    {
        private static ManualLogSource logger = LoggerInstance;

        private Dictionary<PlayerControllerB, int> PlayersCandyTaken = new Dictionary<PlayerControllerB, int>();
        public static bool noHands = false;

        public int localPlayerCandyTaken = 0; // TODO: Test this

#pragma warning disable 0649
        public AudioSource ItemSFX = null!;
        public ScanNodeProperties ScanNode = null!;
#pragma warning restore 0649

        public override void Start()
        {
            base.Start();
            ScanNode.subText = "";
        }

        public override void InteractItem()
        {
            logger.LogDebug("Interacting with SCP-330");

            localPlayerCandyTaken += 1;
            logger.LogDebug("Candy taken: " + localPlayerCandyTaken);

            if (!PlayersCandyTaken.ContainsKey(localPlayer))
            {
                PlayersCandyTaken.Add(localPlayer, 0);
            }

            if (PlayersCandyTaken[localPlayer] >= 4 || (!IsYoung && PlayersCandyTaken[localPlayer] >= 2))
            {
                logger.LogDebug("Player took too much candy!");
                localPlayer.DamagePlayer(10);
                HUDManager.Instance.UpdateHealthUI(localPlayer.health, true);
                localPlayer.MakeCriticallyInjured(true);
                localPlayer.DropAllHeldItemsAndSync();
                noHands = true;
                ItemSFX.Play(); // TODO: Test this
                HUDManager.Instance.DisplayTip("Took too much candy", "You feel a sharp pain where your hands should be. They've been severed by an unknown force.");
                localPlayer.JumpToFearLevel(1f);
                localPlayer.thisPlayerModelArms.enabled = false; // TODO: Test this

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
