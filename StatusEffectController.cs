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

        public int statusNegationSeconds = 0;
        public int damageReductionSeconds = 0;
        public int infiniteSprintSeconds = 0;
        public int increasedMovementSpeedSeconds = 0;

        //public bool statusNegationActive = false;
        //public bool damageReductionActive = false;
        //public bool infiniteSprintActive = false;

        public int damageReductionPercent = 0;
        public int increasedMovementSpeedPercent = 0;

        public float freezeSprintMeter;
        public const float baseMovementSpeed = 0.5f;
        public bool bulletProof = false;
        public int bulletProofMultiplier;

        public void HealPlayer(int amount, bool overHeal = false)
        {
            int newHealth = LocalPlayer.health + amount;
            if ((LocalPlayer.health + newHealth) > 100 && overHeal) { LocalPlayer.health = newHealth; }
            else if (LocalPlayer.health >= 100) { return; }
            else { LocalPlayer.health = 100; }
        }

        public void RestoreStamina(int percent)
        {
            // TODO: Implement this
        }

        public void HealthRegen(int hpPerSecond, int seconds)
        {
            StartCoroutine(HealthRegenCoroutine(hpPerSecond, seconds));
        }

        public void StatusNegation(int seconds, bool timeStackable = false) // TODO: Simplify all of these // TODO: Make sure these dont stop when scene changes
        {
            if (statusNegationCoroutine != null)
            {
                if (timeStackable) { statusNegationSeconds += seconds; return; }
                StopCoroutine(statusNegationCoroutine);
            }
            statusNegationCoroutine = StartCoroutine(StatusNegationCoroutine(seconds));
        }

        public void DamageReduction(int seconds, int percent, bool timeStackable = false, bool stackable = false)
        {
            if (damageReductionCoroutine != null)
            {
                if (timeStackable) { damageReductionSeconds += seconds; }
                if (stackable) { damageReductionPercent += percent; }
                if (timeStackable || stackable) { return; }
                StopCoroutine(damageReductionCoroutine);
            }
            damageReductionCoroutine = StartCoroutine(DamageReductionCoroutine(seconds));
        }

        public void InfiniteSprint(int seconds, bool timeStackable = false)
        {
            if (infiniteSprintCoroutine != null)
            {
                if (timeStackable) { infiniteSprintSeconds += seconds; return; }
                StopCoroutine(infiniteSprintCoroutine);
            }
            infiniteSprintCoroutine = StartCoroutine(InfiniteSprintCoroutine(seconds));
        }

        public void IncreasedMovementSpeed(int seconds, int percent, bool timeStackable = false, bool stackable = false)
        {
            if (increasedMovementSpeedCoroutine != null)
            {
                if (timeStackable) { increasedMovementSpeedSeconds += seconds; }
                if (stackable) { increasedMovementSpeedPercent += percent; }
                if (timeStackable || stackable) { return; }
                StopCoroutine(increasedMovementSpeedCoroutine);
            }
            increasedMovementSpeedCoroutine = StartCoroutine(IncreasedMovementSpeedCoroutine(seconds));
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

        private IEnumerator StatusNegationCoroutine(int seconds)
        {
            statusNegationSeconds = seconds;
            //statusNegationActive = true;
            while (statusNegationSeconds > 0)
            {
                statusNegationSeconds--;
                yield return new WaitForSecondsRealtime(1f);
            }
            //statusNegationActive = false;
            statusNegationCoroutine = null;
        }

        private IEnumerator DamageReductionCoroutine(int seconds)
        {
            damageReductionSeconds = seconds;
            //damageReductionActive = true;
            while (damageReductionSeconds > 0)
            {
                damageReductionSeconds--;
                yield return new WaitForSecondsRealtime(1f);
            }
            //damageReductionActive = false;
            damageReductionPercent = 0;
            damageReductionCoroutine = null;
        }

        private IEnumerator InfiniteSprintCoroutine(int seconds)
        {
            //infiniteSprintActive = true;
            freezeSprintMeter = LocalPlayer.sprintMeter;
            infiniteSprintSeconds = seconds;
            while (infiniteSprintSeconds > 0)
            {
                infiniteSprintSeconds--;
                yield return new WaitForSecondsRealtime(1f);
            }
            //infiniteSprintActive = false;
            infiniteSprintCoroutine = null;
        }

        private IEnumerator IncreasedMovementSpeedCoroutine(int seconds)
        {
            increasedMovementSpeedSeconds = seconds;
            while (increasedMovementSpeedSeconds > 0)
            {
                float movementSpeedMultiplier = 1 + (increasedMovementSpeedPercent / 100);
                LocalPlayer.movementSpeed = baseMovementSpeed * movementSpeedMultiplier;
                increasedMovementSpeedSeconds--;
                yield return new WaitForSecondsRealtime(1f);
            }
            increasedMovementSpeedPercent = 0;
            LocalPlayer.movementSpeed = baseMovementSpeed;
            increasedMovementSpeedCoroutine = null;
        }
    }
}
