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
        #pragma warning restore 0649
        bool isDeadAnimationDone;
        float timeSinceNewRandPos;
        float timeSinceRandTeleport;
        float activationRadius;
        bool secretLabMode;

        List<PlayerControllerB> PlayersUnder12 = new List<PlayerControllerB>();
        bool warningStarted = false;

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
            secretLabMode = configSecretLab.Value;

            currentBehaviourStateIndex = (int)State.Dormant;
            RoundManager.Instance.SpawnedEnemies.Add(this);
        }

        public override void Update()
        {
            base.Update();
            if (isEnemyDead)
            {
                if (!isDeadAnimationDone)
                {
                    logger.LogDebug("Stopping enemy voice with janky code.");
                    isDeadAnimationDone = true;
                    creatureVoice.Stop();
                    creatureVoice.PlayOneShot(dieSFX);
                }
                return;
            }
            timeSinceNewRandPos += Time.deltaTime;
            if (secretLabMode) { timeSinceRandTeleport += Time.deltaTime; }
            //logger.LogDebug($"Time since rand teleport: {timeSinceRandTeleport}");
            
            var state = currentBehaviourStateIndex;

            if (targetPlayer != null && state == (int)State.MovingTowardsPlayer)
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 0.8f * Time.deltaTime);

                if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position, 60f))
                {
                    GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.2f);
                }
            }
            else if (targetPlayer != null && state == (int)State.HeadButtAttackInProgress)
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime);
            }
        }
        
        public override void DoAIInterval()
        {
            logger.LogDebug("Do AI Interval");
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            {
                return;
            }

            if (secretLabMode && IsAnyPlayerLookingAtMe())
            {
                timeSinceRandTeleport = 0;
            }

            /*foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!PlayerIsTargetable(player)) { continue; }
                

                if (PlayersUnder12.Contains(player)
                    || )
                {

                }
            }*/

            if (PlayersMeetingConditions().Count > 0)
            {
                logger.LogDebug("Player meets conditions");
            } // TODO: Figure out how to play the sound file for the players in range and find out when it stops playing and stuff, may have to go back to using the player controller patch again
            

            switch (currentBehaviourStateIndex)
            {
                case (int)State.Dormant:
                    agent.speed = 0f;
                    if (TargetFrozenPlayerInRange(config956ActivationRadius.Value))
                    {
                        logger.LogDebug("Start Killing Player");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }
                    if (configSecretLab.Value && timeSinceRandTeleport > config956TeleportTime.Value) // TODO: Test this more
                    {
                        logger.LogDebug("Teleporting");
                        Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(transform.position, config956TeleportRange.Value, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
                        Teleport(pos);
                        timeSinceRandTeleport = 0;
                    }
                    break;

                case (int)State.MovingTowardsPlayer:
                    agent.speed = 0.5f;
                    timeSinceRandTeleport = 0;
                    if (!TargetFrozenPlayerInRange(config956ActivationRadius.Value))
                    {
                        logger.LogDebug("Stop Killing Players");
                        SwitchToBehaviourClientRpc((int)State.Dormant);
                        return;
                    }
                    MoveToPlayer();
                    break;

                case (int)State.HeadButtAttackInProgress:
                    timeSinceRandTeleport = 0;
                    break;
            }
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
            creatureSFX.PlayOneShot(BoneCracksfx);

            yield return new WaitForSeconds(0.5f);

            if (player.isPlayerDead) 
            { 
                creatureVoice.PlayOneShot(PlayerDeathsfx);

                logger.LogDebug("Player died, spawning candy");
                int candiesCount = UnityEngine.Random.Range(configCandyMinSpawn.Value, configCandyMaxSpawn.Value);

                for (int i = 0; i < candiesCount; i++)
                {
                    Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInRadius(playerPos, 1.5f, RoundManager.Instance.navHit);
                    NetworkHandler.Instance.SpawnItemServerRpc(0, CandyNames[UnityEngine.Random.Range(0, CandyNames.Count)], 0, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 361f), 0f), false, true);
                }

                //NetworkHandler.Instance.FrozenPlayers.Remove(player.actualClientId);
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
            //SwitchToBehaviourServerRpc((int)State.HeadButtAttackInProgress); // TODO: Use this
            targetPlayer = null;


            /*if (NetworkHandler.Instance.FrozenPlayers == null) { return false; } // TODO: Check chatgpt and learn more about making freezing players more modular and decoupleable
            if (NetworkHandler.Instance.FrozenPlayers.Count > 0)
            {
                foreach (ulong id in NetworkHandler.Instance.FrozenPlayers)
                {
                    PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[id]];
                    if (player == null || player.disconnectedMidGame || player.isPlayerDead || !player.isPlayerControlled) { NetworkHandler.Instance.FrozenPlayers.Remove(id); continue; }
                    if (Vector3.Distance(transform.position, player.transform.position) < range && PlayerIsTargetable(player))
                    {
                        targetPlayer = player;
                    }
                }
            }
            if (PlayerControllerBPatch.playerFrozen)
            {
                targetPlayer = localPlayer;
            }*/


            return targetPlayer != null;
        }

        void MoveToPlayer()
        {
            if (targetPlayer == null)
            {
                return;
            }
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) <= 3f)
            {
                logger.LogDebug("Headbutt Attack");
                StartCoroutine(HeadbuttAttack());
                return;
            }

            if (timeSinceNewRandPos > 1.5f)
            {
                timeSinceNewRandPos = 0;
                Vector3 positionInFrontPlayer = (targetPlayer.transform.forward * 2.9f) + targetPlayer.transform.position;
                SetDestinationToPosition(positionInFrontPlayer, checkForPath: false);
            }
        }

        public List<PlayerControllerB> PlayersMeetingConditions()
        {
            List<PlayerControllerB> players = new List<PlayerControllerB>();
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (PlayerIsTargetable(player))
                {
                    if ((player.thisPlayerBody.localScale.x < 1f && player.thisPlayerBody.localScale.y < 1f && player.thisPlayerBody.localScale.z < 1f) || configTargetAllPlayers.Value || (configSecretLab.Value && IsPlayerHoldingCandy(player))) // TODO: See if this works on the client and server
                    {
                        if (Vector3.Distance(transform.position, player.transform.position) <= config956ActivationRadius.Value)
                        {
                            players.Add(player);
                        }
                    }
                }
            }
            return players;
        }

        /*private static bool IsTimeUp()
        {
            if (!_audioSource.isPlaying) { return true; }

            if (configSecretLab.Value)
            {
                if (_audioSource.clip.length < 10) // Player is child
                {
                    if (_audioSource.time >= 2.5f) { return true; }
                }
                else // Player is holding candy
                {
                    if (_audioSource.time >= 20f) { return true; }
                }
            }

            return false;
        }*/

          /*if (!configEnablePinata.Value) { return; } // Method from PlayerControllerBPatch
            timeSinceLastCheck += Time.deltaTime;
            if (timeSinceLastCheck > 0.5f)
            {
                timeSinceLastCheck = 0f;

                if (playerFrozen) { return; }

                if (StartOfRound.Instance == null || localPlayer == null || !localPlayer.isPlayerControlled || localPlayer.isPlayerDead)
                {
                    if (playerFrozen) { playerFrozen = false; }
                    return;
                }


                //AudioSource _audioSource = HUDManager.Instance.UIAudio;

                if (_audioSource == null) { logger.LogError("AudioSource is null"); return; }

                if (PlayerMeetsConditions()) // TODO: Test and make sure this is working without other mods
                {
                    logger.LogDebug("Player meets conditions"); // Temp
                    if (!warningStarted)
                    {
                        if (WarningSoundShortsfx == null || WarningSoundLongsfx == null) { logger.LogError("Warning sounds not set!"); return; }
                        if (!(PlayerAge < 12) && configSecretLab.Value && IsPlayerHoldingCandy(localPlayer)) { _audioSource.clip = WarningSoundLongsfx; } else { _audioSource.clip = WarningSoundShortsfx; }
                        if (!configPlayWarningSound.Value) { _audioSource.volume = 0f; } else { _audioSource.volume = 1f; }
                        _audioSource.loop = false;
                        _audioSource.Play();

                        warningStarted = true;
                        logger.LogDebug("Warning started");
                    }

                    if (warningStarted && IsTimeUp())
                    {
                        logger.LogDebug("Audio stopped");
                        // Freeze localPlayer
                        playerFrozen = true;
                        warningStarted = false;
                        NetworkHandler.Instance.AddToFrozenPlayersListServerRpc(localPlayer.actualClientId);

                        IngamePlayerSettings.Instance.playerInput.DeactivateInput();
                        localPlayer.disableLookInput = true;
                        if (localPlayer.currentlyHeldObject != null) { localPlayer.DropItemAheadOfPlayer(); }
                    }
                }
                else if (warningStarted)
                {
                    logger.LogDebug("Warning ended");
                    warningStarted = false;
                    _audioSource.Stop();
                }
            }*/

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
                if (player.isPlayerControlled && player.HasLineOfSightToPosition(transform.position, 45f, 60, config956SpawnRadius.Value))
                {
                    return true;
                }
            }
            return false;
        }

        // RPC's

        [ServerRpc(RequireOwnership = false)]
        private void TargetPlayerServerRpc(ulong clientId)
        {
            /*if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                
            }*/
        }

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