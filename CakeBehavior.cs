using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956
{
    internal class CakeBehavior : PhysicsProp
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        public AudioSource ItemSFX;

        public override void Start()
        {
            base.Start();

            ItemSFX.enabled = true;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                logger.LogDebug("Eating cake");
                ItemSFX.Play();
                StatusEffectController.Instance.HealPlayer(config559HealAmount.Value);
                playerHeldBy.DespawnHeldObject();
            }
        }
    }
}