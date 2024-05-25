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
using UnityEngine.UI;
using System.Linq;
using System.Runtime.CompilerServices;
using static Netcode.Transports.Facepunch.FacepunchTransport;
using SCP956.Patches;

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

            if (targetPlayer != null && state == (int)State.MovingTowardsPlayer)
            {
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 0.8f * Time.deltaTime);
                targetPlayer.turnCompass.LookAt(transform.position);
                targetPlayer.transform.rotation = Quaternion.Lerp(targetPlayer.transform.rotation, Quaternion.Euler(new Vector3(0f, targetPlayer.turnCompass.eulerAngles.y, 0f)), 0.8f * Time.deltaTime);
            }
            if (targetPlayer != null && state == (int)State.HeadButtAttackInProgress)
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
                    if (TargetFrozenPlayerInRange(config956Radius.Value))
                    {
                        logger.LogDebug("Start Killing Players");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }
                    break;

                case 1:
                    agent.speed = 1f;
                    if (!TargetFrozenPlayerInRange(config956Radius.Value))
                    {
                        logger.LogDebug("Stop Killing Players");
                        SwitchToBehaviourClientRpc((int)State.Dormant);
                        return;
                    }
                    MoveToPlayer();
                    break;

                case 2:
                    break;
            }
        } // TODO: Remove all unneeded comments and todos before publishing

        public IEnumerator HeadbuttAttack()
        {
            SwitchToBehaviourClientRpc((int)State.HeadButtAttackInProgress);
            
            yield return new WaitForSeconds(3f);
            logger.LogDebug("Headbutting");
            DoAnimationClientRpc("headButt");
            
            yield return new WaitForSeconds(0.5f);
            logger.LogDebug($"Damaging player: {targetPlayer.playerUsername}");
            Vector3 playerPos = targetPlayer.transform.position;
            DamageTargetPlayerClientRpc(targetPlayer.actualClientId);

            //yield return new WaitForSeconds(0.5f); // TODO: Check this

            if (targetPlayer.isPlayerDead) 
            { 
                creatureVoice.PlayOneShot(PlayerDeathsfx);

                List<Item> candies = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "CandyRed" || x.itemName == "CandyPink" || x.itemName == "CandyYellow" || x.itemName == "CandyPurple" || x.itemName == "CandyGreen" || x.itemName == "CandyBlue").ToList(); // TODO: make sure candy all shows up and works properly
                int candiesCount = UnityEngine.Random.Range(config9561MinSpawn.Value, config9561MaxSpawn.Value);

                for (int i = 0; i < candiesCount; i++)
                {
                    Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInRadius(playerPos, 1.5f, RoundManager.Instance.navHit);
                    GameObject obj = UnityEngine.Object.Instantiate(candies[UnityEngine.Random.Range(0, 4)].spawnPrefab, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), StartOfRound.Instance.propsContainer);
                    int scrapValue = (int)UnityEngine.Random.Range(config9561MinValue.Value, config9561MaxValue.Value * RoundManager.Instance.scrapValueMultiplier);
                    obj.GetComponent<GrabbableObject>().SetScrapValue(scrapValue);
                    obj.GetComponent<NetworkObject>().Spawn();
                }

                NetworkHandler.Instance.FrozenPlayers.Remove(targetPlayer.actualClientId);
                targetPlayer = null;
            }
            else
            {
                targetPlayer.movementAudio.PlayOneShot(BoneCracksfx);
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
                    if (player == null || player.disconnectedMidGame || player.isPlayerDead) { NetworkHandler.Instance.FrozenPlayers.Remove(id); continue; }
                    if (Vector3.Distance(transform.position, player.transform.position) < range && PlayerIsTargetable(player))
                    {
                        targetPlayer = player;
                        logger.LogDebug(targetPlayer.playerUsername);
                        return true;
                    }
                }
            }
            return false;
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
                return; // TODO: Test this
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
                player.DamagePlayer(50);

                if (player.isPlayerDead) { PlayerControllerBPatch.playerFrozen = false; }
            }
        }
    }
}
// TODO: Make pinata die from explosions
// TODO: Animation for pinata dying
// TODO: Make pinata a scrap object that spawns but when conditions are met, it turns into an enemy??