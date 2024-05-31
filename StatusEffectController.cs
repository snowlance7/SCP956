using BepInEx.Logging;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zeekerss.Core.Singletons;

namespace SCP956
{
    public class StatusEffectController : MonoBehaviour // TODO: Add configs
    {
        private static StatusEffectController _instance;

        public static StatusEffectController Instance
        {
            get
            {
                // If the instance doesn't exist, try to find it in the scene
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StatusEffectController>();

                    // If it's still null, create a new GameObject and add the component
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject("SCP-956 StatusEffectController");
                        _instance = singletonObject.AddComponent<StatusEffectController>();
                    }
                }
                return _instance;
            }
        }

        private static ManualLogSource logger = SCP956.LoggerInstance;

        private PlayerControllerB LocalPlayer
        {
            get
            {
                return StartOfRound.Instance.localPlayerController;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // If an instance already exists and it's not this one, destroy this instance
                if (this != _instance)
                {
                    Destroy(gameObject);
                }
            }
        }

        private Coroutine statusNegationCoroutine;
        private Coroutine damageReductionCoroutine;
        private Coroutine infiniteSprintCoroutine;
        private Coroutine increasedMovementSpeedCoroutine;

        private int statusNegationSeconds = 0;
        private int damageReductionSeconds = 0;
        private int increasedMovementSpeedSeconds = 0;

        public bool statusNegationActive = false;
        public bool damageReductionActive = false;
        public bool infiniteSprintActive = false;

        public float freezeSprintMeter;
        public float movementSpeedMultiplier = 1f;
        public const float baseMovementSpeed = 0.5f;

        public void HealthRegen(int hpPerSecond, int seconds)
        {
            StartCoroutine(HealthRegenCoroutine(hpPerSecond, seconds));
        }

        public void StatusNegation()
        {
            if (statusNegationCoroutine != null)
            {
                statusNegationSeconds += 30;
            }
            else
            {
                statusNegationCoroutine = StartCoroutine(StatusNegationCoroutine());
            }
        }

        public void DamageReduction()
        {
            if (damageReductionCoroutine != null)
            {
                damageReductionSeconds += 15;
            }
            else
            {
                damageReductionCoroutine = StartCoroutine(DamageReductionCoroutine());
            }
        }

        public void InfiniteSprint()
        {
            if (infiniteSprintCoroutine != null)
            {
                StopCoroutine(InfiniteSprintCoroutine());
            }
            infiniteSprintCoroutine = StartCoroutine(InfiniteSprintCoroutine());
        }

        public void IncreasedMovementSpeed()
        {
            if (increasedMovementSpeedCoroutine != null)
            {
                StopCoroutine(IncreasedMovementSpeedCoroutine());
                movementSpeedMultiplier += 0.01f;
                increasedMovementSpeedSeconds += 8;
            }
            increasedMovementSpeedCoroutine = StartCoroutine(IncreasedMovementSpeedCoroutine());
        }

        private IEnumerator HealthRegenCoroutine(int hpPerSecond, int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                if (LocalPlayer.health < 100)
                {
                    logger.LogDebug("HealthRegen: " + hpPerSecond);
                    int newHealth = LocalPlayer.health + hpPerSecond;
                    LocalPlayer.health = newHealth;
                }
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private IEnumerator StatusNegationCoroutine()
        {
            statusNegationSeconds = 30;
            statusNegationActive = true;
            while (statusNegationSeconds > 0)
            {
                statusNegationSeconds--;
                yield return new WaitForSecondsRealtime(1f);
            }
            statusNegationActive = false;
            statusNegationCoroutine = null;
        }

        private IEnumerator DamageReductionCoroutine()
        {
            damageReductionSeconds = 15;
            damageReductionActive = true;
            while (damageReductionSeconds > 0)
            {
                damageReductionSeconds--;
                yield return new WaitForSecondsRealtime(1f);
            }
            damageReductionActive = false;
            damageReductionCoroutine = null;
        }

        private IEnumerator InfiniteSprintCoroutine()
        {
            infiniteSprintActive = true;
            freezeSprintMeter = LocalPlayer.sprintMeter;
            yield return new WaitForSecondsRealtime(8f);
            infiniteSprintActive = false;
            infiniteSprintCoroutine = null;
        }

        private IEnumerator IncreasedMovementSpeedCoroutine()
        {
            LocalPlayer.movementSpeed = baseMovementSpeed * movementSpeedMultiplier;
            while (increasedMovementSpeedSeconds > 0)
            {
                increasedMovementSpeedSeconds--;
                yield return new WaitForSecondsRealtime(1f);
            }

            LocalPlayer.movementSpeed = baseMovementSpeed;
            movementSpeedMultiplier = 1f;
            increasedMovementSpeedCoroutine = null;
        }
    }
}
