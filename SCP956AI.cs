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
using Unity.Netcode.Components;

namespace SCP956
{
    class SCP956AI : EnemyAI
    {
        private static ManualLogSource logger = LoggerInstance;

        #pragma warning disable 0649
        public Transform turnCompass = null!;
        public AudioClip BoneCrackSFX = null!;
        public AudioClip PlayerDeathSFX = null!;
        public AudioClip WarningShortSFX = null!;
        public AudioClip WarningLongSFX = null!;
#pragma warning restore 0649

        float timeSinceNewPos;
        float timeSinceRandTeleport;
        float activationRadius;
        bool firstTimeTeleport;

        public static List<PlayerControllerB> YoungPlayers = new List<PlayerControllerB>();
        public static List<PlayerControllerB> PlayersRecievedWarning = new List<PlayerControllerB>();
        public static List<PlayerControllerB> FrozenPlayers = new List<PlayerControllerB>();

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

                    if (localPlayer.HasLineOfSightToPosition(transform.position, 60f))
                    {
                        localPlayer.IncreaseFearLevelOverTime(0.2f);
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

            FreezeNearbyPlayers();

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
                    if (timeSinceRandTeleport > config956TeleportTime.Value || (timeSinceRandTeleport > 30f && !firstTimeTeleport)) // TODO: This is not working as intended, repeats "teleporting"
                    {
                        logger.LogDebug("Teleporting");
                        firstTimeTeleport = true;
                        if (config956TeleportNearPlayers.Value)
                        {
                            TeleportToRandomPlayer();
                        }
                        else
                        {
                            Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(transform.position, config956TeleportRange.Value, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
                            Teleport(pos);
                        }
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

        public void FreezeNearbyPlayers()
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!player.isPlayerControlled || player.isPlayerDead) { continue; }
                if (PlayersRecievedWarning.Contains(player) || FrozenPlayers.Contains(player)) { continue; }

                bool holdingCandy = IsPlayerHoldingCandy(player);
                bool young = YoungPlayers.Contains(player);

                if (young || holdingCandy)
                {
                    WarnPlayerClientRpc(player.actualClientId, young, activationRadius);
                    PlayersRecievedWarning.Add(player);
                }
            }
        }

        public void TeleportToRandomPlayer()
        {
            List<PlayerControllerB> players = new List<PlayerControllerB>();
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (YoungPlayers.Contains(player) || IsPlayerHoldingCandy(player))
                {
                    players.Add(player);
                }
            }

            int randomIndex = UnityEngine.Random.Range(0, players.Count);
            PlayerControllerB player2 = players[randomIndex];
            Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(player2.transform.position, config956TeleportRange.Value, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
            Teleport(pos);
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
            player.DamagePlayer(configHeadbuttDamage.Value, true, true, CauseOfDeath.Bludgeoning, 7);
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
                SetDestinationToPosition(targetPlayer.transform.position); // TODO: This isnt working, doesnt go to player, just stays still and then headbutts player. adjust stopping distance? otherwise switch back to original method
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

        public IEnumerator PlayWarningCoroutine(bool young, float radius)
        {
            bool outOfRange = false;
            if (young) { creatureVoice.clip = WarningShortSFX; }
            else { creatureVoice.clip = WarningLongSFX; }
            creatureVoice.volume = configWarningSoundVolume.Value;
            creatureVoice.Play();

            while (creatureVoice.isPlaying)
            {
                if (Vector3.Distance(transform.position, localPlayer.transform.position) > radius)
                {
                    outOfRange = true;
                    creatureVoice.Stop();
                    break;
                }

                yield return null;
            }

            if (outOfRange)
            {
                RemoveFromPlayersBeingWarnedServerRpc(localPlayer.actualClientId);
            }
            else
            {
                FreezeLocalPlayer(true);
                KillLocalPlayerAfterDelay(configMaxTimeToKillPlayer.Value);
                AddPlayerToFrozenPlayersServerRpc(localPlayer.actualClientId);
            }
        }

        // RPC's

        [ClientRpc]
        private void DoAnimationClientRpc(string animationName)
        {
            logger.LogDebug($"Doing animation: {animationName}");
            creatureAnimator.SetTrigger(animationName);
        }

        [ClientRpc]
        private void WarnPlayerClientRpc(ulong clientId, bool young, float radius)
        {
            if (localPlayer.actualClientId == clientId)
            {
                StartCoroutine(PlayWarningCoroutine(young, radius));
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RemoveFromPlayersBeingWarnedServerRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                PlayersRecievedWarning.Remove(NetworkHandler.PlayerFromId(clientId));
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void AddPlayerToFrozenPlayersServerRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                FrozenPlayers.Add(NetworkHandler.PlayerFromId(clientId));
            }
        }
    }
}
// TODO: Death animation