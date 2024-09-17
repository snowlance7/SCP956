using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956.Items.Cake
{
    internal class CakeBehavior : PhysicsProp
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable 0649
        public AudioClip CakeEatSFX = null!;
#pragma warning restore 0649

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                logger.LogDebug("Eating cake");
                playerHeldBy.itemAudio.PlayOneShot(CakeEatSFX, 0.5f);
                StatusEffectController.Instance.HealPlayer(config559HealAmount.Value);

                if (localPlayer == playerHeldBy && config559CakeReversesAge.Value && PlayerAge != PlayerOriginalAge)
                {
                    ChangePlayerAge(PlayerOriginalAge);
                }

                playerHeldBy.DespawnHeldObject();
            }
        }
    }
}