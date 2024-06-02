using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Extras;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static SCP956.SCP956;
using LethalLib;
using static LethalLib.Modules.Enemies;
using static UnityEngine.VFX.VisualEffectControlTrackController;
using Unity.Netcode;
using GameNetcodeStuff;
using static UnityEngine.ParticleSystem.PlaybackState;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace SCP956.Patches
{
    // TODO: Figure out how to determine if scrap spawn is on a table or on the floor, USE RAYCASTING
    // spawnpositiontypes: GeneralItemClass, TabletopItems, SmallItems
    [HarmonyPatch]
    internal class TESTING
    {
        private static ManualLogSource logger = SCP956.LoggerInstance;

        private static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), "PingScan_performed")]
        public static void PingScan_performedPostFix()
        {
            // spawnpositiontypes: GeneralItemClass, TabletopItems, SmallItems
            logger.LogDebug("ping scan performed");


            RoundManager.Instance.SpawnScrapInLevel();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnScrapInLevel))]
        public static void SpawnScrapInLevelPreFix()
        {
            logger.LogDebug("spawn scrap in level");

            List<SpawnableItemWithRarity> newScrapList = new List<SpawnableItemWithRarity>();
            foreach (SpawnableItemWithRarity item in RoundManager.Instance.currentLevel.spawnableScrap)
            {
                if (item.spawnableItem.spawnPositionTypes.Count == 1 && item.spawnableItem.spawnPositionTypes[0].name == "GeneralItemClass")
                {
                    newScrapList.Add(item);
                }
            }
            RoundManager.Instance.currentLevel.spawnableScrap.Clear();
            RoundManager.Instance.currentLevel.spawnableScrap.AddRange(newScrapList);
        }
    }
}