using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx.Logging;
using GameNetcodeStuff;
using LethalLib;
using SCP956;
using Unity.Netcode;
using UnityEngine;
using static SCP956.Plugin;
using UnityEngine.UI;
using System.Linq;
using System.Runtime.CompilerServices;
using static Netcode.Transports.Facepunch.FacepunchTransport;
using SCP956.Patches;
using System.Drawing;

namespace SCP956
{
    class SCP956AI : EnemyAI
    {
        private static ManualLogSource logger = LoggerInstance;

        #pragma warning disable 0649
        public Transform turnCompass = null!;
        public AudioClip BoneCrackSFX = null!;
        public AudioClip PlayerDeathSFX = null!;
#pragma warning restore 0649

        bool isDeadAnimationDone;
        float timeSinceNewPos;
        float timeSinceRandTeleport;
        float activationRadius;

        enum State
        {
            Dormant,
            MovingTowardsPlayer,
            HeadButtAttackInProgress
        }

        public override void Start()
        {
            base.Start();
            logger.LogDebug("SCP-956 Spawned");
            isDeadAnimationDone = false;
            activationRadius = config956ActivationRadius.Value;

            if (isOutside)
            {
                SetEnemyOutside(true);
            }

            currentBehaviourStateIndex = (int)State.Dormant;
            RoundManager.Instance.SpawnedEnemies.Add(this);
        }

