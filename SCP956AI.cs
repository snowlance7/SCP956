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
        public Transform attackArea = null!;
        #pragma warning restore 0649
        float timeSinceHittingLocalPlayer;
        Vector3 positionRandomness;
        Vector3 StalkPos;
        System.Random enemyRandom = null!;
        bool isDeadAnimationDone;
        float timeSinceNewRandPos;

        public static int Behavior;
        public static float ActivationRadius;

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
            // TODO: figure out how to tell if enemy spawned from a vent or not
            logger.LogDebug("SCP-956 Spawned");
            timeSinceHittingLocalPlayer = 0;
            timeSinceNewRandPos = 0;
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            //isDeadAnimationDone = false;
            // NOTE: Add your behavior states in your enemy script in Unity, where you can configure fun stuff
            // like a voice clip or an sfx clip to play when changing to that specific behavior state.
            currentBehaviourStateIndex = (int)State.Dormant;
            // We make the enemy start searching. This will make it start wandering around.
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
            timeSinceNewRandPos += Time.deltaTime;

            var state = currentBehaviourStateIndex;
            if (targetPlayer != null && (state == (int)State.MovingTowardsPlayer/* || state == (int)State.HeadButtAttackInProgress*/))
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 0.8f * Time.deltaTime);
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
                case (int)State.Dormant:
                    //logger.LogDebug("Dormant");
                    agent.speed = 0f;
                    /*if (TargetFrozenPlayerInRange(ActivationRadius))
                    {
                        logger.LogDebug("Start Killing Players");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }*/
                    if (FoundClosestPlayerInRange(ActivationRadius)) // Testing
                    {
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }
                    break;

                case (int)State.MovingTowardsPlayer: // TODO: Figure out how to make enemy move to in front of the player to perform the headbutt attack, use attack area? USE WHEN DISTANCE IS EQUAL TO A CERTAIN POINT NEAR PLAYER THATS HOW
                    agent.speed = 1f;
                    if (!TargetFrozenPlayerInRange(ActivationRadius))
                    {
                        logger.LogDebug("Stop Killing Players");
                        SwitchToBehaviourClientRpc((int)State.Dormant);
                        return;
                    }
                    /*if (!TargetClosestPlayerInAnyCase() || (Vector3.Distance(transform.position, targetPlayer.transform.position) > 20 && !CheckLineOfSightForPosition(targetPlayer.transform.position)))
                    {
                        logger.LogDebug("Stop Target Player");
                        StartSearch(transform.position);
                        SwitchToBehaviourClientRpc((int)State.SearchingForPlayer);
                        return;
                    }*/
                    MoveToPlayer();
                    break;

                case (int)State.HeadButtAttackInProgress:
                    //logger.LogDebug("Headbutt Attack In Progress");
                    agent.speed = 0f;
                    
                    // We don't care about doing anything here
                    break;
                    
                default:
                    logger.LogDebug("This Behavior State doesn't exist!");
                    break;
            }
        }

        public IEnumerator HeadbuttAttack()
        {
            yield return new WaitForSeconds(3f);
            creatureAnimator.SetTrigger("headButt");
            targetPlayer.DamagePlayer(50, true, true, CauseOfDeath.Unknown, 0);
            /*if (isEnemyDead)
            {
                yield break;
            }*/
            //DoAnimationClientRpc("swingAttack");
            yield return new WaitForSeconds(1f);
            //SwingAttackHitClientRpc();
            // In case the player has already gone away, we just yield break (basically same as return, but for IEnumerator)
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
                agent.speed = 0f;
                SwitchToBehaviourClientRpc((int)State.HeadButtAttackInProgress);
                StartCoroutine(HeadbuttAttack());
                return;
            }
            if (timeSinceNewRandPos > 1f)
            {
                timeSinceNewRandPos = 0;
                SetDestinationToPosition(targetPlayer.transform.position, checkForPath: false);
            }
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            return;
            if (timeSinceHittingLocalPlayer < 1f)
            {
                return;
            }
            PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
            if (playerControllerB != null)
            {
                logger.LogDebug("Collision with Player!");
                timeSinceHittingLocalPlayer = 0f;
                playerControllerB.DamagePlayer(20);
            }
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