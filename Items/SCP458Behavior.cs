using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956.Items
{
    internal class SCP458Behavior : PhysicsProp // TODO: Make this a singleton?
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable 0649
        public AudioClip PizzaEatSFX = null!;
#pragma warning restore 0649

        public override void Start()
        {
            base.Start();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (StatusEffectController.PlayerFullness + StatusEffectController.pizzaFillAmount <= 1f)
            {
                playerHeldBy.itemAudio.PlayOneShot(PizzaEatSFX, 1f);
                StatusEffectController.Instance.PizzaHealing();
            }
        }
    }
}