        public override void Update()
        {
            base.Update();

            timeSinceNewPos += Time.deltaTime;
            timeSinceRandTeleport += Time.deltaTime;
            //logger.LogDebug($"Time since rand teleport: {timeSinceRandTeleport}");

            if (targetPlayer != null)
            {
                if (currentBehaviourStateIndex == (int)State.MovingTowardsPlayer)
                {
                    //turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                    //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 0.8f * Time.deltaTime);

                    if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position, 60f))
                    {
                        GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.2f);
                    }
                }
                else if (currentBehaviourStateIndex == (int)State.HeadButtAttackInProgress)
                {
                    agent.speed = 0f;
                    turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime);
                }
            }
        }
        
        public override void DoAIInterval()
        {
            //logger.LogDebug("Do AI Interval");
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }

            if (IsAnyPlayerLookingAtMe())
            {
                timeSinceRandTeleport = 0;
            }

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Dormant:
                    agent.speed = 0f;
                    if (TargetFrozenPlayerInRange(activationRadius))
                    {
                        logger.LogDebug("Start Killing Player");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }
                    if (timeSinceRandTeleport > config956TeleportTime.Value) // TODO: This is not working as intended, repeats "teleporting"
                    {
                        logger.LogDebug("Teleporting");
                        //Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(transform.position, config956TeleportRange.Value, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
                        
                        Teleport(pos);
                        timeSinceRandTeleport = 0;
                    }
                    break;

                case (int)State.MovingTowardsPlayer:
                    agent.speed = 1f;
                    agent.stoppingDistance = 3f;
                    timeSinceRandTeleport = 0;
                    if (!TargetFrozenPlayerInRange(config956ActivationRadius.Value))
                    {
                        logger.LogDebug("Stop Killing Players");
                        SwitchToBehaviourClientRpc((int)State.Dormant);
                        return;
                    }
                    MoveToTargetPlayer();
                    break;

                case (int)State.HeadButtAttackInProgress:
                    agent.speed = 0f;
                    timeSinceRandTeleport = 0;
                    break;
            }
        }

        public void TeleportToRandomPlayer()
        {
            
        }

        public IEnumerator HeadbuttAttack()
        {
            SwitchToBehaviourClientRpc((int)State.HeadButtAttackInProgress);
            PlayerControllerB player = targetPlayer;
            Vector3 playerPos = player.transform.position;

            yield return new WaitForSeconds(3f);
            logger.LogDebug("Headbutting");
            DoAnimationClientRpc("headButt");
            
            yield return new WaitForSeconds(0.5f);
            logger.LogDebug($"Damaging player: {targetPlayer.playerUsername}");
            DamageTargetPlayerClientRpc(player.actualClientId);
            creatureSFX.PlayOneShot(BoneCrackSFX, 1f);

            yield return new WaitForSeconds(0.5f);

            if (player.isPlayerDead) 
            { 
                creatureVoice.PlayOneShot(PlayerDeathSFX, 1f);

                logger.LogDebug("Player died, spawning candy");
                int candiesCount = UnityEngine.Random.Range(configCandyMinSpawn.Value, configCandyMaxSpawn.Value);

                for (int i = 0; i < candiesCount; i++)
                {
                    Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInRadius(playerPos, 1.5f, RoundManager.Instance.navHit);
                    NetworkHandler.Instance.SpawnItemServerRpc(0, CandyNames[UnityEngine.Random.Range(0, CandyNames.Count)], 0, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 361f), 0f), false);
                }

                FrozenPlayers.Remove(player);

                targetPlayer = null;
            }
            if (currentBehaviourStateIndex != (int)State.HeadButtAttackInProgress)
            {
                yield break;
            }
            SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
        }

        bool TargetFrozenPlayerInRange(float range)
        {
            targetPlayer = null;
            if (FrozenPlayers == null) { return false; }
            if (FrozenPlayers.Count > 0)
            {
                foreach (PlayerControllerB player in FrozenPlayers)
                {
                    if (player == null || player.disconnectedMidGame || player.isPlayerDead || !player.isPlayerControlled) { FrozenPlayers.Remove(player); continue; }
                    if (Vector3.Distance(transform.position, player.transform.position) < range && PlayerIsTargetable(player))
                    {
                        targetPlayer = player;
                    }
                }
            }
            return targetPlayer != null;
        }

        void MoveToTargetPlayer() // TODO: Test this more
        {
            if (targetPlayer == null) { return; }

            if (Vector3.Distance(transform.position, targetPlayer.transform.position) <= 3f)
            {
                logger.LogDebug("Headbutt Attack");
                StartCoroutine(HeadbuttAttack());
                return;
            }

            if (timeSinceNewPos > 1.5f)
            {
                timeSinceNewPos = 0;
                //Vector3 positionInFrontPlayer = (targetPlayer.transform.forward * 2.9f) + targetPlayer.transform.position;
                //SetDestinationToPosition(positionInFrontPlayer, checkForPath: false);
                SetDestinationToPosition(targetPlayer.transform.position, checkForPath: true); // TODO: This isnt working, doesnt go to player, just stays still and then headbutts player. adjust stopping distance? otherwise switch back to original method
            }
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            return;
        }

        public override void HitFromExplosion(float distance)
        {
            base.HitFromExplosion(distance);
            KillEnemy(true);
        }

        public override void HitEnemy(int force = 0, PlayerControllerB playerWhoHit = null, bool playHitSFX = true, int hitID = -1)
        {
            base.HitEnemy(0, playerWhoHit, playHitSFX, hitID);
        }

        public void Teleport(Vector3 teleportPos)
        {
            serverPosition = teleportPos;
            transform.position = teleportPos;
            agent.Warp(teleportPos);
            SyncPositionToClients();
        }

        public bool IsAnyPlayerLookingAtMe()
        {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerControlled && player.HasLineOfSightToPosition(transform.position, 45f, 60, activationRadius * 2.5f))
                {
                    return true;
                }
            }
            return false;
        }

        // RPC's

        [ClientRpc]
        private void DoAnimationClientRpc(string animationName)
        {
            logger.LogDebug("Animation: " + animationName);
            creatureAnimator.SetTrigger(animationName);
        }

        [ClientRpc]
        private void DamageTargetPlayerClientRpc(ulong clientId)
        {
            PlayerControllerB player = StartOfRound.Instance.localPlayerController;
            if (player.actualClientId == clientId)
            {
                player.DamagePlayer(configHeadbuttDamage.Value);

                if (player.isPlayerDead) { PlayerControllerBPatch.playerFrozen = false; }
            }
        }
    }
}
// TODO: Death animation