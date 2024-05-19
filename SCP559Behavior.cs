using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SCP956.SCP956;

namespace SCP956
{
    internal class SCP559Behavior : PhysicsProp
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                base.gameObject.GetComponent<AudioSource>().PlayOneShot(CandleBlowsfx); // TODO: This no works
                SCP956.PlayerAge = 10;

                NetworkHandler.clientEventShrinkPlayer.InvokeAllClients(true);
                playerHeldBy.DespawnHeldObject();
            }
        }
    }
}
// TODO: Add tooltips in hud for blowing out candles
// TODO: Add more candles to the cake so it equals 10
// TODO: Make it so when you blow out the candles, the fire on the candles go out
// TODO: Make the pitch go up when you blow out the candles