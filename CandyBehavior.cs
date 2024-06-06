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

namespace SCP956
{
    public class CandyBehavior : PhysicsProp // TODO: make candy size 0.86 and test (changed from 0.5)
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        public bool pinataCandy = true;
        // TODO: get this script from itemprefab.GetComponent<CandyBehavior>()
        // TODO: Add SCP-330
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                logger.LogDebug("Eating candy");
                HUDManager.Instance.UIAudio.PlayOneShot(CandyCrunchsfx, 1f);
                

                if (!pinataCandy || PlayerAge >= 12)
                {
                    if (pinataCandy && (int)UnityEngine.Random.Range(0, 101) < config9561DeathChance.Value)
                    {
                        playerHeldBy.KillPlayer(new Vector3(), true, CauseOfDeath.Unknown, 3); // TODO: Probably doesnt despawn properly either here
                        return;
                    }

                    if (!pinataCandy || config956Behavior.Value == 2)
                    {
                        switch (itemProperties.itemName)
                        { // TODO: for custom effects, if disabled use config.defaultvalue
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
                            case "Pink Candy": // TODO: Doesnt despawn properly
                                logger.LogDebug("Candy pink");
                                playerHeldBy.DespawnHeldObject();
                                Landmine.SpawnExplosion(playerHeldBy.transform.position, true, 3, 3);
                                break;
                            case "Rainbow Candy":
                                logger.LogDebug("Candy rainbow");
                                StatusEffectController.Instance.HealPlayer(15);
                                StatusEffectController.Instance.InfiniteSprint(5, true);
                                StatusEffectController.Instance.bulletProofMultiplier += 1; // TODO: implement this
                                StatusEffectController.Instance.StatusNegation(10);
                                StatusEffectController.Instance.HealPlayer(20, true);
                                break;
                            default:
                                logger.LogDebug("Candy not found");
                                break;
                        }
                        // TODO: Create random effect like in secret lab
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
                    }
                }
                else
                {
                    // TODO: Animation for player turning into SCP956 and bones crunching sound effects. Maybe spawn in as scavenger model and play animation to turn into scp956!
                    playerHeldBy.KillPlayer(new Vector3(), false, CauseOfDeath.Unknown, 3);
                    int index = RoundManager.Instance.currentLevel.Enemies.FindIndex(x => x.enemyType.enemyName == "SCP-956");
                    RoundManager.Instance.SpawnEnemyOnServer(playerHeldBy.transform.position, playerHeldBy.previousYRot, index);
                }

                //playerHeldBy.DespawnHeldObject(); // TESTING
            }
        }
    }
}
