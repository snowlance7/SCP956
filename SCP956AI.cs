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
        //float timeSinceLookedAt;

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

            var state = currentBehaviourStateIndex;

            if (!(GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position, 45f, 60, config956SpawnRadius.Value)/* && state == (int)State.Dormant*/)) // TODO: Testing
            {
                timeSinceRandTeleport += Time.deltaTime;
                logger.LogDebug($"Time since rand teleport: {timeSinceRandTeleport}");
            }

            if (targetPlayer != null && state == (int)State.MovingTowardsPlayer)
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 0.8f * Time.deltaTime);

                if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position/* + Vector3.up * 0.75f*/, 60f/*, 15*/))
                {
                    GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.3f);
                }
            }
            else if (targetPlayer != null && state == (int)State.HeadButtAttackInProgress)
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime);

                if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position/* + Vector3.up * 0.75f*/, 60f/*, 15*/))
                {
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
                }
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
                    agent.speed = 0f;
                    if (TargetFrozenPlayerInRange(config956ActivationRadius.Value))
                    {
                        logger.LogDebug("Start Killing Player");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }
                    break;

                case (int)State.MovingTowardsPlayer:
                    agent.speed = 1f;
                    if (!TargetFrozenPlayerInRange(config956ActivationRadius.Value))
                    {
                        logger.LogDebug("Stop Killing Players");
                        SwitchToBehaviourClientRpc((int)State.Dormant);
                        return;
                    }
                    MoveToPlayer();
                    break;

                case (int)State.HeadButtAttackInProgress:
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
                List<Item> candies = StartOfRound.Instance.allItemsList.itemsList.Where(x => CandyNames.Contains(x.itemName)).ToList();
                logger.LogDebug($"Candy count: {candies.Count}");
                int candiesCount = UnityEngine.Random.Range(configCandyMinSpawn.Value, configCandyMaxSpawn.Value);

                for (int i = 0; i < candiesCount; i++)
                {
                    Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInRadius(playerPos, 1.5f, RoundManager.Instance.navHit);
                    int scrapValue = (int)UnityEngine.Random.Range(configCandyMinValue.Value, configCandyMaxValue.Value * RoundManager.Instance.scrapValueMultiplier);
                    NetworkHandler.Instance.SpawnItemServerRpc(0, candies[UnityEngine.Random.Range(0, 6)].itemName, scrapValue, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 361f), 0f));
                }

                NetworkHandler.Instance.FrozenPlayers.Remove(player.actualClientId);
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
            if (NetworkHandler.Instance.FrozenPlayers == null) { return false; }
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

        public override void OnCollideWithPlayer(Collider other)
        {
            return;
        }

        public override void HitEnemy(int force = 0, PlayerControllerB? playerWhoHit = null, bool playHitSFX = true, int hitID = -1)
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        }

        public void Teleport(Vector3 teleportPos) // Unneeded?
        {
            serverPosition = teleportPos;
            transform.position = teleportPos;
            agent.Warp(teleportPos);
            SyncPositionToClients();
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
// TODO: Make pinata die from explosions
// TODO: Animation for pinata dying
// TODO: Make pinata a scrap object that spawns but when conditions are met, it turns into an enemy??