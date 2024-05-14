using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
                logger.LogDebug("In ItemActivate()");
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