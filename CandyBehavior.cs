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

namespace SCP956
{
    public class CandyBehavior : PhysicsProp
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        public bool pinataCandy = true;

        public override void Start()
        {
            base.Start();
            // TODO: Set node sub text here?
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
                logger.LogDebug("Putting candy in bag");
                playerHeldBy.DespawnHeldObject();

                if (playerHeldBy.ItemSlots.Where(x => x != null && x.itemProperties.name == "BagOfCandyItem").FirstOrDefault() == null)
                {
                    NetworkHandler.Instance.SpawnItemServerRpc(playerHeldBy.actualClientId, "Candy Bag", 0, playerHeldBy.transform.position, Quaternion.identity, true);
                }

                GrabbableObject candyBagObj = playerHeldBy.ItemSlots.Where(x => x != null && x.itemProperties.name == "BagOfCandyItem").FirstOrDefault();
                if (candyBagObj == null) { logger.LogError("Candy bag not found"); return; }

                CandyBagBehavior candyBag = candyBagObj.gameObject.GetComponent<CandyBagBehavior>();
                candyBag.AddCandyToBagClientRpc(itemProperties.itemName, pinataCandy);
            }
        }

        public override void GrabItem()
        {
            base.GrabItem();
            if (configSecretLab.Value)
            {
                if (StartOfRound.Instance.localPlayerController.isInsideFactory && RoundManager.Instance.SpawnedEnemies.Where(x => x.enemyType.enemyName == "SCP-956").FirstOrDefault() == null)
                {
                    NetworkHandler.Instance.SpawnPinataNearbyServerRpc(StartOfRound.Instance.localPlayerController.transform.position);
                }
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            logger.LogDebug("Item activate used");
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                logger.LogDebug("Eating candy");
                playerHeldBy.DespawnHeldObject();
                ActivateCandy(itemProperties.itemName, pinataCandy);
            }
        }

        public static void ActivateCandy(string candyName, bool pinataCandy)
        {
            HUDManager.Instance.UIAudio.PlayOneShot(CandyCrunchsfx, 1f);

            if (!pinataCandy || PlayerAge >= 12)
            {
                if (pinataCandy && (int)UnityEngine.Random.Range(0, 101) < configCandyDeathChance.Value)
                {
                    localPlayer.KillPlayer(new Vector3(), true, CauseOfDeath.Unknown, 3);
                    return;
                }

                if (!pinataCandy || configSecretLab.Value)
                {
                    logger.LogDebug("Activating candy effects...");

                    switch (candyName)
                    {
                        case "Blue Candy":
                            logger.LogDebug("Candy blue");
                            if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyBlueEffects.Value); }
                            else
                            {
                                StatusEffectController.Instance.HealPlayer(30, true);
                            }
                            break;
                        case "Green Candy":
                            logger.LogDebug("Candy green");
                            if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyGreenEffects.Value); }
                            else
                            {
                                StatusEffectController.Instance.StatusNegation(30);
                                StatusEffectController.Instance.HealthRegen(1, 80);
                            }
                            break;
                        case "Purple Candy":
                            logger.LogDebug("Candy purple");
                            if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyPurpleEffects.Value); }
                            else
                            {
                                StatusEffectController.Instance.DamageReduction(15, 20, true);
                                StatusEffectController.Instance.HealthRegen(2, 10);
                            }
                            break;
                        case "Red Candy":
                            logger.LogDebug("Candy red");
                            if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyRedEffects.Value); }
                            else
                            {
                                StatusEffectController.Instance.HealthRegen(9, 5);
                            }
                            break;
                        case "Yellow Candy":
                            logger.LogDebug("Candy yellow");
                            if (configEnableCustomStatusEffects.Value) { StatusEffectController.Instance.ApplyCandyEffects(configCandyYellowEffects.Value); }
                            else
                            {
                                StatusEffectController.Instance.RestoreStamina(25);
                                StatusEffectController.Instance.InfiniteSprint(8);
                                StatusEffectController.Instance.IncreasedMovementSpeed(8, 2, true, true);
                            }
                            break;
                        case "Pink Candy":
                            logger.LogDebug("Candy pink");
                            Landmine.SpawnExplosion(localPlayer.transform.position, true, 3, 3);
                            break;
                        case "Rainbow Candy":
                            logger.LogDebug("Candy rainbow");
                            StatusEffectController.Instance.HealPlayer(15);
                            StatusEffectController.Instance.InfiniteSprint(5, true);
                            StatusEffectController.Instance.bulletProofMultiplier += 1;
                            StatusEffectController.Instance.StatusNegation(10);
                            StatusEffectController.Instance.HealPlayer(20, true);
                            break;
                        case "Black Candy":
                            logger.LogDebug("Candy black");
                            ActivateCandy(CandyNames.Where(x => x != "Black Candy").ToList()[UnityEngine.Random.Range(0, CandyNames.Count - 1)], pinataCandy);
                            break;
                        default:
                            logger.LogDebug("Candy not found");
                            break;
                    }
                }
            }
            else if (configEnablePinata.Value)
            {
                // TODO: Animation for player turning into SCP956 and bones crunching sound effects. Maybe spawn in as scavenger model and play animation to turn into scp956!
                localPlayer.KillPlayer(new Vector3(), false, CauseOfDeath.Unknown, 3);
                int index = RoundManager.Instance.currentLevel.Enemies.FindIndex(x => x.enemyType.enemyName == "SCP-956");
                RoundManager.Instance.SpawnEnemyOnServer(localPlayer.transform.position, localPlayer.previousYRot, index);
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