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

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                logger.LogDebug("Eating cake");
                HUDManager.Instance.UIAudio.PlayOneShot(EatCakesfx, 1f);
                StatusEffectController.Instance.HealPlayer(config559HealAmount.Value);
                playerHeldBy.DespawnHeldObject();
            }
        }
    }
}