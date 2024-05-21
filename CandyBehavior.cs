using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;
using static SCP956.SCP956;
using UnityEngine;
using System.Linq;
using UnityEngine.Timeline;

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
                // TODO: Set up tooltips
                playerHeldBy.movementAudio.PlayOneShot(CandyCrunchsfx);

                if (PlayerAge >= 12)
                {
                    if ((int)UnityEngine.Random.Range(0, 101) < config9561DeathChance.Value)
                    {
                        playerHeldBy.KillPlayer(new Vector3(), true, CauseOfDeath.Unknown, 3); // TODO: Make this always do seizure animation but kill when chance is met
                    }

                    if (config956Behavior.Value == 2)
                    {
                        // TODO: Create random effect like in secret lab
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
    }
}
