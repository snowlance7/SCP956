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
        public AudioClip metalDoorSmashSFX = null!;
        public AudioClip bashSFX = null!;
        public DoorCollisionDetect doorCollisionDetectScript = null!;
#pragma warning restore 0649

        float timeSinceRandTeleport;
        bool firstTimeTeleport;
        bool bashingDoor;

        public static List<PlayerControllerB> YoungPlayers = new List<PlayerControllerB>();
        public static List<PlayerControllerB> PlayersRecievedWarning = new List<PlayerControllerB>();
        public static List<PlayerControllerB> FrozenPlayers = new List<PlayerControllerB>();

        DoorLock doorLock = null!;

        // Config Values
        float activationRadius;
        float doorBashForce;
        bool despawnDoorAfterBash;
        float despawnDoorAfterBashTime;

        public enum State
        {
            Dormant,
            MovingTowardsPlayer,
            HeadButtAttackInProgress
        }

        public override void Start()
        {
            base.Start();
            LogIfDebug("SCP-956 Spawned");

            activationRadius = config956ActivationRadius.Value;
            doorBashForce = config956DoorBashForce.Value;
            despawnDoorAfterBash = config956DespawnDoorAfterBash.Value;
            despawnDoorAfterBashTime = config956DespawnDoorAfterBashTime.Value;

            SetOutsideOrInside();
            //SetEnemyOutsideClientRpc(true);

            currentBehaviourStateIndex = (int)State.Dormant;
            RoundManager.Instance.SpawnedEnemies.Add(this);
        }

        public override void Update()
        {
            base.Update();

            timeSinceRandTeleport += Time.deltaTime;
            //LogIfDebug($"Time since rand teleport: {timeSinceRandTeleport}");

            if (currentBehaviourStateIndex == (int)State.MovingTowardsPlayer)
            {
                if (localPlayer.HasLineOfSightToPosition(transform.position, 60f))
                {
                    localPlayer.IncreaseFearLevelOverTime(0.2f);
                }
            }

            if (targetPlayer != null && currentBehaviourStateIndex == (int)State.HeadButtAttackInProgress)
            {
                agent.speed = 0f;
                turnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 10f * Time.deltaTime);
            }
        }
        
        public override void DoAIInterval()
        {
            //LogIfDebug("Do AI Interval");
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
                        LogIfDebug("Start Killing Player");
                        SwitchToBehaviourClientRpc((int)State.MovingTowardsPlayer);
                        return;
                    }
                    if (timeSinceRandTeleport > config956TeleportTime.Value || (timeSinceRandTeleport > 30f && !firstTimeTeleport))
                    {
                        LogIfDebug("Teleporting");
                        firstTimeTeleport = true;
                        if (config956TeleportNearPlayers.Value)
                        {
                            TeleportToRandomPlayer();
                        }
                        else
                        {
                            Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(transform.position, config956TeleportRange.Value, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
                            pos = RoundManager.Instance.GetClosestNode(pos, isOutside).transform.position;
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
                        LogIfDebug("Stop Killing Players");
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

        public void BeginBashDoor(DoorLock _doorLock)
        {
            logger.LogDebug("BeginBashDoor called");
            inSpecialAnimation = true;
            doorLock = _doorLock;
            bashingDoor = true;
            creatureAnimator.SetTrigger("headButt");
        }

        public void BashDoor()
        {
            if (!bashingDoor) { return; }
            bashingDoor = false;

            var steelDoorObj = doorLock.transform.parent.transform.parent.gameObject;
            var doorMesh = steelDoorObj.transform.Find("DoorMesh").gameObject;

            GameObject flyingDoorPrefab = new GameObject("FlyingDoor");
            BoxCollider tempCollider = flyingDoorPrefab.AddComponent<BoxCollider>();
            tempCollider.isTrigger = true;
            tempCollider.size = new Vector3(1f, 1.5f, 3f);

            flyingDoorPrefab.AddComponent<DoorPlayerCollisionDetect>();

            AudioSource tempAS = flyingDoorPrefab.AddComponent<AudioSource>();
            tempAS.spatialBlend = 1;
            tempAS.maxDistance = 60;
            tempAS.rolloffMode = AudioRolloffMode.Linear;
            tempAS.volume = 1f;

            var flyingDoor = UnityEngine.Object.Instantiate(flyingDoorPrefab, doorLock.transform.position, doorLock.transform.rotation);
            doorMesh.transform.SetParent(flyingDoor.transform);

            GameObject.Destroy(flyingDoorPrefab);

            Rigidbody rb = flyingDoor.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = true;
            rb.isKinematic = true;

            // Determine which direction to apply the force
            Vector3 doorForward = flyingDoor.transform.position + flyingDoor.transform.right * 2f;
            Vector3 doorBackward = flyingDoor.transform.position - flyingDoor.transform.right * 2f;
            Vector3 direction;

            if (Vector3.Distance(doorForward, transform.position) < Vector3.Distance(doorBackward, transform.position))
            {
                direction = (doorBackward - doorForward).normalized;
                flyingDoor.transform.position = flyingDoor.transform.position - flyingDoor.transform.right;
            }
            else
            {
                direction = (doorForward - doorBackward).normalized;
                flyingDoor.transform.position = flyingDoor.transform.position + flyingDoor.transform.right;
            }

            Vector3 upDirection = transform.TransformDirection(Vector3.up).normalized * 0.1f;
            Vector3 playerHitDirection = (direction + upDirection).normalized;
            flyingDoor.GetComponent<DoorPlayerCollisionDetect>().force = playerHitDirection * doorBashForce;

            // Release the Rigidbody from kinematic state
            rb.isKinematic = false;

            // Add an impulse force to the door
            rb.AddForce(direction * doorBashForce, ForceMode.Impulse);

            AudioSource doorAudio = flyingDoor.GetComponent<AudioSource>();
            doorAudio.PlayOneShot(bashSFX, 1f);

            string flowType = RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.name;
            if (flowType == "Level1Flow" || flowType == "Level1FlowExtraLarge" || flowType == "Level1Flow3Exits" || flowType == "Level3Flow")
            {
                doorAudio.PlayOneShot(metalDoorSmashSFX, 0.8f);
            }

            doorCollisionDetectScript.triggering = false;
            doorLock = null!;
            inSpecialAnimation = false;

            if (despawnDoorAfterBash)
            {
                Destroy(flyingDoor, despawnDoorAfterBashTime);
            }
        }

        public void SetOutsideOrInside()
        {
            GameObject closestOutsideNode = GetClosestAINode(GameObject.FindGameObjectsWithTag("OutsideAINode").ToList());
            GameObject closestInsideNode = GetClosestAINode(GameObject.FindGameObjectsWithTag("AINode").ToList());

            if (Vector3.Distance(transform.position, closestOutsideNode.transform.position) < Vector3.Distance(transform.position, closestInsideNode.transform.position))
            {
                LogIfDebug("Setting enemy outside");
                SetEnemyOutsideClientRpc(true);
                return;
            }
            LogIfDebug("Setting enemy inside");
        }

        public GameObject GetClosestAINode(List<GameObject> nodes)
        {
            float closestDistance = Mathf.Infinity;
            GameObject closestNode = null!;
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
                if (!PlayerIsTargetable(player, false, true))
                {
                    continue;
                }

                if (Vector3.Distance(transform.position, player.transform.position) < activationRadius)
                {
                    if (PlayersRecievedWarning.Contains(player))
                    {
                        //LogIfDebug($"Skipping player {player.actualClientId}: Player has already received a warning.");
                        continue;
                    }

                    if (FrozenPlayers.Contains(player))
                    {
                        //LogIfDebug($"Skipping player {player.actualClientId}: Player is already frozen.");
                        continue;
                    }

                    bool holdingCandy = IsPlayerHoldingCandy(player);

                    bool young = IsPlayerYoung(player);

                    if (young || holdingCandy)
                    {
                        LogIfDebug($"Warning player {player.actualClientId}. Young: {young}, Radius: {activationRadius}");
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
                if (IsPlayerYoung(player) || IsPlayerHoldingCandy(player))
                {
                    players.Add(player);
                }
            }

            int randomIndex = UnityEngine.Random.Range(0, players.Count);
            PlayerControllerB player2 = players[randomIndex];
            Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(player2.transform.position, config956TeleportRange.Value, RoundManager.Instance.navHit, RoundManager.Instance.AnomalyRandom);
            pos = RoundManager.Instance.GetClosestNode(pos, isOutside).transform.position;
            Teleport(pos);
        }

        public IEnumerator HeadbuttAttack()
        {
            SwitchToBehaviourClientRpc((int)State.HeadButtAttackInProgress);
            PlayerControllerB player = targetPlayer;
            Vector3 playerPos = player.transform.position;

            yield return new WaitForSeconds(3f);
            LogIfDebug("Headbutting");
            DoAnimationClientRpc("headButt");
            
            yield return new WaitForSeconds(0.5f);
            LogIfDebug($"Damaging player: {targetPlayer.playerUsername}");
            DamagePlayerServerRpc(player.actualClientId);
            creatureSFX.PlayOneShot(BoneCrackSFX, 1f);

            yield return new WaitForSeconds(0.5f);

            if (player.isPlayerDead) 
            { 
                creatureVoice.PlayOneShot(PlayerDeathSFX, 1f);

                LogIfDebug("Player died, spawning candy");
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
                LogIfDebug("Headbutt Attack");
                StartCoroutine(HeadbuttAttack());
                return;
            }

            SetDestinationToPosition(targetPlayer.transform.position);
        }

        public override void HitFromExplosion(float distance)
        {
            base.HitFromExplosion(distance);
            if (!inSpecialAnimation && !isEnemyDead)
            {
                KillEnemy(true);
            }
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
            LogIfDebug($"Starting PlayWarningCoroutine. Young: {young}, Radius: {radius}");

            if (young)
            {
                creatureVoice.clip = WarningShortSFX;
                LogIfDebug("Set creature voice to WarningShortSFX.");
            }
            else
            {
                creatureVoice.clip = WarningLongSFX;
                wasHoldingCandy = true;
                LogIfDebug("Set creature voice to WarningLongSFX.");
            }

            creatureVoice.volume = configWarningSoundVolume.Value;
            LogIfDebug($"Set creature voice volume to {creatureVoice.volume}.");

            creatureVoice.Play();
            LogIfDebug("Playing creature voice sound.");

            while (creatureVoice.isPlaying)
            {
                float distance = Vector3.Distance(transform.position, localPlayer.transform.position);

                if (distance > radius || (wasHoldingCandy && !IsPlayerHoldingCandy(localPlayer)))
                {
                    outOfRange = true;
                    creatureVoice.Stop();
                    LogIfDebug("Player out of range. Stopping creature voice.");
                    break;
                }

                yield return null;
            }

            if (outOfRange)
            {
                LogIfDebug($"Player is out of range. Removing player with ID {localPlayer.actualClientId} from being warned.");
                RemoveFromPlayersBeingWarnedServerRpc(localPlayer.actualClientId);
            }
            else
            {
                LogIfDebug("Player is in range. Freezing local player and initiating kill after delay.");
                FreezeLocalPlayer(true);
                StatusEffectController.Instance.KillLocalPlayerAfterDelay(configMaxTimeToKillPlayer.Value);
                AddPlayerToFrozenPlayersServerRpc(localPlayer.actualClientId);
            }
        }


        // RPC's

        [ServerRpc(RequireOwnership = false)]
        private void DamagePlayerServerRpc(ulong clientId) // TODO: May be unneeded
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
            LogIfDebug($"Doing animation: {animationName}");
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
                if (!FrozenPlayers.Contains(player))
                {
                    FrozenPlayers.Add(player);
                }
                PlayersRecievedWarning.Remove(player);
            }
        }
    }
}
// TODO: Death animation