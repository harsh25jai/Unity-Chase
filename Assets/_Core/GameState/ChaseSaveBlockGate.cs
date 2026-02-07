using System;
using UnityEngine;
using Data.Session.Runtime;

namespace Core.GameState
{
    /// <summary>
    /// Centralized policy gate that determines if saving is currently allowed.
    /// Based on pursuit status and game session lifecycle.
    /// </summary>
    public class ChaseSaveBlockGate : MonoBehaviour
    {
        [Header("Dependencies")]
        public SessionRuntimeData sessionData;

        // Events
        public event Action<bool> OnSavePolicyChanged;

        private bool _isSaveAllowed = true;
        private bool _hasWarnedMissingData = false;

        private void Start()
        {
            if (sessionData == null)
                sessionData = FindFirstObjectByType<SessionRuntimeData>();

            // Initial evaluation
            EvaluatePolicy();
        }

        private void Update()
        {
            EvaluatePolicy();
        }

        /// <summary>
        /// Authoritative check for whether saving is allowed right now.
        /// </summary>
        public bool IsSaveAllowed()
        {
            if (sessionData == null)
            {
                if (!_hasWarnedMissingData)
                {
                    Debug.LogWarning("[ChaseSaveBlockGate] SessionRuntimeData is missing. Defaulting to ALLOWED save policy.");
                    _hasWarnedMissingData = true;
                }
                return true;
            }

            // disallow if pursuit is active
            if (sessionData.isPursuitActive) return false;

            // Disallow if session is not in Active state (e.g., Booting, Restarting, Ended)
            if (sessionData.currentSessionState != SessionState.Active) return false;

            return true;
        }

        private void EvaluatePolicy()
        {
            bool currentAllowed = IsSaveAllowed();
            if (currentAllowed != _isSaveAllowed)
            {
                _isSaveAllowed = currentAllowed;
                OnSavePolicyChanged?.Invoke(_isSaveAllowed);
                Debug.Log($"[ChaseSaveBlockGate] Save policy changed. Allowed: {_isSaveAllowed}");
            }
        }
    }
}
