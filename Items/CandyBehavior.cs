using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;
using static SCP956.Plugin;
using UnityEngine;
using System.Linq;
using UnityEngine.Timeline;
using System.Collections;
using UnityEngine.ProBuilder.Poly2Tri;
using UnityEngine.XR;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.ProBuilder;
using SCP956.Items.CandyBag;
using GameNetcodeStuff;

namespace SCP956.Items
{
    public class CandyBehavior : PhysicsProp
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable 0649
        public AudioClip CandyCrunchSFX = null!;
        public ScanNodeProperties ScanNode = null!;
#pragma warning restore 0649

        public override void Start()
        {
            base.Start();
            ScanNode.subText = "";
        }

        public override void EquipItem()
        {
            base.EquipItem();
            if (configEnableCandyBag.Value) { playerHeldBy.equippedUsableItemQE = true; }
        }

        public override void PocketItem()
        {
            base.PocketItem();
            if (configEnableCandyBag.Value) { playerHeldBy.equippedUsableItemQE = false; }
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (right) { return; }
            if (configEnableCandyBag.Value)
            {
                LogIfDebug("Putting candy in bag");

                PlayerControllerB player = playerHeldBy;
                string name = itemProperties.name;

                playerHeldBy.DespawnHeldObject();
                PutCandyInBag(player, name);
            }
        }

        private static void PutCandyInBag(PlayerControllerB player, string _name)
        {
            GrabbableObject candyBagObj = player.ItemSlots.Where(x => x != null && x.itemProperties.name == "CandyBagItem").FirstOrDefault();

            if (candyBagObj == null)
            {
                NetworkHandler.Instance.SpawnItemServerRpc(player.actualClientId, "CandyBagItem", 0, player.transform.position, Quaternion.identity, true);
                candyBagObj = player.ItemSlots.Where(x => x != null && x.itemProperties.name == "CandyBagItem").FirstOrDefault();
                if (candyBagObj == null) { logger.LogError("Candy bag not found or could not be spawned"); return; }
            }

            CandyBagBehavior candyBag = candyBagObj.gameObject.GetComponent<CandyBagBehavior>();
            candyBag.AddCandyToBag(_name);
            candyBag.UpdateScanNode();
        }

        public override void GrabItem()
        {
            base.GrabItem();

            if (!StartOfRound.Instance.inShipPhase && RoundManager.Instance.SpawnedEnemies.OfType<SCP956AI>().FirstOrDefault() == null)
            {
                NetworkHandler.Instance.SpawnPinataServerRpc();
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            LogIfDebug("Item activate used");
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                LogIfDebug("Eating candy");
                playerHeldBy.itemAudio.PlayOneShot(CandyCrunchSFX, 1f);
                playerHeldBy.DespawnHeldObject();
                ActivateCandy(itemProperties.name);
            }
        }

        public static void ActivateCandy(string name)
        {
            LogIfDebug("Activating candy effects...");

            switch (name)
            {
                case "BlueCandyItem":
                    LogIfDebug("Candy blue");
                    if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyBlueEffects.Value); }
                    else
                    {
                        StatusEffectController.Instance.HealPlayer(30, true);
                    }
                    break;
                case "GreenCandyItem":
                    LogIfDebug("Candy green");
                    if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyGreenEffects.Value); }
                    else
                    {
                        StatusEffectController.Instance.StatusNegation(30);
                        StatusEffectController.Instance.HealthRegen(1, 80);
                    }
                    break;
                case "PurpleCandyItem":
                    LogIfDebug("Candy purple");
                    if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyPurpleEffects.Value); }
                    else
                    {
                        StatusEffectController.Instance.DamageReduction(15, 20, true);
                        StatusEffectController.Instance.HealthRegen(2, 10);
                    }
                    break;
                case "RedCandyItem":
                    LogIfDebug("Candy red");
                    if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyRedEffects.Value); }
                    else
                    {
                        StatusEffectController.Instance.HealthRegen(9, 5);
                    }
                    break;
                case "YellowCandyItem":
                    LogIfDebug("Candy yellow");
                    if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyYellowEffects.Value); }
                    else
                    {
                        StatusEffectController.Instance.RestoreStamina(25);
                        StatusEffectController.Instance.InfiniteSprint(8);
                        StatusEffectController.Instance.IncreasedMovementSpeed(8, 2, true, true);
                    }
                    break;
                case "PinkCandyItem":
                    LogIfDebug("Candy pink");
                    Landmine.SpawnExplosion(localPlayer.transform.position, true, 3, 3);
                    break;
                case "RainbowCandyItem":
                    LogIfDebug("Candy rainbow");
                    StatusEffectController.Instance.HealPlayer(15);
                    StatusEffectController.Instance.InfiniteSprint(5, true);
                    StatusEffectController.Instance.bulletProofMultiplier += 1;
                    StatusEffectController.Instance.StatusNegation(10);
                    StatusEffectController.Instance.HealPlayer(20, true);
                    break;
                case "BlackCandyItem":
                    LogIfDebug("Candy black");
                    ActivateCandy(CandyNames.Where(x => x != "BlackCandyItem").ToList()[UnityEngine.Random.Range(0, CandyNames.Count - 1)]);
                    break;
                default:
                    LogIfDebug("Candy not found");
                    break;
            }
        }
    }
}

// From Secret Lab Wiki:
// Blue:
//      gives 30 hp and goes over max health limit
// Green:
//      Negate status effects for 30 seconds (time stackable) - Web slow effect - undetectable for x seconds?
//      HP regeneration for for 80 seconds 1.5 hp per second 120 hp total
// Purple:
//      20% damage reduction for 15 seconds (time stackable)
//      hp regeneration for 10 seconds 1.5 hp per second 15 hp total
// Rainbow:
//      15 hp
//      infinite sprint for 5 seconds (stackable)
//      damage reduction from turrets for the round
//      Negate status effects for 10 seconds (not stackable)
//      20 hp goes over max health limit
// Red:
//      HP regeneration for 5 seconds 9 hp per second 45 hp total
// Yellow:
//      Instantly restores 25% stamina
//      Infinite sprint for 8 seconds
//      movement speed increase for 8 seconds (effect and time stackable)
// Pink:
//      player instantly explodes