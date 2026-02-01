using System;
using UnityEngine;
using Data.Session.Runtime;
using Data.World.Runtime;

namespace Systems.Police
{
    public class PoliceEscalationController : MonoBehaviour
    {
        [Header("Backend Data")]
        public SessionRuntimeData sessionData;
        public WorldState worldState;

        [Header("Escalation Settings")]
        public float decayRate = 1f;
        public float baseEscalationMultiplier = 1f;
        public float urbanMultiplier = 1.5f;

        [Header("Thresholds")]
        public float lowAlertThreshold = 25f;
        public float activePursuitThreshold = 50f;
        public float highIntensityThreshold = 80f;

        // Events
        public event Action OnPursuitStarted;
        public event Action<WantedLevel> OnPursuitEscalated;
        public event Action OnPursuitEnded;

        // Internal
        private float _currentEscalationMultiplier;
        private bool _isPursuitActive;

        private void Start()
        {
            if (sessionData == null)
            {
                sessionData = FindFirstObjectByType<SessionRuntimeData>();
                if (sessionData == null) Debug.LogError("[PoliceEscalation] Missing SessionRuntimeData!");
            }

            if (worldState == null)
            {
                worldState = FindFirstObjectByType<WorldState>();
            }

            if (worldState != null)
            {
                worldState.OnBiomeChanged += HandleBiomeChanged;
                // Initialize context
                HandleBiomeChanged(worldState.currentBiome);
            }
            else
            {
                // Default
                _currentEscalationMultiplier = baseEscalationMultiplier;
            }
        }

        private void OnDestroy()
        {
            if (worldState != null)
            {
                worldState.OnBiomeChanged -= HandleBiomeChanged;
            }
        }

        private void Update()
        {
            if (sessionData == null) return;

            // Decay logic (if no intensity added recently) - Simplified linear decay
            if (sessionData.globalThreatLevel > 0)
            {
                // Only decay if we want mechanic where hiding reduces heat
                // For now, let's assume decay happens if not actively engaged (placeholder logic)
                // sessionData.SetThreatLevel(sessionData.globalThreatLevel - (decayRate * Time.deltaTime)); 
            }
        }

        /// <summary>
        /// Adds to the global threat level, scaled by context.
        /// </summary>
        public void AddIntensity(float amount)
        {
            if (sessionData == null) return;
            if (sessionData.currentSessionState == SessionState.Ended) return;

            float scaledAmount = amount * _currentEscalationMultiplier;
            sessionData.SetThreatLevel(sessionData.globalThreatLevel + scaledAmount);

            EvaluateEscalation();
        }

        private void HandleBiomeChanged(string biomeName)
        {
            // Simple string check; usually this would be Enum or ScriptableObject
            if (!string.IsNullOrEmpty(biomeName) && biomeName.ToLower().Contains("city"))
            {
                _currentEscalationMultiplier = urbanMultiplier;
                Debug.Log("[PoliceEscalation] Urban context active. Escalation increased.");
            }
            else
            {
                _currentEscalationMultiplier = baseEscalationMultiplier;
                Debug.Log("[PoliceEscalation] Remote context active. Normal escalation.");

            }
        }

        private void EvaluateEscalation()
        {
            WantedLevel newLevel = WantedLevel.None;
            float threat = sessionData.globalThreatLevel;

            if (threat >= highIntensityThreshold) newLevel = WantedLevel.HighIntensity;
            else if (threat >= activePursuitThreshold) newLevel = WantedLevel.ActivePursuit;
            else if (threat >= lowAlertThreshold) newLevel = WantedLevel.LowAlert;

            if (sessionData.currentWantedLevel != newLevel)
            {
                sessionData.currentWantedLevel = newLevel;
                Debug.Log($"[PoliceEscalation] Wanted Level changed to: {newLevel}");

                if (newLevel != WantedLevel.None && !_isPursuitActive)
                {
                    _isPursuitActive = true;
                    OnPursuitStarted?.Invoke();
                }
                else if (newLevel == WantedLevel.None && _isPursuitActive)
                {
                    _isPursuitActive = false;
                    OnPursuitEnded?.Invoke();
                }

                if (_isPursuitActive)
                {
                    OnPursuitEscalated?.Invoke(newLevel);
                }
            }
        }
    }
}
