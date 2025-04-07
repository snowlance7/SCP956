using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SCP956.Plugin;

namespace SCP956
{
    internal class DoorCollisionDetect : MonoBehaviour
    {
        private static ManualLogSource logger = LoggerInstance;
        public SCP956AI mainScript = null!;

        public bool triggering;
        float timeInTrigger = 0f;

        void OnTriggerStay(Collider other) // InteractTrigger
        {
            if (mainScript.currentBehaviourStateIndex == (int)SCP956AI.State.MovingTowardsPlayer)
            {
                //logger.LogDebug("OnTriggerEnter: " + other.tag);
                if (!triggering && other.CompareTag("InteractTrigger"))
                {
                    DoorLock doorLock = other.gameObject.GetComponent<DoorLock>();
                    if (doorLock != null && !doorLock.isDoorOpened)
                    {
                        timeInTrigger += Time.deltaTime;

                        if (timeInTrigger > 2f)
                        {
                            triggering = true;
                            timeInTrigger = 0f;
                            other.tag = "Untagged";
                            mainScript.BeginBashDoor(doorLock);
                        }
                    }
                }
            }
        }
    }
}