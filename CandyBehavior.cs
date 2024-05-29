using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;
using static SCP956.SCP956;
using UnityEngine;
using System.Linq;
using UnityEngine.Timeline;
using System.Collections;

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
                logger.LogDebug("Eating candy");
                // TODO: Set up tooltips in unity editor
                HUDManager.Instance.UIAudio.PlayOneShot(CandyCrunchsfx, 1f);
                

                if (PlayerAge >= 12)
                {
                    if ((int)UnityEngine.Random.Range(0, 101) < config9561DeathChance.Value)
                    {
                        playerHeldBy.KillPlayer(new Vector3(), true, CauseOfDeath.Unknown, 3); // TODO: Make this always do seizure animation but kill when chance is met
                    }

                    if (config956Behavior.Value == 2)
                    {
                        
                        // TODO: Create random effect like in secret lab
                        // Blue:
                        //      gives 30 hp and goes over max health limit
                        // Green:
                        //      Negate status effects for 30 seconds (time stackable) - Web slow effect - undetectable for x seconds?
                        //      HP regeneration for for 80 seconds 1.5 hp per second 120 hp total
                        // Purple:
                        //      20% damage reduction for 15 seconds (time stackable)
                        //      hp regeneration for 10 seconds 1.5 hp per second 15 hp total
                        // Rainbow: not added yet
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

                playerHeldBy.DespawnHeldObject();
            }
        }

        public IEnumerator HealthRegen()
        {
            yield return new WaitForSeconds(10f);
        }
    }
}
