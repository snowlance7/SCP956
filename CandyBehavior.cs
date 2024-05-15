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
                //Vector3 tempPos = playerHeldBy.transform.position;
                playerHeldBy.KillPlayer(new Vector3(), false, CauseOfDeath.Unknown, 0);

                int index = RoundManager.Instance.currentLevel.Enemies.FindIndex(x => x.enemyType.enemyName == "SCP-956");
                RoundManager.Instance.SpawnEnemyOnServer(playerHeldBy.transform.position, playerHeldBy.previousYRot, index); // TODO: TEST
                playerHeldBy.DespawnHeldObject();
                return;

                if (PlayerAge >= 12)
                {
                    if (PluginInstance.random != null)
                    {
                        if (PluginInstance.random.Next(0, 100) < 35)
                        {
                            playerHeldBy.KillPlayer(new Vector3(), true, CauseOfDeath.Unknown, 3);
                        }

                        if (config956Behavior.Value == 2)
                        {
                            // TODO: Create random effect like in secret lab
                        }
                    }
                }
                else
                {
                    // TODO: Animation for player turning into SCP956 and bones crunching sound effects. Maybe spawn in as scavenger model and play animation to turn into scp956!
                    /*Vector3 tempPos = playerHeldBy.transform.position;
                    playerHeldBy.KillPlayer(new Vector3(), false, CauseOfDeath.Unknown, 0);

                    int index = RoundManager.Instance.currentLevel.Enemies.FindIndex(x => x.enemyType.enemyName == "SCP-956");
                    RoundManager.Instance.SpawnEnemyOnServer(tempPos, playerHeldBy.previousYRot, index);*/
                }

                playerHeldBy.DespawnHeldObject();
            }
        }
    }
}
