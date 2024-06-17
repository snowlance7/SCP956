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
using Unity.Netcode;
using GameNetcodeStuff;
using static UnityEngine.ParticleSystem.PlaybackState;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Burst.Intrinsics;
using System.Collections;
using static UnityEngine.VFX.VisualEffectControlTrackController;
using UnityEngine.InputSystem.Utilities;

namespace SCP956.Patches
{
    // spawnpositiontypes: GeneralItemClass, TabletopItems, SmallItems
    [HarmonyPatch]
    internal class TESTING : MonoBehaviour
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        private static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), "PingScan_performed")]
        public static void PingScan_performedPostFix()
        {
            //StatusEffectController.Instance.TransformPlayer(localPlayer); // TODO: [Error  : Unity Log] [Netcode-Server Sender=1] Destroy a spawned NetworkObject on a non-host client is not valid. Call Destroy or Despawn on the server/host instead.

            //SCP956AI scp = RoundManager.Instance.SpawnedEnemies.OfType<SCP956AI>().FirstOrDefault();

            //Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(scp.transform.position, config956TeleportRange.Value, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
            //scp.Teleport(pos);

            // deathAnimations:
            // 0 = normal
            // 1 = decapitation
            // 2 = coilhead decapitation
            // 3 = seizure
            // 4 = disapearance // was masked
            // 5 = mask
            // 6 = burn
            // spawnpositiontypes: GeneralItemClass, TabletopItems, SmallItems
            // PlayerAge = 10;
            //logger.LogDebug("ping scan performed");
            //logger.LogDebug("Player Age: " + PlayerAge);
            //logger.LogDebug("Hands?: " + SCP330Behavior.noHands);
            //logger.LogDebug("Candy taken: " + SCP330Behavior.candyTaken);

            //localPlayer.bodyParts[1].GetComponent<Renderer>().enabled = false; // TODO: Do something with this to hide the players hands?
            //WhiteSpike — Today at 8:09 PM
            //You would probably have to mess with what's shown in the camera (the local body) and then the model itself when viewed by others (which means executed by other clients)
            /*[Debug: Pinata] spine.004
            [Debug: Pinata] arm.R_lower
            [Debug: Pinata] arm.L_lower
            [Debug: Pinata] shin.R
            [Debug: Pinata] shin.L
            [Debug: Pinata] spine.002
            [Debug: Pinata] Player
            [Debug: Pinata] thigh.R
            [Debug: Pinata] thigh.L
            [Debug: Pinata] arm.L_upper
            [Debug: Pinata] arm.R_upper*/


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
    }
}