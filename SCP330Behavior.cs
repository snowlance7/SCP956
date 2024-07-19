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

        private Dictionary<PlayerControllerB, int> PlayersCandyTaken = new Dictionary<PlayerControllerB, int>();
        public static bool noHands = false;

        public AudioSource ItemSFX; // TODO: Set audio clip to a slicing sound

        public override void Start()
        {
            base.Start();

            ItemSFX.enabled = true;
        }

        public override void InteractItem()
        {
            logger.LogDebug("Interacting with SCP-330");

            KeyValuePair<PlayerControllerB, int> player = PlayersCandyTaken.Where(x => x.Key == localPlayer).FirstOrDefault();
            if (player.Key == null)
            {
                PlayersCandyTaken.Add(localPlayer, 0);
            }

            if (player.Value >= 4 || (PlayerAge >= 12 && player.Value >= 2))
            {
                logger.LogDebug("Player took too much candy!");
                localPlayer.DamagePlayer(10);
                HUDManager.Instance.UpdateHealthUI(localPlayer.health, true);
                localPlayer.MakeCriticallyInjured(true);
                localPlayer.DropAllHeldItemsAndSync();
                noHands = true;
                ItemSFX.Play();
                HUDManager.Instance.DisplayTip("Took too much candy", "You feel a sharp pain where your hands should be. They've been severed by an unknown force.");
                localPlayer.JumpToFearLevel(3f);
                // TODO: Make it so the players hands are no longer visible

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
