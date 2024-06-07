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
        public static void firstDayAnimationPostfix()
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

            PlayerOriginalAge = PlayerAge;
            logger.LogDebug($"Age is {PlayerAge}");

            // Setting up itemgroups

            if ((NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) && configEnable330.Value)
            {
                ItemGroup TabletopItems = Resources.FindObjectsOfTypeAll<ItemGroup>().Where(x => x.name == "TabletopItems").First();
                ItemGroup GeneralItemClass = Resources.FindObjectsOfTypeAll<ItemGroup>().Where(x => x.name == "GeneralItemClass").First();
                logger.LogDebug("Got itemgroups");

                Item scp330 = LethalLib.Modules.Items.LethalLibItemList.Where(x => x.name == "BowlOfCandyItem").First();
                Item scp330p = LethalLib.Modules.Items.LethalLibItemList.Where(x => x.name == "BowlOfCandyPItem").First();
                logger.LogDebug("Got items");

                scp330.spawnPositionTypes.Clear();
                scp330p.spawnPositionTypes.Clear();
                scp330.spawnPositionTypes.Add(TabletopItems);
                scp330p.spawnPositionTypes.Add(GeneralItemClass);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(StartOfRound.Awake))]
        public static void AwakePostFix()
        {
            logger.LogDebug("Awake PostFix started");

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

            PlayerOriginalAge = PlayerAge;
            logger.LogDebug($"Age is {PlayerAge}");
        }
    }
}
