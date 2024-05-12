using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;

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
                
            }
        }
    }
}
