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
using System.Linq;
using System.Runtime.CompilerServices;

namespace SCP956
{
    class SCP956AI : EnemyAI
    {
        private ManualLogSource logger = LoggerInstance;

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
        // TODO: Figure out how all this works first before changing anything
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
                    if (TargetFrozenPlayerInRange(config956Radius.Value))
                    {
                        logger.LogDebug("Start Killing Players");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }
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
                    break;
            }
        }

        public IEnumerator HeadbuttAttack() // TODO: Headbutt animation doesnt work on other players side alone with not causing damage. learn to use unity netcode patcher!
        {
            SwitchToBehaviourClientRpc((int)State.HeadButtAttackInProgress);
            
            yield return new WaitForSeconds(3f);
            logger.LogDebug("Headbutting");
            DoAnimationClientRpc("headButt");
            
            yield return new WaitForSeconds(0.5f); // TODO: Check this
            targetPlayer.DamagePlayer(50);
            if (targetPlayer.isPlayerDead) 
            { 
                creatureVoice.PlayOneShot(PlayerDeathsfx);

                List<Item> candies = StartOfRound.Instance.allItemsList.itemsList.Where(x => x.itemName == "CandyRed" || x.itemName == "CandyPink" || x.itemName == "CandyYellow" || x.itemName == "CandyPurple").ToList();
                //int candiesCount = PluginInstance.random.Next(config9561MinSpawn.Value, config9561MaxSpawn.Value);
                int candiesCount = UnityEngine.Random.Range(config9561MinSpawn.Value, config9561MaxSpawn.Value);

                for (int i = 0; i < candiesCount; i++)
                {
                    Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInRadius(targetPlayer.transform.position, 1.5f, RoundManager.Instance.navHit);
                    GameObject obj = UnityEngine.Object.Instantiate(candies[UnityEngine.Random.Range(0, 4)].spawnPrefab, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), StartOfRound.Instance.propsContainer);
                    int scrapValue = (int)UnityEngine.Random.Range(config9561MinValue.Value, config9561MaxValue.Value * RoundManager.Instance.scrapValueMultiplier);
                    obj.GetComponent<GrabbableObject>().SetScrapValue(scrapValue);
                    obj.GetComponent<GrabbableObject>().fallTime = 0f;
                    obj.GetComponent<NetworkObject>().Spawn();
                }

                targetPlayer = null;
            }
            else
            {
                targetPlayer.movementAudio.PlayOneShot(BoneCracksfx);
            }
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
            if (PlayersToDie == null/* || !IsOwner*/) { return false; }
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
            if (targetPlayer == null/* || !IsOwner*/)
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

        [ClientRpc]
        public void DoAnimationClientRpc(string animationName) // TODO: Might have to clone example enemy project or copy everything to this to enable networking. this wont play animation for other clients. watch xiaos tutorial video more
        {
            logger.LogDebug("Animation: " + animationName);
            creatureAnimator.SetTrigger(animationName);
        }
    }
}
// TODO: Make pinata die from explosions
// TODO: Animation for pinata dying
// TODO: Make pinata a scrap object that spawns but when conditions are met, it turns into an enemy??