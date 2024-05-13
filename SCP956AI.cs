using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace SCP956
{
    public class SCP956AI : EnemyAI
    {
        //#pragma warning disable 0649
        //public Transform turnCompass = null!;
        //public Transform attackArea = null!;
        //#pragma warning restore 0649

        private static ManualLogSource logger = SCP956.LoggerInstance;
        private float timeSinceHittingLocalPlayer;
        private Vector3 positionRandomness;
        private System.Random enemyRandom;
        private bool isDeadAnimationDone;
        public static int ActivationRadius;
        public static int Behavior;

        enum State
        {
            SearchingForPlayer,
            AttackingPlayers
        }

        public override void Start()
        {
            
            logger.LogDebug("Start()");
            logger.LogDebug("Pinata spawned!");
            transform.rotation = Quaternion.Euler(270, 0, 0); // TODO: THIS ONLY WORKS SOMETIMES IDK WHY TF IT DOES THIS FUCK THIS SHIT I AM BEYOND IRRITATED, only works in certain areas????
            base.Start();

            /*timeSinceHittingLocalPlayer = 0;
            positionRandomness = new Vector3(0, 0, 0);
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            isDeadAnimationDone = false;
            // NOTE: Add your behavior states in your enemy script in Unity, where you can configure fun stuff
            // like a voice clip or an sfx clip to play when changing to that specific behavior state.
            ////currentBehaviourStateIndex = (int)State.SearchingForPlayer;
            // We make the enemy start searching. This will make it start wandering around.
            StartSearch(transform.position);*/
        }
        
        public override void Update()
        {
            return;
            //transform.rotation = Quaternion.Euler(270, 0, 0); // this fucking works of course, what the fuck
            base.Update();
            if (isEnemyDead)
            {
                // For some weird reason I can't get an RPC to get called from HitEnemy() (works from other methods), so we do this workaround. We just want the enemy to stop playing the song.
                if (!isDeadAnimationDone)
                {
                    logger.LogDebug("Stopping enemy voice with janky code.");
                    isDeadAnimationDone = true;
                    creatureVoice.Stop();
                    creatureVoice.PlayOneShot(dieSFX);
                }
                return;
            }
            timeSinceHittingLocalPlayer += Time.deltaTime;
            ////timeSinceNewRandPos += Time.deltaTime;

            var state = currentBehaviourStateIndex;
            if (targetPlayer != null/* && (state == (int)State.StickingInFrontOfPlayer || state == (int)State.HeadSwingAttackInProgress)*/)
            {
                ////turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                ////transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
            }
            if (stunNormalizedTimer > 0f)
            {
                agent.speed = 0f;
            }
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }

            /*switch (base.currentBehaviourStateIndex)
            {
                case 0:
                    base.agent.speed = 3f;
                    if (FoundClosestPlayerInRange(25f, 3f))
                    {
                        LogIfDebugBuild("Start Target Player");
                        this.StopSearch(base.currentSearch, true);
                        this.SwitchToBehaviourClientRpc(1);
                    }
                    break;
                case 1:
                    base.agent.speed = 5f;
                    if (!TargetClosestPlayerInAnyCase() || (Vector3.Distance(((Component)this).transform.position, ((Component)base.targetPlayer).transform.position) > 20f && !this.CheckLineOfSightForPosition(((Component)base.targetPlayer).transform.position, 45f, 60, -1f, (Transform)null)))
                    {
                        LogIfDebugBuild("Stop Target Player");
                        this.StartSearch(((Component)this).transform.position, (AISearchRoutine)null);
                        this.SwitchToBehaviourClientRpc(0);
                    }
                    else
                    {
                        StickingInFrontOfPlayer();
                    }
                    break;
                case 2:
                    break;
                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }*/
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            logger.LogDebug("OnCollideWithPlayer");
            return;
            if (timeSinceHittingLocalPlayer < 1f)
            {
                return;
            }
            PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
            if (playerControllerB != null)
            {
                logger.LogDebug("Enemy Collision with Player!");
                timeSinceHittingLocalPlayer = 0f;
                playerControllerB.DamagePlayer(20);
            }
        }

        public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            if (isEnemyDead)
            {
                return;
            }
            enemyHP -= force;
            if (IsOwner)
            {
                if (enemyHP <= 0 && !isEnemyDead)
                {
                    // Our death sound will be played through creatureVoice when KillEnemy() is called.
                    // KillEnemy() will also attempt to call creatureAnimator.SetTrigger("KillEnemy"),
                    // so we don't need to call a death animation ourselves.

                    ////StopCoroutine(SwingAttack());
                    // We need to stop our search coroutine, because the game does not do that by default.
                    StopCoroutine(searchCoroutine);
                    KillEnemyOnOwnerClient();
                }
            }
        }
    }
}
