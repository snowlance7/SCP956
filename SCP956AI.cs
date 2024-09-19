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
using LethalLib.Modules;

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

            SetOutsideOrInside();
            //SetEnemyOutsideClientRpc(true);

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
                    if (timeSinceRandTeleport > config956TeleportTime.Value || (timeSinceRandTeleport > 30f && !firstTimeTeleport))
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

        public void SetOutsideOrInside()
        {
            GameObject closestOutsideNode = GetClosestAINode(RoundManager.Instance.outsideAINodes.ToList());
            GameObject closestInsideNode = GetClosestAINode(RoundManager.Instance.insideAINodes.ToList());

            if (Vector3.Distance(transform.position, closestOutsideNode.transform.position) < Vector3.Distance(transform.position, closestInsideNode.transform.position))
            {
                SetEnemyOutsideClientRpc(true);
            }
        }

        public GameObject GetClosestAINode(List<GameObject> nodes)
        {
            float closestDistance = Mathf.Infinity;
            GameObject closestNode = null;
            foreach (GameObject node in nodes)
            {
                float distanceToNode = Vector3.Distance(transform.position, node.transform.position);
                if (distanceToNode < closestDistance)
                {
                    closestDistance = distanceToNode;
                    closestNode = node;
                }
            }
            return closestNode;
        }

        public void FreezeNearbyPlayers()
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!player.isPlayerControlled || player.isPlayerDead)
                {
                    continue;
                }

                if (Vector3.Distance(transform.position, player.transform.position) < activationRadius)
                {
                    if (PlayersRecievedWarning.Contains(player))
                    {
                        //logger.LogDebug($"Skipping player {player.actualClientId}: Player has already received a warning.");
                        continue;
                    }

                    if (FrozenPlayers.Contains(player))
                    {
                        //logger.LogDebug($"Skipping player {player.actualClientId}: Player is already frozen.");
                        continue;
                    }

                    bool holdingCandy = IsPlayerHoldingCandy(player);

                    bool young = YoungPlayers.Contains(player);

                    if (young || holdingCandy)
                    {
                        logger.LogDebug($"Warning player {player.actualClientId}. Young: {young}, Radius: {activationRadius}");
                        WarnPlayerClientRpc(player.actualClientId, young, activationRadius);
                        PlayersRecievedWarning.Add(player);
                    }
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
            DamagePlayerServerRpc(player.actualClientId);
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

        void MoveToTargetPlayer()
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
                SetDestinationToPosition(targetPlayer.transform.position);
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
            bool wasHoldingCandy = false;
            logger.LogDebug($"Starting PlayWarningCoroutine. Young: {young}, Radius: {radius}");

            if (young)
            {
                creatureVoice.clip = WarningShortSFX;
                logger.LogDebug("Set creature voice to WarningShortSFX.");
            }
            else
            {
                creatureVoice.clip = WarningLongSFX;
                wasHoldingCandy = true;
                logger.LogDebug("Set creature voice to WarningLongSFX.");
            }

            creatureVoice.volume = configWarningSoundVolume.Value;
            logger.LogDebug($"Set creature voice volume to {creatureVoice.volume}.");

            creatureVoice.Play();
            logger.LogDebug("Playing creature voice sound.");

            while (creatureVoice.isPlaying)
            {
                float distance = Vector3.Distance(transform.position, localPlayer.transform.position);

                if (distance > radius || (wasHoldingCandy && !IsPlayerHoldingCandy(localPlayer)))
                {
                    outOfRange = true;
                    creatureVoice.Stop();
                    logger.LogDebug("Player out of range. Stopping creature voice.");
                    break;
                }

                yield return null;
            }

            if (outOfRange)
            {
                logger.LogDebug($"Player is out of range. Removing player with ID {localPlayer.actualClientId} from being warned.");
                RemoveFromPlayersBeingWarnedServerRpc(localPlayer.actualClientId);
            }
            else
            {
                logger.LogDebug("Player is in range. Freezing local player and initiating kill after delay.");
                FreezeLocalPlayer(true);
                StatusEffectController.Instance.KillLocalPlayerAfterDelay(configMaxTimeToKillPlayer.Value);
                AddPlayerToFrozenPlayersServerRpc(localPlayer.actualClientId);
            }
        }


        // RPC's

        [ServerRpc(RequireOwnership = false)]
        private void DamagePlayerServerRpc(ulong clientId)
        {
            if (localPlayer.actualClientId == clientId)
            {
                localPlayer.DamagePlayer(configHeadbuttDamage.Value, true, true, CauseOfDeath.Bludgeoning, 7);
            }
        }

        [ClientRpc]
        private void SetEnemyOutsideClientRpc(bool value)
        {
            SetEnemyOutside(value);
        }

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
                PlayerControllerB player = NetworkHandler.PlayerFromId(clientId);
                PlayersRecievedWarning.Remove(player);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void AddPlayerToFrozenPlayersServerRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                PlayerControllerB player = NetworkHandler.PlayerFromId(clientId);
                FrozenPlayers.Add(player);
                PlayersRecievedWarning.Remove(player);
            }
        }
    }
}
// TODO: Death animation