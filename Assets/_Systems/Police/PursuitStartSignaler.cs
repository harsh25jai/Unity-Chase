using System;
using UnityEngine;

namespace Systems.Police
{
    public class PursuitStartSignaler : MonoBehaviour
    {
        [Header("References")]
        public PoliceEscalationController escalationController;

        // Events
        public event Action<string> OnPursuitStarted; // reason
        public event Action<string> OnRadioChatterCueRequested; // cueType

        private void Start()
        {
            if (escalationController == null) 
                escalationController = FindFirstObjectByType<PoliceEscalationController>();
        }

        public void TriggerPursuit(string reason)
        {
            Debug.Log($"[PursuitSignaler] Pursuit Triggered: {reason}");
            
            // Emit internal signals
            OnPursuitStarted?.Invoke(reason);
            OnRadioChatterCueRequested?.Invoke("EscapeStart_Intro");

            // Kickstart escalation if possible
            if (escalationController != null)
            {
                // Give a starting boost to threat level
                escalationController.AddIntensity(30f); // Jump-start to around LowAlert
            }
        }
    }
}
