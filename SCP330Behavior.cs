using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static SCP956.SCP956;

namespace SCP956
{
    internal class SCP330Behavior : PhysicsProp
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        public override void InteractItem()
        {
            //candy.spawnPrefab.gameObject.GetComponent<ScanNodeProperties>().headerText = "Candy"; // TODO: Do something like this when giving the player candy, or use getcomponentinparent
            logger.LogDebug("Interacting with SCP-330");
        }
    }
}
