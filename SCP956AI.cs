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
            logger.LogDebug("In DoAIInterval()");
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            };
            logger.LogDebug("Start switch");
            switch (currentBehaviourStateIndex)
            {
                case (int)State.Dormant:
                    //logger.LogDebug("Dormant");
                    agent.speed = 0f;
                    /*List<ulong> PlayersToKill = NetworkHandler.UnfortunatePlayers.Value;
                    if (PlayersToKill.Count > 0)
                    {
                        //if (PlayersToKill) // TODO: Continue here
                        logger.LogDebug("Start Killing Players");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                    }*/
                    if (FoundClosestPlayerInRange(10f)) // Testing
                    {
                        logger.LogDebug("Start Target Player");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        //movingTowardsTargetPlayer = true;
                        return;
                    }
                    break;

                case (int)State.MovingTowardsPlayer: // TODO: Figure out how to make enemy move to in front of the player to perform the headbutt attack, use attack area? USE WHEN DISTANCE IS EQUAL TO A CERTAIN POINT NEAR PLAYER THATS HOW
                    agent.speed = 1f;
                    if (!FoundClosestPlayerInRange(10f))
                    {
                        logger.LogDebug("Stop Target Player");
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
                    agent.speed = 10f;
                    // We don't care about doing anything here
                    break;

                default:
                    logger.LogDebug("This Behavior State doesn't exist!");
                    break;
            }
        }

        public IEnumerator HeadbuttAttack()
        {
            SwitchToBehaviourClientRpc((int)State.HeadButtAttackInProgress);
            Vector3 initialPos = transform.position;
            StalkPos = targetPlayer.transform.position;
            yield return new WaitForSeconds(5f);
            SetDestinationToPosition(targetPlayer.transform.position, checkForPath: false);
            yield return new WaitForSeconds(3f);
            SetDestinationToPosition(initialPos, checkForPath: false);
            /*if (isEnemyDead)
            {
                yield break;
            }*/
            //DoAnimationClientRpc("swingAttack");
            yield return new WaitForSeconds(3f);
            //SwingAttackHitClientRpc();
            // In case the player has already gone away, we just yield break (basically same as return, but for IEnumerator)
            /*if (currentBehaviourStateIndex != (int)State.HeadButtAttackInProgress)
            {
                yield break;
            }*/
            SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
        }
        
        bool TargetFrozenPlayerInRange(float range)
        {
            targetPlayer = null;
            if (UnfortunatePlayers.Value.Count > 0)
            {
                foreach (ulong id in UnfortunatePlayers.Value)
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

                    //StopCoroutine(SwingAttack());
                    // We need to stop our search coroutine, because the game does not do that by default.
                    StopCoroutine(searchCoroutine);
                    KillEnemyOnOwnerClient();
                }
            }
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