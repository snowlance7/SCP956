using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Extras;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static SCP956.Plugin;
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
        private static ManualLogSource logger = Plugin.LoggerInstance;

        private static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), "PingScan_performed")]
        public static void PingScan_performedPostFix()
        {
            // spawnpositiontypes: GeneralItemClass, TabletopItems, SmallItems
            logger.LogDebug("ping scan performed");

            //string effects = "status negation:10,true;DamageReduction:15, 35, false, true;HealthRegen:10,5;restorestamina:50;IncreasedMovementSpeed:15,10;";
            //StatusEffectController.Instance.ApplyCandyEffects(effects);
            //StatusEffectController.Instance.RestoreStamina(10);
            //StatusEffectController.Instance.InfiniteSprint(10, true);
            //StatusEffectController.Instance.IncreasedMovementSpeed(10, 10, true, true);

            /*Item scp330 = LethalLib.Modules.Items.LethalLibItemList.Where(x => x.name == "CandyBowlItem").First();
            Item scp330p = LethalLib.Modules.Items.LethalLibItemList.Where(x => x.name == "CandyBowlPItem").First();
            logger.LogDebug("Got items");

            logger.LogDebug(scp330.name + ": " + scp330.spawnPositionTypes.Count);
            logger.LogDebug(scp330p.name + ": " + scp330p.spawnPositionTypes.Count);
            //StartOfRound.Instance.ManuallyEjectPlayersServerRpc();
            //RoundManager.Instance.SpawnScrapInLevel();*/
        }

        /*[HarmonyPrefix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnScrapInLevel))]
        public static void SpawnScrapInLevelPreFix()
        {
            logger.LogDebug("spawn scrap in level");
            //SpawnableItemWithRarity scp330 = RoundManager.Instance.currentLevel.spawnableScrap.Where(x => x.spawnableItem.itemName == "SCP-330").First();
            List<SpawnableItemWithRarity> newScrapList = new List<SpawnableItemWithRarity>();
            foreach (SpawnableItemWithRarity item in RoundManager.Instance.currentLevel.spawnableScrap)
            {
                if (item.spawnableItem.spawnPositionTypes.Count == 1 && item.spawnableItem.spawnPositionTypes[0].name == "TabletopItems" && item.spawnableItem.itemName != "Fancy lamp")
                {
                    newScrapList.Add(item);
                }
            }


            RandomScrapSpawn[] source = UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>();
            List<RandomScrapSpawn> tabletopspawns = source.Where(x => x.spawnableItems.name == "TabletopItems").ToList();
            scp330.spawnableItem.spawnPositionTypes.Add(tabletopspawns[UnityEngine.Random.Range(0, tabletopspawns.Count)].spawnableItems); // TODO: MAKE THIS WORK
            newScrapList.Add(scp330);

            //RoundManager.Instance.currentLevel.spawnableScrap.Clear();
            //RoundManager.Instance.currentLevel.spawnableScrap.AddRange(newScrapList);
            //RoundManager.Instance.currentLevel.spawnableScrap.Add(item);
            // No tiles containing a scrap spawn with item type: SCP-330
        }*/
    }
}