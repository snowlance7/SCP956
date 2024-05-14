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
using Steamworks.Data;
using static SCP956.SCP956;

namespace SCP956
{
    public class SCP956AI : EnemyAI
    {
        //#pragma warning disable 0649
        public Transform turnCompass = null!;
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
            Dormant,
            Activated
        }

        public void HeadbuttPlayer()
        {
            logger.LogDebug("In AttackPlayer()");

            
        }



        public override void Start()
        {
            
            logger.LogDebug("Start()");
            logger.LogDebug("Pinata spawned!");
            base.Start();

            timeSinceHittingLocalPlayer = 0;
            positionRandomness = new Vector3(0, 0, 0);
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            isDeadAnimationDone = false;
            // NOTE: Add your behavior states in your enemy script in Unity, where you can configure fun stuff
            // like a voice clip or an sfx clip to play when changing to that specific behavior state.
            currentBehaviourStateIndex = (int)State.Dormant;
            // We make the enemy start searching. This will make it start wandering around.
            ////StartSearch(transform.position);
            
        }
        
        public override void Update()
        {
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

        bool FoundClosestPlayerInRange(float range, float senseRange)
        {
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: true);
            if (targetPlayer == null)
            {
                // Couldn't see a player, so we check if a player is in sensing distance instead
                TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: false);
                range = senseRange;
            }
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) < range;
        }

        bool TargetClosestPlayerInAnyCase()
        {
            mostOptimalDistance = 2000f;
            targetPlayer = null;
            for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++)
            {
                tempDist = Vector3.Distance(transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
                if (tempDist < mostOptimalDistance)
                {
                    mostOptimalDistance = tempDist;
                    targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
                }
            }
            if (targetPlayer == null) return false;
            return true;
        }

        public override void OnCollideWithPlayer(Collider other)
        {
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

        public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1) // TODO: May be unneeded
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
            if (isEnemyDead)
            {
                return;
            }

            //enemyHP -= force;
            if (IsOwner)
            {
                if (enemyHP <= 0 && !isEnemyDead)
                {
                    // Our death sound will be played through creatureVoice when KillEnemy() is called.
                    // KillEnemy() will also attempt to call creatureAnimator.SetTrigger("KillEnemy"),
                    // so we don't need to call a death animation ourselves.

                    ////StopCoroutine(SwingAttack());
                    // We need to stop our search coroutine, because the game does not do that by default.
                    ////StopCoroutine(searchCoroutine);
                    KillEnemyOnOwnerClient();
                }
            }
        }
    }
}
// TODO: Make the pinata die from explosions only