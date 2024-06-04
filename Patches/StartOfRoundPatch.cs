using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Netcode;
using static SCP956.Plugin;
using UnityEngine;

namespace SCP956.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(StartOfRound.firstDayAnimation))]
        public static void Patch()
        {
            logger.LogDebug("First day started");

            // Setting up player age

            if (Plugin.config956Behavior.Value == 3)
            {
                if (configMaxAge.Value < 5) { configMaxAge.Value = 5; }
                PlayerAge = (int)UnityEngine.Random.Range(5, configMaxAge.Value);

                if (PlayerAge < 12)
                {
                    NetworkHandler.Instance.ChangePlayerSizeServerRpc(StartOfRound.Instance.localPlayerController.actualClientId, 0.8f);
                }
            }
            else
            {
                if (configMaxAge.Value < 18) { configMaxAge.Value = 18; }
                PlayerAge = (int)UnityEngine.Random.Range(18, configMaxAge.Value);
            }

            logger.LogDebug($"Age is {PlayerAge}");

            // Setting up itemgroups

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                ItemGroup TabletopItems = Resources.FindObjectsOfTypeAll<ItemGroup>().Where(x => x.name == "TabletopItems").First(); // Testing
                ItemGroup GeneralItemClass = Resources.FindObjectsOfTypeAll<ItemGroup>().Where(x => x.name == "GeneralItemClass").First();
                logger.LogDebug("Got itemgroups");

                Item scp330 = LethalLib.Modules.Items.LethalLibItemList.Where(x => x.name == "CandyBowlItem").First();
                Item scp330p = LethalLib.Modules.Items.LethalLibItemList.Where(x => x.name == "CandyBowlPItem").First();
                logger.LogDebug("Got items");

                scp330.spawnPositionTypes.Clear();
                scp330p.spawnPositionTypes.Clear();
                scp330.spawnPositionTypes.Add(TabletopItems);
                scp330p.spawnPositionTypes.Add(GeneralItemClass);
            }
        }
    }
}
