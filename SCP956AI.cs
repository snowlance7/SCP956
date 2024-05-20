using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx.Logging;
using GameNetcodeStuff;
using LethalLib;
using SCP956;
using Unity.Netcode;
using UnityEngine;
using static SCP956.SCP956;
using static SCP956.NetworkHandler;
using LethalNetworkAPI;
using UnityEngine.UI;

namespace SCP956
{

    // You may be wondering, how does the Example Enemy know it is from class ExampleEnemyAI?
    // Well, we give it a reference to to this class in the Unity project where we make the asset bundle.
    // Asset bundles cannot contain scripts, so our script lives here. It is important to get the
    // reference right, or else it will not find this file. See the guide for more information.

    class SCP956AI : EnemyAI
    {
        private ManualLogSource logger = LoggerInstance;

        // We set these in our Asset Bundle, so we can disable warning CS0649:
        // Field 'field' is never assigned to, and will always have its default value 'value'
        #pragma warning disable 0649
        public Transform turnCompass = null!;
        #pragma warning restore 0649
        //Vector3 positionRandomness;
        //Vector3 StalkPos;
        System.Random enemyRandom = null!;
        bool isDeadAnimationDone;
        float timeSinceNewRandPos;

        enum State
        {
            Dormant,
            MovingTowardsPlayer,
            HeadButtAttackInProgress
        }
        // TODO: Figure out how all this works first before changing anything
        public override void Start()
        {
            base.Start();
            logger.LogDebug("SCP-956 Spawned");
            //timeSinceHittingLocalPlayer = 0;
            //timeSinceNewRandPos = 0;
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            //isDeadAnimationDone = false;
            currentBehaviourStateIndex = (int)State.Dormant;
            RoundManager.Instance.SpawnedEnemies.Add(this);
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
            //timeSinceHittingLocalPlayer += Time.deltaTime;
            timeSinceNewRandPos += Time.deltaTime;

            var state = currentBehaviourStateIndex;
            if (targetPlayer != null && state == (int)State.MovingTowardsPlayer)
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 0.8f * Time.deltaTime);
                targetPlayer.turnCompass.LookAt(transform.position);
                targetPlayer.transform.rotation = Quaternion.Lerp(targetPlayer.transform.rotation, Quaternion.Euler(new Vector3(0f, targetPlayer.turnCompass.eulerAngles.y, 0f)), 0.8f * Time.deltaTime);
            }
            if (state == (int)State.HeadButtAttackInProgress)
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 3f * Time.deltaTime);
                targetPlayer.turnCompass.LookAt(transform.position);
                targetPlayer.transform.rotation = Quaternion.Lerp(targetPlayer.transform.rotation, Quaternion.Euler(new Vector3(0f, targetPlayer.turnCompass.eulerAngles.y, 0f)), 3f * Time.deltaTime);
            }
        }
        
        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }

            switch (currentBehaviourStateIndex)
            {
                case 0:
                    agent.speed = 0f;
                    /*if (TargetFrozenPlayerInRange(ActivationRadius))
                    {
                        logger.LogDebug("Start Killing Players");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }*/
                    /*if (FoundClosestPlayerInRange(config956Radius.Value)) // Testing
                    {
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }*/
                    break;

                case 1:
                    agent.speed = 1f;
                    if (!TargetFrozenPlayerInRange(config956Radius.Value))
                    {
                        logger.LogDebug("Stop Killing Players");
                        SwitchToBehaviourClientRpc((int)State.Dormant);
                        return;
                    }
                    /*if (!FoundClosestPlayerInRange(config956Radius.Value))
                    {
                        logger.LogDebug("Stop Killing Players");
                        SwitchToBehaviourClientRpc((int)State.Dormant);
                        return;
                    }*/
                    MoveToPlayer();
                    break;

                case 2:
                    logger.LogDebug("In HeadButtAttackInProgress Behaviour State");
                    //agent.speed = 0f;
                    break;
            }
        }

        public IEnumerator HeadbuttAttack()
        {
            SwitchToBehaviourClientRpc((int)State.HeadButtAttackInProgress);
            
            yield return new WaitForSeconds(3f);
            logger.LogDebug("Headbutting");
            creatureAnimator.SetTrigger("headButt");
            
            yield return new WaitForSeconds(0.4f);
            targetPlayer.DamagePlayer(50, hasDamageSFX: true, callRPC: true, CauseOfDeath.Unknown, 0);
            if (targetPlayer.isPlayerDead && config956Behavior.Value == 2) { targetPlayer = null; creatureVoice.PlayOneShot(PlayerDeathsfx); } else { targetPlayer.movementAudio.PlayOneShot(BoneCracksfx); }
            //yield return new WaitForSeconds(1f);
            if (currentBehaviourStateIndex != (int)State.HeadButtAttackInProgress)
            {
                yield break;
            }
            SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
        }
        
        bool TargetFrozenPlayerInRange(float range)
        {
            targetPlayer = null;
            List<ulong> PlayersToDie = UnfortunatePlayers.Value;
            if (PlayersToDie == null || !IsOwner) { return false; }
            if (PlayersToDie.Count > 0)
            {
                foreach (ulong id in PlayersToDie)
                {
                    PlayerControllerB player = id.GetPlayerFromId();
                    if (Vector3.Distance(transform.position, player.transform.position) < range && PlayerIsTargetable(player))
                    {
                        targetPlayer = player;
                        return true;
                    }
                }
            }
            return false;
        }

        bool FoundClosestPlayerInRange(float range)
        {
            if (!IsOwner) { return false; }
            TargetClosestPlayer(bufferDistance: 1.5f, requireLineOfSight: false);
            return targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.transform.position) < range;
        }

        void MoveToPlayer()
        {
            if (targetPlayer == null || !IsOwner)
            {
                return;
            }
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) <= 3f)
            {
                logger.LogDebug("Headbutt Attack");
                //moveTowardsDestination = false; // TODO: Find a better way to do this
                StartCoroutine(HeadbuttAttack());
                //return;
            }

            if (timeSinceNewRandPos > 1.5f)
            {
                timeSinceNewRandPos = 0;
                Vector3 positionInFrontPlayer = (targetPlayer.transform.forward * 2.8f) + targetPlayer.transform.position;
                SetDestinationToPosition(positionInFrontPlayer, checkForPath: false);
            }
            //agent.isStopped = true;;
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            return;
        }

        public override void HitEnemy(int force = 0, PlayerControllerB? playerWhoHit = null, bool playHitSFX = true, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        }

        public void Teleport(Vector3 teleportPos)
        {
            serverPosition = teleportPos;
            transform.position = teleportPos;
            agent.Warp(teleportPos);
            SyncPositionToClients();
        }
    }
}
// TODO: Make pinata die from explosions
// TODO: Animation for pinata dying
// TODO: Make pinata a scrap object that spawns but when conditions are met, it turns into an enemy??