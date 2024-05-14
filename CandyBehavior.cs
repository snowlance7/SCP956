using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;
using static SCP956.SCP956;
using UnityEngine;

namespace SCP956
{
    internal class CandyBehavior : PhysicsProp
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                if (PlayerAge >= 12)
                {
                    if (PluginInstance.random != null)
                    {
                        if (PluginInstance.random.Next(0, 100) < 35)
                        {
                            playerHeldBy.KillPlayer(new Vector3(), true, CauseOfDeath.Unknown, 3);
                        }

                        if (config956Behavior.Value == 2)
                        {
                            // TODO: Create random effect like in secret lab
                        }
                    }
                }
                else
                {
                    // TODO: Turn player into SCP956
                }

                playerHeldBy.DespawnHeldObject();
            }
        }
    }
}
